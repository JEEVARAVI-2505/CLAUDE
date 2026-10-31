using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;

namespace CRM_Master.Tests.Support;

/// <summary>Creates real CSV / XLSX input files for tests.</summary>
public static class Files
{
    public static string Csv(string folder, string name, IEnumerable<string[]> rows, string delimiter = ",", bool bom = false)
    {
        var path = Path.Combine(folder, name);
        using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(bom));
        using var csv = new CsvWriter(w, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter });
        foreach (var r in rows) { foreach (var f in r) csv.WriteField(f); csv.NextRecord(); }
        return path;
    }

    public static string Xlsx(string folder, string name, IEnumerable<object?[]> rows, string sheet = "Leads")
    {
        var path = Path.Combine(folder, name);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(sheet);
        var r = 1;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
            {
                var cell = ws.Cell(r, c + 1);
                switch (row[c])
                {
                    case null: break;
                    case DateTime d: cell.Value = d; break;
                    case long l: cell.Value = l; break;
                    case int i: cell.Value = i; break;
                    case double db: cell.Value = db; break;
                    default: cell.Value = row[c]!.ToString(); break;
                }
            }
            r++;
        }
        wb.SaveAs(path);
        return path;
    }

    public static readonly string[] StandardHeader = { "First Name", "Last Name", "Email", "Mobile", "Company", "City" };
}
