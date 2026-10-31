using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Domain;
using Microsoft.Data.Sqlite;

namespace LeadPilot.Data;

/// <summary>Dashboard and analytics aggregates, computed live from the database.</summary>
public sealed class StatsRepository : IStatsRepository
{
    private readonly SqliteDb _db;
    public StatsRepository(SqliteDb db) { _db = db; }

    public DashboardStats GetDashboard(DateTime nowUtc)
    {
        using var c = _db.Open();
        long N(string sql, params (string, object?)[] p) => SqliteDb.Scalar<long>(c, null, sql, p);
        const string active = "FROM Leads l JOIN Businesses b ON b.Id=l.BusinessId LEFT JOIN Contacts c ON c.Id=l.PrimaryContactId WHERE l.IsArchived=0";
        var s = new DashboardStats
        {
            TotalDiscovered = (int)N("SELECT COALESCE(SUM(ResultCount),0) FROM ApiUsage WHERE Operation='search' AND Success=1"),
            TotalLeads = (int)N("SELECT COUNT(*) " + active),
            NewLeads = (int)N("SELECT COUNT(*) " + active + " AND l.Status=0"),
            QualifiedLeads = (int)N("SELECT COUNT(*) " + active + " AND l.Status IN (2,3,4)"),
            HighPriorityLeads = (int)N("SELECT COUNT(*) " + active + " AND l.Temperature=3 AND l.Status NOT IN (4,5,6)"),
            IncompleteRecords = (int)N($"SELECT COUNT(*) {active} AND ({LeadSql.PhoneExpr} IS NULL OR {LeadSql.EmailExpr} IS NULL OR COALESCE(b.Website,'')='')"),
            DuplicateRecords = (int)N("SELECT COUNT(*) FROM DuplicateMatches d JOIN Leads a ON a.Id=d.LeadAId JOIN Leads x ON x.Id=d.LeadBId WHERE d.Decision=0 AND a.IsArchived=0 AND x.IsArchived=0"),
            PendingFollowUps = (int)N("SELECT COUNT(*) FROM FollowUpTasks t JOIN Leads l ON l.Id=t.LeadId WHERE t.Status=0 AND l.IsArchived=0"),
            OverdueFollowUps = (int)N("SELECT COUNT(*) FROM FollowUpTasks t JOIN Leads l ON l.Id=t.LeadId WHERE t.Status=0 AND t.DueAt < @n AND l.IsArchived=0", ("@n", nowUtc)),
            ConvertedLeads = (int)N("SELECT COUNT(*) " + active + " AND l.Status=4")
        };
        List<KeyCount> Top(string expr, int take) => SqliteDb.Query(c, $"SELECT COALESCE(NULLIF({expr},''),'Unknown') k, COUNT(*) n {active} GROUP BY k ORDER BY n DESC, k LIMIT {take}", r => new KeyCount(r.S(0), r.I(1)));
        s.ByIndustry = Top("b.Industry", 8); s.ByLocation = Top("b.City", 8); s.BySource = Top("l.SourceName", 8);
        var temps = SqliteDb.Query(c, "SELECT l.Temperature, COUNT(*) " + active + " GROUP BY l.Temperature", r => (r.I(0), r.I(1))).ToDictionary(x => x.Item1, x => x.Item2);
        s.Quality = new[] { LeadTemperature.Hot, LeadTemperature.Warm, LeadTemperature.Cold, LeadTemperature.Unqualified }.Select(t => new KeyCount(t.ToString(), temps.GetValueOrDefault((int)t))).ToList();
        var st = SqliteDb.Query(c, "SELECT l.Status, COUNT(*) " + active + " GROUP BY l.Status", r => (r.I(0), r.I(1))).ToDictionary(x => x.Item1, x => x.Item2);
        s.Pipeline = Enum.GetValues<LeadStatus>().Select(t => new KeyCount(t.ToString(), st.GetValueOrDefault((int)t))).ToList();

        var months = Enumerable.Range(0, 12).Select(i => new DateTime(nowUtc.Year, nowUtc.Month, 1).AddMonths(-11 + i)).ToList();
        var since = months[0];
        var created = SqliteDb.Query(c, "SELECT strftime('%Y-%m', l.CreatedAt), COUNT(*) FROM Leads l WHERE l.IsArchived=0 AND l.CreatedAt >= @s GROUP BY 1", r => (r.S(0), r.I(1)), ("@s", since)).ToDictionary(x => x.Item1, x => x.Item2);
        var won = SqliteDb.Query(c, "SELECT strftime('%Y-%m', l.UpdatedAt), COUNT(*) FROM Leads l WHERE l.IsArchived=0 AND l.Status=4 AND l.UpdatedAt >= @s GROUP BY 1", r => (r.S(0), r.I(1)), ("@s", since)).ToDictionary(x => x.Item1, x => x.Item2);
        s.MonthlyDiscovery = months.Select(m => new KeyCount(m.ToString("MMM yy", System.Globalization.CultureInfo.InvariantCulture), created.GetValueOrDefault(m.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)))).ToList();
        s.ConversionTrend = months.Select(m => new KeyCount(m.ToString("MMM yy", System.Globalization.CultureInfo.InvariantCulture), won.GetValueOrDefault(m.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)))).ToList();
        return s;
    }

    public IReadOnlyList<SourcePerformance> GetSourcePerformance()
    {
        using var c = _db.Open();
        return SqliteDb.Query(c, @"SELECT SourceName, COUNT(*), SUM(CASE WHEN Status IN (2,3,4) THEN 1 ELSE 0 END), SUM(CASE WHEN Status=4 THEN 1 ELSE 0 END), AVG(Score)
 FROM Leads WHERE IsArchived=0 GROUP BY SourceName ORDER BY 4 DESC, 3 DESC, 2 DESC",
            r => new SourcePerformance(r.S(0), r.I(1), r.I(2), r.I(3), Math.Round(r.D(4), 1)));
    }
}
