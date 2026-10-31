using System.Text.Json;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Duplicates;
using LeadPilot.Application.Models;
using LeadPilot.Domain;
using Microsoft.Data.Sqlite;

namespace LeadPilot.Data;

/// <summary>Duplicate candidates, merge and undo-merge (all multi-table changes are transactional).</summary>
public sealed class DuplicateRepository : IDuplicateRepository
{
    private readonly SqliteDb _db;
    private readonly IClock _clock;
    public DuplicateRepository(SqliteDb db, IClock clock) { _db = db; _clock = clock; }

    public int UpsertMatches(IEnumerable<DuplicateResult> matches)
    {
        var inserted = 0; var now = _clock.UtcNow;
        using var c = _db.Open(); using var tx = c.BeginTransaction();
        foreach (var m in matches)
        {
            var (a, b) = m.LeadAId < m.LeadBId ? (m.LeadAId, m.LeadBId) : (m.LeadBId, m.LeadAId);
            var existing = SqliteDb.Scalar<long?>(c, tx, "SELECT Decision FROM DuplicateMatches WHERE LeadAId=@a AND LeadBId=@b", ("@a", a), ("@b", b));
            if (existing == null)
            {
                SqliteDb.Exec(c, tx, "INSERT INTO DuplicateMatches(LeadAId,LeadBId,Score,Confidence,Reasons,Decision,CreatedAt) VALUES(@a,@b,@s,@c,@r,0,@n)",
                    ("@a", a), ("@b", b), ("@s", m.Score), ("@c", m.Confidence), ("@r", string.Join("; ", m.Reasons)), ("@n", now));
                inserted++;
            }
            else if (existing == (long)DuplicateDecision.Pending)
                SqliteDb.Exec(c, tx, "UPDATE DuplicateMatches SET Score=@s,Confidence=@c,Reasons=@r WHERE LeadAId=@a AND LeadBId=@b",
                    ("@s", m.Score), ("@c", m.Confidence), ("@r", string.Join("; ", m.Reasons)), ("@a", a), ("@b", b));
        }
        tx.Commit();
        return inserted;
    }

    public IReadOnlyList<DuplicateView> GetMatches(DuplicateDecision decision)
    {
        using var c = _db.Open();
        var matches = SqliteDb.Query(c, @"SELECT d.Id,d.LeadAId,d.LeadBId,d.Score,d.Confidence,d.Reasons,d.Decision,d.CreatedAt,d.DecidedAt,d.DecidedBy
 FROM DuplicateMatches d JOIN Leads a ON a.Id=d.LeadAId JOIN Leads b ON b.Id=d.LeadBId
 WHERE d.Decision=@d AND (@d<>0 OR (a.IsArchived=0 AND b.IsArchived=0)) ORDER BY d.Score DESC, d.Id",
            r => new DuplicateMatch { Id = r.I(0), LeadAId = r.I(1), LeadBId = r.I(2), Score = r.D(3), Confidence = r.E<DuplicateConfidence>(4), Reasons = r.S(5), Decision = r.E<DuplicateDecision>(6), CreatedAt = r.Dt(7), DecidedAt = r.NDt(8), DecidedBy = r.NS(9) },
            ("@d", decision));
        var rows = LeadSql.RowsByIds(c, matches.SelectMany(m => new[] { m.LeadAId, m.LeadBId }).Distinct()).ToDictionary(r => r.Id);
        return matches.Where(m => rows.ContainsKey(m.LeadAId) && rows.ContainsKey(m.LeadBId))
            .Select(m => new DuplicateView { Match = m, A = rows[m.LeadAId], B = rows[m.LeadBId] }).ToList();
    }

    public int CountPending()
    {
        using var c = _db.Open();
        return (int)SqliteDb.Scalar<long>(c, null, "SELECT COUNT(*) FROM DuplicateMatches d JOIN Leads a ON a.Id=d.LeadAId JOIN Leads b ON b.Id=d.LeadBId WHERE d.Decision=0 AND a.IsArchived=0 AND b.IsArchived=0");
    }

    public void SetDecision(int matchId, DuplicateDecision decision, string? by)
    {
        using var c = _db.Open();
        SqliteDb.Exec(c, "UPDATE DuplicateMatches SET Decision=@d, DecidedAt=@t, DecidedBy=@by WHERE Id=@id", ("@d", decision), ("@t", _clock.UtcNow), ("@by", by), ("@id", matchId));
    }

    public int ApplyMerge(MergePlan plan)
    {
        var now = _clock.UtcNow;
        using var c = _db.Open(); using var tx = c.BeginTransaction();
        if (SqliteDb.Scalar<long>(c, tx, "SELECT IsArchived FROM Leads WHERE Id=@id", ("@id", plan.MergedLeadId)) == 1)
            throw new InvalidOperationException("The lead to merge is already archived.");

        var moved = new MovedRecords
        {
            Sources = Ids(c, tx, "LeadSources", plan.MergedLeadId), Evidence = Ids(c, tx, "LeadFieldsEvidence", plan.MergedLeadId),
            Activities = Ids(c, tx, "LeadActivities", plan.MergedLeadId), Tasks = Ids(c, tx, "FollowUpTasks", plan.MergedLeadId)
        };
        foreach (var t in new[] { "LeadSources", "LeadFieldsEvidence", "LeadActivities", "FollowUpTasks" })
            SqliteDb.Exec(c, tx, $"UPDATE {t} SET LeadId=@s WHERE LeadId=@m", ("@s", plan.SurvivorLeadId), ("@m", plan.MergedLeadId));

        plan.SurvivorBusinessAfter.UpdatedAt = now;
        LeadRepository.UpdateBusinessSql(c, tx, plan.SurvivorBusinessAfter);
        SqliteDb.Exec(c, tx, @"UPDATE Leads SET PrimaryContactId=@pc, UpdatedAt=@n,
 IntentEvidence=COALESCE(IntentEvidence,(SELECT IntentEvidence FROM Leads WHERE Id=@m)),
 LastContactedAt=(SELECT MAX(x) FROM (SELECT LastContactedAt x FROM Leads WHERE Id=@s UNION ALL SELECT LastContactedAt FROM Leads WHERE Id=@m)),
 NextFollowUpAt=(SELECT MIN(DueAt) FROM FollowUpTasks WHERE LeadId=@s AND Status=0) WHERE Id=@s",
            ("@pc", plan.SurvivorPrimaryContactAfter), ("@n", now), ("@m", plan.MergedLeadId), ("@s", plan.SurvivorLeadId));
        SqliteDb.Exec(c, tx, "UPDATE Leads SET IsArchived=1, MergedIntoLeadId=@s, UpdatedAt=@n, NextFollowUpAt=NULL WHERE Id=@m", ("@s", plan.SurvivorLeadId), ("@n", now), ("@m", plan.MergedLeadId));

        var id = SqliteDb.Insert(c, tx, "INSERT INTO MergeHistory(SurvivorLeadId,MergedLeadId,MergedAt,MergedBy,SurvivorBeforeJson,MergedBeforeJson,MovedJson) VALUES(@s,@m,@t,@by,@sj,@mj,@mv)",
            ("@s", plan.SurvivorLeadId), ("@m", plan.MergedLeadId), ("@t", now), ("@by", plan.MergedBy), ("@sj", plan.SurvivorBeforeJson), ("@mj", plan.MergedBeforeJson), ("@mv", JsonSerializer.Serialize(moved)));
        SqliteDb.Exec(c, tx, "UPDATE DuplicateMatches SET Decision=@d, DecidedAt=@t, DecidedBy=@by WHERE LeadAId=@a AND LeadBId=@b",
            ("@d", DuplicateDecision.Merged), ("@t", now), ("@by", plan.MergedBy), ("@a", Math.Min(plan.SurvivorLeadId, plan.MergedLeadId)), ("@b", Math.Max(plan.SurvivorLeadId, plan.MergedLeadId)));
        LeadRepository.InsertActivity(c, tx, new LeadActivity { LeadId = plan.SurvivorLeadId, Type = ActivityType.Merged, Subject = "Merged duplicate lead", Body = $"Lead id {plan.MergedLeadId} merged into this lead.", CreatedAt = now });
        tx.Commit();
        return id;
    }

    private static List<int> Ids(SqliteConnection c, SqliteTransaction tx, string table, int leadId)
    {
        using var cmd = c.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = $"SELECT Id FROM {table} WHERE LeadId=@l"; cmd.Parameters.AddWithValue("@l", leadId);
        using var r = cmd.ExecuteReader(); var l = new List<int>();
        while (r.Read()) l.Add((int)r.GetInt64(0));
        return l;
    }

    public void UndoMerge(int mergeId, string? by)
    {
        var now = _clock.UtcNow;
        using var c = _db.Open(); using var tx = c.BeginTransaction();
        var rec = SqliteDb.QueryTx(c, tx, "SELECT Id,SurvivorLeadId,MergedLeadId,MergedAt,MergedBy,UndoneAt,SurvivorBeforeJson,MergedBeforeJson,MovedJson FROM MergeHistory WHERE Id=@id", ReadMerge, ("@id", mergeId)).FirstOrDefault()
                  ?? throw new InvalidOperationException("Merge record not found.");
        if (rec.UndoneAt != null) throw new InvalidOperationException("This merge was already undone.");
        var survivor = JsonSerializer.Deserialize<LeadSnapshot>(rec.SurvivorBeforeJson)!;
        var merged = JsonSerializer.Deserialize<LeadSnapshot>(rec.MergedBeforeJson)!;
        var moved = JsonSerializer.Deserialize<MovedRecords>(rec.MovedJson)!;

        survivor.Business.UpdatedAt = now;
        LeadRepository.UpdateBusinessSql(c, tx, survivor.Business);
        SqliteDb.Exec(c, tx, "UPDATE Leads SET PrimaryContactId=@pc, IntentEvidence=@ie, LastContactedAt=@lc, UpdatedAt=@n WHERE Id=@id",
            ("@pc", survivor.Lead.PrimaryContactId), ("@ie", survivor.Lead.IntentEvidence), ("@lc", survivor.Lead.LastContactedAt), ("@n", now), ("@id", rec.SurvivorLeadId));
        SqliteDb.Exec(c, tx, "UPDATE Leads SET IsArchived=@a, MergedIntoLeadId=NULL, UpdatedAt=@n WHERE Id=@id", ("@a", merged.Lead.IsArchived), ("@n", now), ("@id", rec.MergedLeadId));
        Back(c, tx, "LeadSources", moved.Sources, rec.MergedLeadId);
        Back(c, tx, "LeadFieldsEvidence", moved.Evidence, rec.MergedLeadId);
        Back(c, tx, "LeadActivities", moved.Activities, rec.MergedLeadId);
        Back(c, tx, "FollowUpTasks", moved.Tasks, rec.MergedLeadId);
        foreach (var id in new[] { rec.SurvivorLeadId, rec.MergedLeadId })
            SqliteDb.Exec(c, tx, "UPDATE Leads SET NextFollowUpAt=(SELECT MIN(DueAt) FROM FollowUpTasks WHERE LeadId=@l AND Status=0) WHERE Id=@l", ("@l", id));
        SqliteDb.Exec(c, tx, "UPDATE MergeHistory SET UndoneAt=@n WHERE Id=@id", ("@n", now), ("@id", mergeId));
        SqliteDb.Exec(c, tx, "UPDATE DuplicateMatches SET Decision=0, DecidedAt=NULL, DecidedBy=NULL WHERE LeadAId=@a AND LeadBId=@b",
            ("@a", Math.Min(rec.SurvivorLeadId, rec.MergedLeadId)), ("@b", Math.Max(rec.SurvivorLeadId, rec.MergedLeadId)));
        LeadRepository.InsertActivity(c, tx, new LeadActivity { LeadId = rec.SurvivorLeadId, Type = ActivityType.Merged, Subject = "Merge undone", Body = $"Lead id {rec.MergedLeadId} restored by {by ?? "user"}.", CreatedAt = now });
        tx.Commit();
    }

    private static void Back(SqliteConnection c, SqliteTransaction tx, string table, List<int> ids, int leadId)
    {
        foreach (var id in ids) SqliteDb.Exec(c, tx, $"UPDATE {table} SET LeadId=@l WHERE Id=@id", ("@l", leadId), ("@id", id));
    }

    private static MergeRecord ReadMerge(SqliteDataReader r) => new()
    {
        Id = r.I(0), SurvivorLeadId = r.I(1), MergedLeadId = r.I(2), MergedAt = r.Dt(3), MergedBy = r.NS(4), UndoneAt = r.NDt(5),
        SurvivorBeforeJson = r.S(6), MergedBeforeJson = r.S(7), MovedJson = r.S(8)
    };

    public IReadOnlyList<MergeRecord> GetMerges()
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, "SELECT Id,SurvivorLeadId,MergedLeadId,MergedAt,MergedBy,UndoneAt,SurvivorBeforeJson,MergedBeforeJson,MovedJson FROM MergeHistory ORDER BY Id DESC", ReadMerge);
    }
}
