using System.Text;
using LeadPilot.Application.Models;
using Microsoft.Data.Sqlite;

namespace LeadPilot.Data;

/// <summary>Shared SELECT for <see cref="LeadRow"/> and the query-to-WHERE translation.</summary>
internal static class LeadSql
{
    public const string PhoneExpr = "COALESCE(NULLIF(c.Phone,''), NULLIF(b.Phone,''))";
    public const string EmailExpr = "COALESCE(NULLIF(c.Email,''), NULLIF(b.Email,''))";

    public const string RowSelect = @"SELECT l.Id, l.LeadCode, b.Name, c.FullName, " + PhoneExpr + ", " + EmailExpr + @", b.Website, b.Industry,
 TRIM(COALESCE(b.City,'') || CASE WHEN NULLIF(b.City,'') IS NOT NULL AND NULLIF(b.Region,'') IS NOT NULL THEN ', ' ELSE '' END || COALESCE(b.Region,'')),
 l.SourceName, l.Score, l.Temperature, l.Status, u.Name, l.CreatedAt, l.LastContactedAt, l.NextFollowUpAt, l.EmailStatus, l.PhoneStatus, l.WebsiteStatus
 FROM Leads l JOIN Businesses b ON b.Id = l.BusinessId LEFT JOIN Contacts c ON c.Id = l.PrimaryContactId LEFT JOIN SalesUsers u ON u.Id = l.AssignedUserId";

    public static LeadRow Row(SqliteDataReader r) => new()
    {
        Id = r.I(0), LeadCode = r.S(1), CompanyName = r.S(2), ContactPerson = r.NS(3), Phone = r.NS(4), Email = r.NS(5), Website = r.NS(6), Industry = r.NS(7),
        Location = string.IsNullOrWhiteSpace(r.NS(8)) ? null : r.NS(8), Source = r.S(9), Score = r.I(10), Temperature = r.E<Domain.LeadTemperature>(11),
        Status = r.E<Domain.LeadStatus>(12), AssignedTo = r.NS(13), CreatedAt = r.Dt(14), LastContactedAt = r.NDt(15), NextFollowUpAt = r.NDt(16),
        EmailStatus = r.E<Domain.EmailStatus>(17), PhoneStatus = r.E<Domain.PhoneStatus>(18), WebsiteStatus = r.E<Domain.WebsiteStatus>(19)
    };

    private static readonly Dictionary<LeadSort, string> SortColumns = new()
    {
        [LeadSort.Created] = "l.CreatedAt", [LeadSort.Company] = "b.Name COLLATE NOCASE", [LeadSort.Score] = "l.Score", [LeadSort.Status] = "l.Status",
        [LeadSort.NextFollowUp] = "l.NextFollowUpAt", [LeadSort.LastContacted] = "l.LastContactedAt", [LeadSort.LeadId] = "l.Id"
    };

    /// <summary>Builds " WHERE …" and parameters from a query. Values are always bound, never concatenated.</summary>
    public static (string Where, List<(string, object?)> Params) Where(LeadQuery q, DateTime nowUtc)
    {
        var w = new List<string>(); var p = new List<(string, object?)>();
        if (!q.IncludeArchived) w.Add("l.IsArchived = 0");
        if (!string.IsNullOrWhiteSpace(q.Text))
        {
            var t = "%" + q.Text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            w.Add(@"(b.Name LIKE @t ESCAPE '\' OR l.LeadCode LIKE @t ESCAPE '\' OR c.FullName LIKE @t ESCAPE '\' OR b.Phone LIKE @t ESCAPE '\' OR c.Phone LIKE @t ESCAPE '\'
 OR b.Email LIKE @t ESCAPE '\' OR c.Email LIKE @t ESCAPE '\' OR b.Website LIKE @t ESCAPE '\' OR b.Industry LIKE @t ESCAPE '\' OR b.City LIKE @t ESCAPE '\' OR b.Address LIKE @t ESCAPE '\')");
            p.Add(("@t", t));
        }
        if (q.Status != null) { w.Add("l.Status = @st"); p.Add(("@st", q.Status)); }
        if (q.Temperature != null) { w.Add("l.Temperature = @tp"); p.Add(("@tp", q.Temperature)); }
        if (!string.IsNullOrWhiteSpace(q.Source)) { w.Add("l.SourceName = @src"); p.Add(("@src", q.Source)); }
        if (!string.IsNullOrWhiteSpace(q.Industry)) { w.Add("b.Industry = @ind"); p.Add(("@ind", q.Industry)); }
        if (!string.IsNullOrWhiteSpace(q.City)) { w.Add("b.City = @city"); p.Add(("@city", q.City)); }
        if (q.AssignedUserId != null) { w.Add("l.AssignedUserId = @au"); p.Add(("@au", q.AssignedUserId)); }
        if (q.CampaignId != null) { w.Add("l.CampaignId = @cmp"); p.Add(("@cmp", q.CampaignId)); }
        if (q.MinScore != null) { w.Add("l.Score >= @ms"); p.Add(("@ms", q.MinScore)); }
        if (q.OnlyIncomplete) w.Add($"({PhoneExpr} IS NULL OR {EmailExpr} IS NULL OR COALESCE(b.Website,'') = '')");
        if (q.OnlyMissingContact) w.Add($"({PhoneExpr} IS NULL AND {EmailExpr} IS NULL)");
        if (q.OnlyNeedsFollowUp) { w.Add("l.NextFollowUpAt IS NOT NULL AND l.NextFollowUpAt <= @now"); p.Add(("@now", nowUtc)); }
        return (w.Count == 0 ? "" : " WHERE " + string.Join(" AND ", w), p);
    }

    public static string OrderBy(LeadQuery q) =>
        $" ORDER BY {SortColumns[q.Sort]} {(q.Descending ? "DESC" : "ASC")}, l.Id DESC";

    /// <summary>Appends a parameterised IN list: returns "(@i0,@i1,…)".</summary>
    public static string InList(IEnumerable<int> ids, List<(string, object?)> p, string prefix = "@i")
    {
        var sb = new StringBuilder("("); var n = 0;
        foreach (var id in ids) { if (n > 0) sb.Append(','); sb.Append(prefix).Append(n); p.Add((prefix + n, id)); n++; }
        if (n == 0) { sb.Append("-1"); }
        return sb.Append(')').ToString();
    }

    public static List<LeadRow> RowsByIds(SqliteConnection c, IEnumerable<int> ids)
    {
        var p = new List<(string, object?)>();
        var inl = InList(ids, p);
        return SqliteDb.Query(c, RowSelect + " WHERE l.Id IN " + inl, Row, p.ToArray());
    }
}
