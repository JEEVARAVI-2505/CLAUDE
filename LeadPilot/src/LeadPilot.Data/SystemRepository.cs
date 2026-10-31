using LeadPilot.Application.Abstractions;
using LeadPilot.Domain;

namespace LeadPilot.Data;

/// <summary>Settings, users, audit log, import history, API usage and destructive data operations.</summary>
public sealed class SystemRepository : ISystemRepository
{
    private readonly SqliteDb _db;
    private readonly IClock _clock;
    public SystemRepository(SqliteDb db, IClock clock) { _db = db; _clock = clock; }

    public string? GetSetting(string key)
    {
        using var c = _db.Open();
        return SqliteDb.Scalar<string>(c, null, "SELECT Value FROM Settings WHERE Key=@k", ("@k", key));
    }

    public void SetSetting(string key, string value)
    {
        using var c = _db.Open();
        SqliteDb.Exec(c, "INSERT INTO Settings(Key,Value,UpdatedAt) VALUES(@k,@v,@t) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value, UpdatedAt=excluded.UpdatedAt", ("@k", key), ("@v", value), ("@t", _clock.UtcNow));
    }

    public int AddUser(SalesUser u)
    {
        if (string.IsNullOrWhiteSpace(u.Name)) throw new ArgumentException("User name is required.");
        u.CreatedAt = _clock.UtcNow;
        using var c = _db.Open();
        return u.Id = SqliteDb.Insert(c, null, "INSERT INTO SalesUsers(Name,Email,Role,PinHash,PinSalt,IsActive,CreatedAt) VALUES(@n,@e,@r,@ph,@ps,@a,@c)",
            ("@n", u.Name), ("@e", u.Email), ("@r", u.Role), ("@ph", u.PinHash), ("@ps", u.PinSalt), ("@a", u.IsActive), ("@c", u.CreatedAt));
    }

    public void UpdateUser(SalesUser u)
    {
        using var c = _db.Open();
        SqliteDb.Exec(c, "UPDATE SalesUsers SET Name=@n,Email=@e,Role=@r,PinHash=@ph,PinSalt=@ps,IsActive=@a WHERE Id=@id",
            ("@n", u.Name), ("@e", u.Email), ("@r", u.Role), ("@ph", u.PinHash), ("@ps", u.PinSalt), ("@a", u.IsActive), ("@id", u.Id));
    }

    public IReadOnlyList<SalesUser> GetUsers()
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, $"SELECT {Mappers.UserCols} FROM SalesUsers ORDER BY Name", Mappers.User);
    }

    public SalesUser? GetUser(int id)
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, $"SELECT {Mappers.UserCols} FROM SalesUsers WHERE Id=@id", Mappers.User, ("@id", id)).FirstOrDefault();
    }

    public void AddAudit(AuditEntry e)
    {
        using var c = _db.Open();
        SqliteDb.Exec(c, "INSERT INTO AuditLogs(At,UserId,Action,Entity,EntityId,Details) VALUES(@t,@u,@a,@e,@i,@d)",
            ("@t", e.At == default ? _clock.UtcNow : e.At), ("@u", e.UserId), ("@a", e.Action), ("@e", e.Entity), ("@i", e.EntityId), ("@d", e.Details));
    }

    public IReadOnlyList<AuditEntry> GetAudit(int limit)
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, "SELECT Id,At,UserId,Action,Entity,EntityId,Details FROM AuditLogs ORDER BY Id DESC LIMIT @l",
            r => new AuditEntry { Id = r.I(0), At = r.Dt(1), UserId = r.NI(2), Action = r.S(3), Entity = r.NS(4), EntityId = r.NS(5), Details = r.NS(6) }, ("@l", limit));
    }

    public int AddImport(ImportRecord r)
    {
        using var c = _db.Open();
        return r.Id = SqliteDb.Insert(c, null, "INSERT INTO ImportHistory(FileName,FileHash,TotalRows,Imported,Duplicates,Skipped,Errors,ReportJson,ImportedAt,UserId) VALUES(@f,@h,@t,@i,@d,@s,@e,@r,@at,@u)",
            ("@f", r.FileName), ("@h", r.FileHash), ("@t", r.TotalRows), ("@i", r.Imported), ("@d", r.Duplicates), ("@s", r.Skipped), ("@e", r.Errors), ("@r", r.ReportJson),
            ("@at", r.ImportedAt == default ? _clock.UtcNow : r.ImportedAt), ("@u", r.UserId));
    }

    public IReadOnlyList<ImportRecord> GetImports(int limit)
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, "SELECT Id,FileName,FileHash,TotalRows,Imported,Duplicates,Skipped,Errors,ReportJson,ImportedAt,UserId FROM ImportHistory ORDER BY Id DESC LIMIT @l",
            r => new ImportRecord { Id = r.I(0), FileName = r.S(1), FileHash = r.NS(2), TotalRows = r.I(3), Imported = r.I(4), Duplicates = r.I(5), Skipped = r.I(6), Errors = r.I(7), ReportJson = r.S(8), ImportedAt = r.Dt(9), UserId = r.NI(10) }, ("@l", limit));
    }

    public void AddApiUsage(ApiUsageRecord r)
    {
        using var c = _db.Open();
        SqliteDb.Exec(c, "INSERT INTO ApiUsage(At,Provider,Operation,Requests,Success,ResultCount,ErrorMessage) VALUES(@t,@p,@o,@r,@s,@c,@e)",
            ("@t", r.At == default ? _clock.UtcNow : r.At), ("@p", r.Provider), ("@o", r.Operation), ("@r", r.Requests), ("@s", r.Success), ("@c", r.ResultCount), ("@e", r.ErrorMessage));
    }

    public IReadOnlyList<ApiUsageRecord> GetApiUsage(int limit)
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, "SELECT Id,At,Provider,Operation,Requests,Success,ResultCount,ErrorMessage FROM ApiUsage ORDER BY Id DESC LIMIT @l",
            r => new ApiUsageRecord { Id = r.I(0), At = r.Dt(1), Provider = r.S(2), Operation = r.S(3), Requests = r.I(4), Success = r.B(5), ResultCount = r.I(6), ErrorMessage = r.NS(7) }, ("@l", limit));
    }

    public void DeleteAllBusinessData()
    {
        using var c = _db.Open(); using var tx = c.BeginTransaction();
        foreach (var t in new[] { "DuplicateMatches", "MergeHistory", "LeadActivities", "FollowUpTasks", "LeadFieldsEvidence", "LeadSources", "Leads", "Contacts", "Businesses", "Campaigns", "ImportHistory", "ApiUsage" })
            SqliteDb.Exec(c, tx, $"DELETE FROM {t}");
        SqliteDb.Exec(c, tx, "DELETE FROM sqlite_sequence WHERE name IN ('Leads','Businesses','Contacts','Campaigns')");
        tx.Commit();
    }
}
