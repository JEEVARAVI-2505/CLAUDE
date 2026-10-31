using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;

namespace LeadPilot.Infrastructure.Files;

/// <summary>CSV / Excel / PDF export. Text cells that start with = + - @ are neutralised to prevent spreadsheet formula injection.</summary>
public sealed class ExportService : IExportService
{
    private static readonly string[] Headers =
    {
        "Lead ID", "Company", "Contact person", "Phone", "Email", "Website", "Industry", "Location", "Source", "Score", "Relevance", "Status", "Assigned to",
        "Created", "Last contacted", "Next follow-up", "Email check", "Phone check", "Website check"
    };

    private static string Date(DateTime? d) => d?.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "";

    private static string[] Cells(LeadRow r) => new[]
    {
        r.LeadCode, r.CompanyName, r.ContactPerson ?? "", r.Phone ?? "", r.Email ?? "", r.Website ?? "", r.Industry ?? "", r.Location ?? "", r.Source,
        r.Score.ToString(CultureInfo.InvariantCulture), r.Temperature.ToString(), r.Status.ToString(), r.AssignedTo ?? "",
        Date(r.CreatedAt), Date(r.LastContactedAt), Date(r.NextFollowUpAt), r.EmailStatus.ToString(), r.PhoneStatus.ToString(), r.WebsiteStatus.ToString()
    };

    /// <summary>Prefixes a single quote to values a spreadsheet would interpret as a formula.</summary>
    public static string Safe(string v) => v.Length > 0 && v[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + v : v;

    public void ExportCsv(IReadOnlyList<LeadRow> rows, string path)
    {
        EnsureDir(path);
        using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
        using var csv = new CsvWriter(w, new CsvConfiguration(CultureInfo.InvariantCulture));
        foreach (var h in Headers) csv.WriteField(h);
        csv.NextRecord();
        foreach (var r in rows) { foreach (var c in Cells(r)) csv.WriteField(Safe(c)); csv.NextRecord(); }
    }

    public void ExportXlsx(IReadOnlyList<LeadRow> rows, string path)
    {
        EnsureDir(path);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Leads");
        for (var c = 0; c < Headers.Length; c++) ws.Cell(1, c + 1).Value = Headers[c];
        ws.Row(1).Style.Font.Bold = true; ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF9");
        for (var i = 0; i < rows.Count; i++)
        {
            var cells = Cells(rows[i]);
            for (var c = 0; c < cells.Length; c++)
            {
                if (c == 9) ws.Cell(i + 2, c + 1).Value = rows[i].Score;
                else ws.Cell(i + 2, c + 1).Value = Safe(cells[c]);
            }
        }
        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents(1, Math.Min(rows.Count + 1, 200));
        foreach (var col in ws.ColumnsUsed()) if (col.Width > 50) col.Width = 50;
        wb.SaveAs(path);
    }

    public void ExportPdf(IReadOnlyList<LeadRow> rows, string path, string title)
    {
        EnsureDir(path);
        var pdf = new SimplePdfWriter("LeadPilot AI - relevance scores show fit with your target market, not buying intent");
        pdf.Line(title, 16, true, 2);
        pdf.Line($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}  |  {rows.Count} lead(s)", 9, false, 8);
        var widths = new[] { 55.0, 170, 90, 150, 110, 90, 35, 55, 45 };
        pdf.Row(new[] { "Lead ID", "Company", "Phone", "Email", "Industry", "Location", "Score", "Status", "Relevance" }, widths, 8, true);
        foreach (var r in rows)
            pdf.Row(new[] { r.LeadCode, r.CompanyName, r.Phone ?? "", r.Email ?? "", r.Industry ?? "", r.Location ?? "", r.Score.ToString(CultureInfo.InvariantCulture), r.Status.ToString(), r.Temperature.ToString() }, widths);
        File.WriteAllBytes(path, pdf.ToBytes());
    }

    public void ExportTextReportPdf(string title, IReadOnlyList<string> lines, string path)
    {
        EnsureDir(path);
        var pdf = new SimplePdfWriter("LeadPilot AI");
        pdf.Line(title, 16, true, 2);
        pdf.Line($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}", 9, false, 8);
        foreach (var l in lines)
            foreach (var part in Wrap(l, 150)) pdf.Line(part, 10, false, 3);
        File.WriteAllBytes(path, pdf.ToBytes());
    }

    private static IEnumerable<string> Wrap(string s, int width)
    {
        foreach (var raw in s.Split('\n'))
        {
            var line = raw;
            while (line.Length > width) { var cut = line.LastIndexOf(' ', width); if (cut < 20) cut = width; yield return line[..cut]; line = line[cut..].TrimStart(); }
            yield return line;
        }
    }

    private static void EnsureDir(string path) { var d = Path.GetDirectoryName(Path.GetFullPath(path)); if (!string.IsNullOrEmpty(d)) Directory.CreateDirectory(d); }
}
