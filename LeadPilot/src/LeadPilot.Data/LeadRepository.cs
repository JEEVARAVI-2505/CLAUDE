using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Duplicates;
using LeadPilot.Application.Models;
using LeadPilot.Domain;

namespace LeadPilot.Data;

/// <summary>SQLite implementation of <see cref="ILeadRepository"/>.</summary>
public sealed class LeadRepository : ILeadRepository
{
    private readonly SqliteDb _db;
    private readonly IClock _clock;

    public LeadRepository(SqliteDb db, IClock clock) { _db = db; _clock = clock; }

    public LeadPage Search(LeadQuery q)
    {
        var (where, p) = LeadSql.Where(q, _clock.UtcNow);
        var size = Math.Clamp(q.PageSize, 1, 1000); var page = Math.Max(1, q.Page);
        using var c = _db.Open();
        var total = (int)SqliteDb.Scalar<long>(c, null, "SELECT COUNT(*) FROM Leads l JOIN Businesses b ON b.Id=l.BusinessId LEFT JOIN Contacts c ON c.Id=l.PrimaryContactId" + where, p.ToArray());
        var pp = new List<(string, object?)>(p) { ("@lim", size), ("@off", (page - 1) * size) };
        var rows = SqliteDb.Query(c, LeadSql.RowSelect + where + LeadSql.OrderBy(q) + " LIMIT @lim OFFSET @off", LeadSql.Row, pp.ToArray());
        return new LeadPage(rows, total, page, size);
    }

    public IReadOnlyList<LeadRow> Export(LeadQuery q, IReadOnlyCollection<int>? ids = null)
    {
        var (where, p) = LeadSql.Where(q, _clock.UtcNow);
        if (ids != null)
        {
            var inl = LeadSql.InList(ids, p, "@x");
            where += (where.Length == 0 ? " WHERE " : " AND ") + "l.Id IN " + inl;
        }
        using var c = _db.Open();
        return SqliteDb.Query(c, LeadSql.RowSelect + where + LeadSql.OrderBy(q), LeadSql.Row, p.ToArray());
    }

    public Lead? GetLead(int leadId)
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, $"SELECT {Mappers.LeadCols} FROM Leads WHERE Id=@id", Mappers.Lead, ("@id", leadId)).FirstOrDefault();
    }

    public Business? GetBusiness(int businessId)
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, $"SELECT {Mappers.BusinessCols} FROM Businesses WHERE Id=@id", Mappers.Business, ("@id", businessId)).FirstOrDefault();
    }

    public LeadDetail? GetDetail(int leadId)
    {
        using var c = _db.Open();
        var lead = SqliteDb.Query(c, $"SELECT {Mappers.LeadCols} FROM Leads WHERE Id=@id", Mappers.Lead, ("@id", leadId)).FirstOrDefault();
        if (lead == null) return null;
        var biz = SqliteDb.Query(c, $"SELECT {Mappers.BusinessCols} FROM Businesses WHERE Id=@id", Mappers.Business, ("@id", lead.BusinessId)).First();
        return new LeadDetail
        {
            Lead = lead, Business = biz,
            Contacts = SqliteDb.Query(c, $"SELECT {Mappers.ContactCols} FROM Contacts WHERE BusinessId=@b ORDER BY IsPrimary DESC, Id", Mappers.Contact, ("@b", lead.BusinessId)),
            Sources = SqliteDb.Query(c, $"SELECT {Mappers.SourceCols} FROM LeadSources WHERE LeadId=@id ORDER BY RetrievedAt", Mappers.Source, ("@id", leadId)),
            Evidence = SqliteDb.Query(c, $"SELECT {Mappers.EvidenceCols} FROM LeadFieldsEvidence WHERE LeadId=@id ORDER BY ExtractedAt DESC, Id DESC", Mappers.Evidence, ("@id", leadId)),
            Activities = SqliteDb.Query(c, $"SELECT {Mappers.ActivityCols} FROM LeadActivities WHERE LeadId=@id ORDER BY CreatedAt DESC, Id DESC", Mappers.Activity, ("@id", leadId)),
            Tasks = SqliteDb.Query(c, $"SELECT {Mappers.TaskCols} FROM FollowUpTasks WHERE LeadId=@id ORDER BY DueAt", Mappers.Task, ("@id", leadId)),
            AssignedTo = lead.AssignedUserId == null ? null : SqliteDb.Scalar<string>(c, null, "SELECT Name FROM SalesUsers WHERE Id=@u", ("@u", lead.AssignedUserId))
        };
    }

    public int Insert(LeadInsert ins)
    {
        Validate(ins);
        var now = _clock.UtcNow;
        using var c = _db.Open();
        using var tx = c.BeginTransaction();
        var b = ins.Business;
        b.CreatedAt = b.CreatedAt == default ? now : b.CreatedAt; b.UpdatedAt = now;
        b.Id = SqliteDb.Insert(c, tx, @"INSERT INTO Businesses(Name,Website,Domain,Phone,NormalizedPhone,Email,NormalizedEmail,Address,City,Region,Country,PostalCode,Industry,Services,Description,ContactPageUrl,CompanySize,Latitude,Longitude,CreatedAt,UpdatedAt)
 VALUES(@name,@web,@dom,@ph,@nph,@em,@nem,@addr,@city,@reg,@ctry,@pc,@ind,@svc,@desc,@cp,@size,@lat,@lon,@cr,@up)", Mappers.BusinessParams(b));

        var lead = ins.Lead;
        lead.BusinessId = b.Id;
        if (ins.Contact != null)
        {
            var ct = ins.Contact; ct.BusinessId = b.Id; ct.IsPrimary = true; ct.CreatedAt = now; ct.UpdatedAt = now;
            ct.Id = InsertContact(c, tx, ct);
            lead.PrimaryContactId = ct.Id;
        }
        lead.CreatedAt = now; lead.UpdatedAt = now;
        lead.Id = SqliteDb.Insert(c, tx, @"INSERT INTO Leads(LeadCode,BusinessId,PrimaryContactId,CampaignId,AssignedUserId,Status,Score,Temperature,ScoreExplanation,EmailStatus,PhoneStatus,WebsiteStatus,IntentEvidence,SourceName,CreatedAt,UpdatedAt)
 VALUES(@code,@b,@pc,@cmp,@au,@st,@sc,@tp,@ex,@es,@ps,@ws,@ie,@src,@cr,@up)",
            ("@code", "PENDING-" + Guid.NewGuid().ToString("N")), ("@b", b.Id), ("@pc", lead.PrimaryContactId), ("@cmp", lead.CampaignId), ("@au", lead.AssignedUserId),
            ("@st", lead.Status), ("@sc", lead.Score), ("@tp", lead.Temperature), ("@ex", lead.ScoreExplanation), ("@es", lead.EmailStatus), ("@ps", lead.PhoneStatus),
            ("@ws", lead.WebsiteStatus), ("@ie", lead.IntentEvidence), ("@src", lead.SourceName), ("@cr", now), ("@up", now));
        lead.LeadCode = $"LD-{lead.Id:D6}";
        SqliteDb.Exec(c, tx, "UPDATE Leads SET LeadCode=@c WHERE Id=@id", ("@c", lead.LeadCode), ("@id", lead.Id));

        if (ins.Source != null) { ins.Source.LeadId = lead.Id; InsertSource(c, tx, ins.Source); }
        foreach (var e in ins.Evidence) { e.LeadId = lead.Id; InsertEvidence(c, tx, e); }
        var act = ins.Activity ?? new LeadActivity { Type = ActivityType.Created, Subject = "Lead created" };
        act.LeadId = lead.Id; act.CreatedAt = now;
        InsertActivity(c, tx, act);
        tx.Commit();
        return lead.Id;
    }

    private static void Validate(LeadInsert ins)
    {
        if (string.IsNullOrWhiteSpace(ins.Business.Name)) throw new ArgumentException("Company name is required.");
        if (ins.Business.Name.Length > 300) throw new ArgumentException("Company name is too long (max 300 characters).");
        if (ins.Contact != null && string.IsNullOrWhiteSpace(ins.Contact.FullName)) throw new ArgumentException("Contact name is required when a contact is supplied.");
        if (ins.Lead.Score is < 0 or > 100) throw new ArgumentException("Score must be between 0 and 100.");
    }

    internal static int InsertContact(Microsoft.Data.Sqlite.SqliteConnection c, Microsoft.Data.Sqlite.SqliteTransaction? tx, Contact ct) =>
        SqliteDb.Insert(c, tx, @"INSERT INTO Contacts(BusinessId,FullName,JobTitle,Phone,NormalizedPhone,Email,NormalizedEmail,IsPrimary,CreatedAt,UpdatedAt)
 VALUES(@b,@n,@t,@ph,@nph,@em,@nem,@ip,@cr,@up)", ("@b", ct.BusinessId), ("@n", ct.FullName), ("@t", ct.JobTitle), ("@ph", ct.Phone), ("@nph", ct.NormalizedPhone),
            ("@em", ct.Email), ("@nem", ct.NormalizedEmail), ("@ip", ct.IsPrimary), ("@cr", ct.CreatedAt), ("@up", ct.UpdatedAt));

    internal static void InsertSource(Microsoft.Data.Sqlite.SqliteConnection c, Microsoft.Data.Sqlite.SqliteTransaction? tx, LeadSourceRecord s) =>
        s.Id = SqliteDb.Insert(c, tx, "INSERT INTO LeadSources(LeadId,SourceName,SourceType,SourceUrl,ExternalId,License,RetrievedAt) VALUES(@l,@n,@t,@u,@x,@li,@r)",
            ("@l", s.LeadId), ("@n", s.SourceName), ("@t", s.SourceType), ("@u", s.SourceUrl), ("@x", s.ExternalId), ("@li", s.License), ("@r", s.RetrievedAt));

    internal static void InsertEvidence(Microsoft.Data.Sqlite.SqliteConnection c, Microsoft.Data.Sqlite.SqliteTransaction? tx, FieldEvidence e) =>
        e.Id = SqliteDb.Insert(c, tx, "INSERT INTO LeadFieldsEvidence(LeadId,FieldName,Value,SourceName,SourceUrl,ExtractedAt,Confidence,Applied,Note) VALUES(@l,@f,@v,@s,@u,@x,@c,@a,@n)",
            ("@l", e.LeadId), ("@f", e.FieldName), ("@v", e.Value), ("@s", e.SourceName), ("@u", e.SourceUrl), ("@x", e.ExtractedAt), ("@c", e.Confidence), ("@a", e.Applied), ("@n", e.Note));

    internal static void InsertActivity(Microsoft.Data.Sqlite.SqliteConnection c, Microsoft.Data.Sqlite.SqliteTransaction? tx, LeadActivity a)
    {
        a.Id = SqliteDb.Insert(c, tx, "INSERT INTO LeadActivities(LeadId,Type,Subject,Body,CreatedAt,UserId) VALUES(@l,@t,@s,@b,@c,@u)",
            ("@l", a.LeadId), ("@t", a.Type), ("@s", a.Subject), ("@b", a.Body), ("@c", a.CreatedAt), ("@u", a.UserId));
        if (a.Type is ActivityType.Call or ActivityType.Email or ActivityType.Meeting or ActivityType.WhatsApp)
            SqliteDb.Exec(c, tx, "UPDATE Leads SET LastContactedAt=@t, UpdatedAt=@t WHERE Id=@id AND (LastContactedAt IS NULL OR LastContactedAt < @t)", ("@t", a.CreatedAt), ("@id", a.LeadId));
    }

    internal static void UpdateBusinessSql(Microsoft.Data.Sqlite.SqliteConnection c, Microsoft.Data.Sqlite.SqliteTransaction? tx, Business b)
    {
        var p = Mappers.BusinessParams(b).Append(("@id", (object?)b.Id)).ToArray();
        SqliteDb.Exec(c, tx, @"UPDATE Businesses SET Name=@name,Website=@web,Domain=@dom,Phone=@ph,NormalizedPhone=@nph,Email=@em,NormalizedEmail=@nem,Address=@addr,City=@city,Region=@reg,Country=@ctry,PostalCode=@pc,
 Industry=@ind,Services=@svc,Description=@desc,ContactPageUrl=@cp,CompanySize=@size,Latitude=@lat,Longitude=@lon,UpdatedAt=@up WHERE Id=@id", p);
    }

    public void UpdateBusiness(Business b)
    {
        if (string.IsNullOrWhiteSpace(b.Name)) throw new ArgumentException("Company name is required.");
        b.UpdatedAt = _clock.UtcNow;
        using var c = _db.Open();
        UpdateBusinessSql(c, null, b);
    }

    public void UpdateLead(Lead l)
    {
        if (l.Score is < 0 or > 100) throw new ArgumentException("Score must be between 0 and 100.");
        l.UpdatedAt = _clock.UtcNow;
        using var c = _db.Open();
        SqliteDb.Exec(c, @"UPDATE Leads SET PrimaryContactId=@pc,CampaignId=@cmp,AssignedUserId=@au,Status=@st,Score=@sc,Temperature=@tp,ScoreExplanation=@ex,EmailStatus=@es,PhoneStatus=@ps,WebsiteStatus=@ws,
 IntentEvidence=@ie,IsArchived=@ar,MergedIntoLeadId=@mi,SourceName=@src,UpdatedAt=@up,LastContactedAt=@lc,NextFollowUpAt=@nf WHERE Id=@id",
            ("@pc", l.PrimaryContactId), ("@cmp", l.CampaignId), ("@au", l.AssignedUserId), ("@st", l.Status), ("@sc", l.Score), ("@tp", l.Temperature), ("@ex", l.ScoreExplanation),
            ("@es", l.EmailStatus), ("@ps", l.PhoneStatus), ("@ws", l.WebsiteStatus), ("@ie", l.IntentEvidence), ("@ar", l.IsArchived), ("@mi", l.MergedIntoLeadId), ("@src", l.SourceName),
            ("@up", l.UpdatedAt), ("@lc", l.LastContactedAt), ("@nf", l.NextFollowUpAt), ("@id", l.Id));
    }

    public int AddContact(Contact ct)
    {
        if (string.IsNullOrWhiteSpace(ct.FullName)) throw new ArgumentException("Contact name is required.");
        var now = _clock.UtcNow; ct.CreatedAt = now; ct.UpdatedAt = now;
        using var c = _db.Open();
        return ct.Id = InsertContact(c, null, ct);
    }

    public void UpdateContact(Contact ct)
    {
        ct.UpdatedAt = _clock.UtcNow;
        using var c = _db.Open();
        SqliteDb.Exec(c, "UPDATE Contacts SET FullName=@n,JobTitle=@t,Phone=@ph,NormalizedPhone=@nph,Email=@em,NormalizedEmail=@nem,IsPrimary=@ip,UpdatedAt=@up WHERE Id=@id",
            ("@n", ct.FullName), ("@t", ct.JobTitle), ("@ph", ct.Phone), ("@nph", ct.NormalizedPhone), ("@em", ct.Email), ("@nem", ct.NormalizedEmail), ("@ip", ct.IsPrimary), ("@up", ct.UpdatedAt), ("@id", ct.Id));
    }

    public void AddSource(LeadSourceRecord s) { using var c = _db.Open(); InsertSource(c, null, s); }
    public void AddEvidence(FieldEvidence e) { using var c = _db.Open(); InsertEvidence(c, null, e); }

    public void BulkSetStatus(IReadOnlyCollection<int> ids, LeadStatus status)
    {
        if (ids.Count == 0) return;
        var now = _clock.UtcNow;
        using var c = _db.Open(); using var tx = c.BeginTransaction();
        var p = new List<(string, object?)> { ("@s", status), ("@now", now) };
        SqliteDb.Exec(c, tx, "UPDATE Leads SET Status=@s, UpdatedAt=@now WHERE Id IN " + LeadSql.InList(ids, p), p.ToArray());
        foreach (var id in ids)
            InsertActivity(c, tx, new LeadActivity { LeadId = id, Type = ActivityType.StatusChange, Subject = $"Status set to {status}", CreatedAt = now });
        tx.Commit();
    }

    public void BulkAssign(IReadOnlyCollection<int> ids, int? userId)
    {
        if (ids.Count == 0) return;
        var p = new List<(string, object?)> { ("@u", userId), ("@now", _clock.UtcNow) };
        using var c = _db.Open();
        SqliteDb.Exec(c, "UPDATE Leads SET AssignedUserId=@u, UpdatedAt=@now WHERE Id IN " + LeadSql.InList(ids, p), p.ToArray());
    }

    public void Delete(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0) return;
        using var c = _db.Open(); using var tx = c.BeginTransaction();
        var p = new List<(string, object?)>();
        var inl = LeadSql.InList(ids, p);
        SqliteDb.Exec(c, tx, "DELETE FROM Leads WHERE Id IN " + inl, p.ToArray());
        SqliteDb.Exec(c, tx, "DELETE FROM Businesses WHERE Id NOT IN (SELECT BusinessId FROM Leads)");
        tx.Commit();
    }

    public IReadOnlyList<MatchProfile> GetMatchProfiles() => LoadProfiles(null);

    public MatchProfile? GetMatchProfile(int leadId) => LoadProfiles(leadId).FirstOrDefault();

    private List<MatchProfile> LoadProfiles(int? leadId)
    {
        using var c = _db.Open();
        var sql = @"SELECT l.Id, l.LeadCode, b.Name, b.Domain, b.NormalizedPhone, c.NormalizedPhone, b.NormalizedEmail, c.NormalizedEmail, b.Address, b.City
 FROM Leads l JOIN Businesses b ON b.Id=l.BusinessId LEFT JOIN Contacts c ON c.Id=l.PrimaryContactId WHERE l.IsArchived=0" + (leadId == null ? "" : " AND l.Id=@id");
        var p = leadId == null ? Array.Empty<(string, object?)>() : new (string, object?)[] { ("@id", leadId) };
        return SqliteDb.Query(c, sql, r => new MatchProfile
        {
            LeadId = r.I(0), LeadCode = r.S(1), Name = r.S(2), Domain = r.NS(3),
            Phones = new[] { r.NS(4), r.NS(5) }.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToArray(),
            Emails = new[] { r.NS(6), r.NS(7) }.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToArray(),
            Address = r.NS(8), City = r.NS(9)
        }, p);
    }

    public FilterOptions GetFilterOptions()
    {
        using var c = _db.Open();
        List<string> Distinct(string sql) => SqliteDb.Query(c, sql, r => r.S(0));
        return new FilterOptions(
            Distinct("SELECT DISTINCT b.Industry FROM Businesses b JOIN Leads l ON l.BusinessId=b.Id WHERE COALESCE(b.Industry,'')<>'' AND l.IsArchived=0 ORDER BY 1"),
            Distinct("SELECT DISTINCT b.City FROM Businesses b JOIN Leads l ON l.BusinessId=b.Id WHERE COALESCE(b.City,'')<>'' AND l.IsArchived=0 ORDER BY 1"),
            Distinct("SELECT DISTINCT SourceName FROM Leads WHERE IsArchived=0 ORDER BY 1"));
    }

    public int Count(bool includeArchived = false)
    {
        using var c = _db.Open();
        return (int)SqliteDb.Scalar<long>(c, null, "SELECT COUNT(*) FROM Leads" + (includeArchived ? "" : " WHERE IsArchived=0"));
    }

    public int PurgeClosedBefore(DateTime cutoffUtc)
    {
        using var c = _db.Open(); using var tx = c.BeginTransaction();
        var n = SqliteDb.Exec(c, tx, "DELETE FROM Leads WHERE (IsArchived=1 OR Status IN (@lost,@unq)) AND UpdatedAt < @cut",
            ("@lost", LeadStatus.Lost), ("@unq", LeadStatus.Unqualified), ("@cut", cutoffUtc));
        SqliteDb.Exec(c, tx, "DELETE FROM Businesses WHERE Id NOT IN (SELECT BusinessId FROM Leads)");
        tx.Commit();
        return n;
    }
}
