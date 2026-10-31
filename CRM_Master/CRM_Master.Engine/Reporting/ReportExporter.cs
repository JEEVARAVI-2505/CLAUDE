using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;

namespace CRM_Master.Engine.Reporting;

/// <summary>Writes a <see cref="ReportData"/> to Excel, CSV or PDF.</summary>
public static class ReportExporter
{
    public static string Extension(ExportFormat f) => f switch { ExportFormat.Excel => ".xlsx", ExportFormat.Csv => ".csv", _ => ".pdf" };

    public static void Export(ReportData report, ExportFormat format, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        switch (format)
        {
            case ExportFormat.Excel: ToExcel(report, path); break;
            case ExportFormat.Csv: ToCsv(report, path); break;
            default: File.WriteAllBytes(path, MiniPdfWriter.Render(report)); break;
        }
    }

    public static void ToExcel(ReportData r, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(Safe(r.Title));
        var row = 1;
        ws.Cell(row, 1).Value = r.Title; ws.Cell(row, 1).Style.Font.Bold = true; ws.Cell(row, 1).Style.Font.FontSize = 16; ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#0F2654"); row++;
        if (r.Subtitle is not null) { ws.Cell(row, 1).Value = r.Subtitle; ws.Cell(row, 1).Style.Font.FontColor = XLColor.Gray; row++; }
        row++;
        foreach (var (label, value) in r.Summary)
        { ws.Cell(row, 1).Value = label; ws.Cell(row, 1).Style.Font.Bold = true; ws.Cell(row, 2).Value = value; row++; }
        if (r.Summary.Count > 0) row++;

        var headerRow = row;
        for (var c = 0; c < r.Columns.Count; c++)
        {
            var cell = ws.Cell(row, c + 1); cell.Value = r.Columns[c];
            cell.Style.Font.Bold = true; cell.Style.Font.FontColor = XLColor.White; cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F2654");
        }
        row++;
        foreach (var data in r.Rows)
        {
            for (var c = 0; c < r.Columns.Count; c++)
            {
                var cell = ws.Cell(row, c + 1);
                switch (data[c])
                {
                    case null: break;
                    case DateTime d: cell.Value = d; cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm"; break;
                    case int or long or double or decimal: cell.Value = Convert.ToDouble(data[c], CultureInfo.InvariantCulture); break;
                    case bool b: cell.Value = b; break;
                    default: cell.SetValue(SafeCell(data[c]!.ToString() ?? "")); cell.Style.NumberFormat.Format = "@"; break;   // text stays text (phone numbers!)
                }
            }
            row++;
        }
        if (r.Rows.Count > 0) { ws.Range(headerRow, 1, row - 1, r.Columns.Count).SetAutoFilter(); ws.SheetView.FreezeRows(headerRow); }
        ws.Columns().AdjustToContents(headerRow, Math.Min(row, headerRow + 200));
        foreach (var col in ws.ColumnsUsed()) if (col.Width > 60) col.Width = 60;
        ws.Cell(row + 1, 1).Value = r.Footer + "  |  Generated " + r.GeneratedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        ws.Cell(row + 1, 1).Style.Font.FontColor = XLColor.Gray; ws.Cell(row + 1, 1).Style.Font.FontSize = 8;
        wb.SaveAs(path);
    }

    public static void ToCsv(ReportData r, string path)
    {
        using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));      // BOM so Excel opens UTF-8 correctly
        using var csv = new CsvWriter(w, new CsvConfiguration(CultureInfo.InvariantCulture) { ShouldQuote = a => true });
        foreach (var col in r.Columns) csv.WriteField(col);
        csv.NextRecord();
        foreach (var row in r.Rows)
        {
            foreach (var v in row) csv.WriteField(v switch { null => "", DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), IFormattable f => f.ToString(null, CultureInfo.InvariantCulture), _ => SafeCell(v.ToString() ?? "") });
            csv.NextRecord();
        }
    }

    private static string Safe(string s) { var t = new string(s.Where(c => !"[]:*?/\\".Contains(c)).ToArray()); return t.Length > 31 ? t[..31] : t.Length == 0 ? "Report" : t; }

    /// <summary>Neutralises spreadsheet formula injection: a text value starting with = + - @ would otherwise be executed by Excel.</summary>
    internal static string SafeCell(string s) => s.Length > 0 && s[0] is '=' or '@' || (s.Length > 1 && s[0] is '+' or '-' && !char.IsDigit(s[1]) && s[1] != ' ' && s[1] != '.') ? "'" + s : s;
}
