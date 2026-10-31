using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Domain;

namespace LeadPilot.Data;

/// <summary>Activities, follow-up tasks and campaigns.</summary>
public sealed class WorkRepository : IWorkRepository
{
    private readonly SqliteDb _db;
    private readonly IClock _clock;
    public WorkRepository(SqliteDb db, IClock clock) { _db = db; _clock = clock; }

    public void AddActivity(LeadActivity a)
    {
        if (a.CreatedAt == default) a.CreatedAt = _clock.UtcNow;
        if (string.IsNullOrWhiteSpace(a.Subject)) throw new ArgumentException("Activity subject is required.");
        using var c = _db.Open();
        LeadRepository.InsertActivity(c, null, a);
    }

    public IReadOnlyList<LeadActivity> GetActivities(int leadId)
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, $"SELECT {Mappers.ActivityCols} FROM LeadActivities WHERE LeadId=@l ORDER BY CreatedAt DESC, Id DESC", Mappers.Activity, ("@l", leadId));
    }

    public int AddTask(FollowUpTask t)
    {
        if (string.IsNullOrWhiteSpace(t.Title)) throw new ArgumentException("Task title is required.");
        t.CreatedAt = _clock.UtcNow;
        using var c = _db.Open(); using var tx = c.BeginTransaction();
        t.Id = SqliteDb.Insert(c, tx, "INSERT INTO FollowUpTasks(LeadId,Title,Notes,DueAt,Status,AssignedUserId,CreatedAt,CompletedAt) VALUES(@l,@t,@n,@d,@s,@a,@c,@x)",
            ("@l", t.LeadId), ("@t", t.Title), ("@n", t.Notes), ("@d", t.DueAt), ("@s", t.Status), ("@a", t.AssignedUserId), ("@c", t.CreatedAt), ("@x", t.CompletedAt));
        Refresh(c, tx, t.LeadId);
        tx.Commit();
        return t.Id;
    }

    public void UpdateTask(FollowUpTask t)
    {
        using var c = _db.Open(); using var tx = c.BeginTransaction();
        SqliteDb.Exec(c, tx, "UPDATE FollowUpTasks SET Title=@t,Notes=@n,DueAt=@d,Status=@s,AssignedUserId=@a,CompletedAt=@x WHERE Id=@id",
            ("@t", t.Title), ("@n", t.Notes), ("@d", t.DueAt), ("@s", t.Status), ("@a", t.AssignedUserId), ("@x", t.CompletedAt), ("@id", t.Id));
        Refresh(c, tx, t.LeadId);
        tx.Commit();
    }

    public FollowUpTask? GetTask(int id)
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, $"SELECT {Mappers.TaskCols} FROM FollowUpTasks WHERE Id=@id", Mappers.Task, ("@id", id)).FirstOrDefault();
    }

    public IReadOnlyList<TaskRow> GetTasks(TaskQuery q, DateTime nowUtc)
    {
        var w = new List<string> { "l.IsArchived=0" }; var p = new List<(string, object?)>();
        if (q.Status != null) { w.Add("t.Status=@s"); p.Add(("@s", q.Status)); }
        if (q.OnlyOverdue) { w.Add("t.Status=0 AND t.DueAt < @now"); p.Add(("@now", nowUtc)); }
        if (q.LeadId != null) { w.Add("t.LeadId=@l"); p.Add(("@l", q.LeadId)); }
        if (q.AssignedUserId != null) { w.Add("t.AssignedUserId=@a"); p.Add(("@a", q.AssignedUserId)); }
        if (q.DueBefore != null) { w.Add("t.DueAt <= @db"); p.Add(("@db", q.DueBefore)); }
        var sql = @"SELECT t.Id,t.LeadId,t.Title,t.Notes,t.DueAt,t.Status,t.AssignedUserId,t.CreatedAt,t.CompletedAt, l.LeadCode, b.Name, u.Name
 FROM FollowUpTasks t JOIN Leads l ON l.Id=t.LeadId JOIN Businesses b ON b.Id=l.BusinessId LEFT JOIN SalesUsers u ON u.Id=t.AssignedUserId
 WHERE " + string.Join(" AND ", w) + " ORDER BY t.Status, t.DueAt";
        using var c = _db.Open();
        return SqliteDb.Query(c, sql, r => new TaskRow { Task = Mappers.Task(r), LeadCode = r.S(9), CompanyName = r.S(10), AssignedTo = r.NS(11) }, p.ToArray());
    }

    public void RefreshNextFollowUp(int leadId)
    {
        using var c = _db.Open();
        Refresh(c, null, leadId);
    }

    private static void Refresh(Microsoft.Data.Sqlite.SqliteConnection c, Microsoft.Data.Sqlite.SqliteTransaction? tx, int leadId) =>
        SqliteDb.Exec(c, tx, "UPDATE Leads SET NextFollowUpAt=(SELECT MIN(DueAt) FROM FollowUpTasks WHERE LeadId=@l AND Status=0) WHERE Id=@l", ("@l", leadId));

    public int AddCampaign(Campaign cmp)
    {
        if (string.IsNullOrWhiteSpace(cmp.Name)) throw new ArgumentException("Campaign name is required.");
        cmp.CreatedAt = _clock.UtcNow;
        using var c = _db.Open();
        return cmp.Id = SqliteDb.Insert(c, null, "INSERT INTO Campaigns(Name,Product,TargetCustomer,Location,TargetCount,Status,Notes,CreatedAt) VALUES(@n,@p,@t,@l,@c,@s,@no,@cr)",
            ("@n", cmp.Name), ("@p", cmp.Product), ("@t", cmp.TargetCustomer), ("@l", cmp.Location), ("@c", cmp.TargetCount), ("@s", cmp.Status), ("@no", cmp.Notes), ("@cr", cmp.CreatedAt));
    }

    public void UpdateCampaign(Campaign cmp)
    {
        using var c = _db.Open();
        SqliteDb.Exec(c, "UPDATE Campaigns SET Name=@n,Product=@p,TargetCustomer=@t,Location=@l,TargetCount=@c,Status=@s,Notes=@no WHERE Id=@id",
            ("@n", cmp.Name), ("@p", cmp.Product), ("@t", cmp.TargetCustomer), ("@l", cmp.Location), ("@c", cmp.TargetCount), ("@s", cmp.Status), ("@no", cmp.Notes), ("@id", cmp.Id));
    }

    public Campaign? GetCampaign(int id)
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, $"SELECT {Mappers.CampaignCols} FROM Campaigns WHERE Id=@id", Mappers.Campaign, ("@id", id)).FirstOrDefault();
    }

    public IReadOnlyList<Campaign> GetCampaigns()
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, $"SELECT {Mappers.CampaignCols} FROM Campaigns ORDER BY CreatedAt DESC, Id DESC", Mappers.Campaign);
    }

    public void DeleteCampaign(int id) { using var c = _db.Open(); SqliteDb.Exec(c, "DELETE FROM Campaigns WHERE Id=@id", ("@id", id)); }

    public int LeadCountForCampaign(int campaignId)
    {
        using var c = _db.Open();
        return (int)SqliteDb.Scalar<long>(c, null, "SELECT COUNT(*) FROM Leads WHERE CampaignId=@c AND IsArchived=0", ("@c", campaignId));
    }
}
