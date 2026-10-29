using BuildingStudio.Engine.Quantity;
using ClosedXML.Excel;

namespace BuildingStudio.IO.Excel;

/// <summary>A plain table to add as an extra worksheet.</summary>
public sealed record SheetTable(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<object?>> Rows);

/// <summary>BOQ workbook with live formulas (Qty × Rate, totals, contingency, margin, GST).</summary>
public static class ExcelExporter
{
    public static void Save(string path, string project, CostSummary cost, IEnumerable<SheetTable>? extra = null)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("BOQ");
        ws.Cell(1, 1).Value = $"Bill of Quantities - {project}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Rates: {cost.Rates.Name} ({cost.Rates.Year}). {cost.Rates.Disclaimer}";
        string[] headers = ["Group", "Code", "Description", "Unit", "Quantity", $"Rate ({cost.Currency})", $"Amount ({cost.Currency})"];
        for (var c = 0; c < headers.Length; c++)
        {
            ws.Cell(4, c + 1).Value = headers[c];
            ws.Cell(4, c + 1).Style.Font.Bold = true;
            ws.Cell(4, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        var row = 5;
        foreach (var l in cost.Lines)
        {
            ws.Cell(row, 1).Value = l.Group;
            ws.Cell(row, 2).Value = l.Code;
            ws.Cell(row, 3).Value = l.Description;
            ws.Cell(row, 4).Value = l.Unit;
            ws.Cell(row, 5).Value = l.Quantity;
            ws.Cell(row, 6).Value = l.Rate;
            ws.Cell(row, 7).FormulaA1 = $"E{row}*F{row}";
            row++;
        }
        var last = row - 1;
        void Total(string label, string formula, bool bold = false)
        {
            ws.Cell(row, 3).Value = label;
            ws.Cell(row, 7).FormulaA1 = formula;
            if (bold) ws.Range(row, 3, row, 7).Style.Font.Bold = true;
            row++;
        }
        row++;
        var sub = row;
        Total("Subtotal", $"SUM(G5:G{last})", true);
        Total($"Contingency {cost.Rates.ContingencyPct}%", $"G{sub}*{cost.Rates.ContingencyPct}/100");
        Total($"Contractor margin {cost.Rates.ContractorMarginPct}%", $"(G{sub}+G{sub + 1})*{cost.Rates.ContractorMarginPct}/100");
        Total($"GST {cost.Rates.GstPct}%", $"(G{sub}+G{sub + 1}+G{sub + 2})*{cost.Rates.GstPct}/100");
        Total("TOTAL", $"SUM(G{sub}:G{sub + 3})", true);
        ws.Cell(row, 3).Value = "Built-up area (sq ft)";
        ws.Cell(row, 7).Value = cost.BuiltUpSqft;
        row++;
        Total("Cost per sq ft", $"G{row - 2}/G{row - 1}", true);
        ws.Range(5, 5, row, 7).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns().AdjustToContents(4, row);
        ws.Column(3).Width = 60;

        foreach (var t in extra ?? [])
        {
            var s = wb.AddWorksheet(t.Name.Length > 31 ? t.Name[..31] : t.Name);
            for (var c = 0; c < t.Headers.Count; c++)
            {
                s.Cell(1, c + 1).Value = t.Headers[c];
                s.Cell(1, c + 1).Style.Font.Bold = true;
                s.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            for (var r = 0; r < t.Rows.Count; r++)
                for (var c = 0; c < t.Rows[r].Count; c++)
                    s.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(t.Rows[r][c]);
            s.Columns().AdjustToContents();
        }
        wb.SaveAs(path);
    }
}
