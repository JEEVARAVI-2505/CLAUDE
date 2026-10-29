using System.Text;
using BuildingStudio.Core.Model;
using BuildingStudio.Engine.Drawing;
using BuildingStudio.Engine.Model3D;
using BuildingStudio.IO.Dxf;
using BuildingStudio.IO.Excel;
using BuildingStudio.IO.Gltf;
using BuildingStudio.IO.Pdf;

namespace BuildingStudio.Studio.Pipeline;

public sealed record ExportResult(string Directory, IReadOnlyList<string> Files);

/// <summary>"Plot to drawings, BOQ and report in one click."</summary>
public static class ExportService
{
    public static ExportResult ExportAll(StudioService studio, BuildingModel m, string directory)
    {
        Directory.CreateDirectory(directory);
        var drawings = Path.Combine(directory, "drawings");
        Directory.CreateDirectory(drawings);
        var files = new List<string>();
        string Out(string name) { var p = Path.Combine(directory, name); files.Add(p); return p; }

        ProjectSerializer.Save(m, Out("project.json"));

        var sheets = DrawingSetBuilder.Build(m);
        foreach (var sheet in sheets)
        {
            var baseName = Path.Combine(drawings, $"{sheet.Name} {Safe(sheet.Title)}");
            File.WriteAllText(baseName + ".svg", SvgWriter.Write(sheet, 1400));
            DxfExporter.Save(sheet, baseName + ".dxf");
            files.Add(baseName + ".svg");
            files.Add(baseName + ".dxf");
        }

        var report = studio.Evaluate(m);
        ExcelExporter.Save(Out("boq.xlsx"), m.Name, report.Cost, ExtraSheets(m, report));
        PdfReportWriter.Save(ReportComposer.Compose(m, report, sheets), Out("report.pdf"));

        var glb = GltfExporter.ToGlb(SceneBuilder.Build(m, new SceneOptions(Furniture: true)));
        File.WriteAllBytes(Out("model.glb"), glb);
        File.WriteAllText(Out("viewer.html"), HtmlViewerExporter.Build(glb, m.Name,
            $"{m.Site.Facing}-facing · {(m.Brief.Floors == 1 ? "G" : $"G+{m.Brief.Floors - 1}")} · {m.Exterior.Style} · Vastu {report.Vastu.Score:0}/100"));

        File.WriteAllText(Out("summary.md"), Summary(m, report, sheets));
        return new ExportResult(directory, files);
    }

    private static string Safe(string s) => string.Concat(s.Select(c => char.IsLetterOrDigit(c) || c is ' ' or '-' ? c : '_')).Trim();

    private static IEnumerable<SheetTable> ExtraSheets(BuildingModel m, ModelReport r)
    {
        yield return new SheetTable("Areas", ["Floor", "Room", "Type", "Target m²", "Actual m²", "Delta %", "Zone"],
            r.Areas.Select(a => (IReadOnlyList<object?>)[a.Level, a.Room, a.Type.ToString(), a.TargetSqm, a.ActualSqm, a.DeltaPct, a.Zone.ToString()]).ToList());
        yield return new SheetTable("Vastu", ["Floor", "Room", "Zone", "Fit", "Penalty", "Explanation"],
            r.Vastu.Rooms.Select(v => (IReadOnlyList<object?>)[v.Level, v.RoomName, v.Zone.ToString(), v.Fit.ToString(), v.Penalty, v.Explanation]).ToList());
        yield return new SheetTable("Compliance", ["Rule", "Category", "Check", "Required", "Actual", "Status"],
            r.Compliance.Items.Select(i => (IReadOnlyList<object?>)[i.RuleId, i.Category, i.Description, i.Required, i.Actual, i.Status.ToString()]).ToList());
        yield return new SheetTable("BBS", ["Member", "Bar mark", "Shape", "Dia (mm)", "Nos", "Cut length (mm)", "Weight (kg)"],
            m.Structure.Bbs.Select(b => (IReadOnlyList<object?>)[b.Member, b.BarMark, b.Shape, b.Dia, b.Count, b.CutLength, b.Weight]).ToList());
        yield return new SheetTable("Electrical", ["Floor", "Room", "Area m²", "Lights", "Fans", "Sockets", "Power sockets"],
            r.Electrical.Select(e => (IReadOnlyList<object?>)[e.Level, e.Room, e.AreaSqm, e.Lights, e.Fans, e.Sockets, e.PowerSockets]).ToList());
    }

    public static string Summary(BuildingModel m, ModelReport r, IReadOnlyList<DrawingSheet> sheets)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {m.Name}").AppendLine();
        sb.AppendLine($"- Vastu score: **{r.Vastu.Score:0}/100**");
        sb.AppendLine($"- Bylaw checks: {r.Compliance.Items.Count} ({r.Compliance.Failures} fail, {r.Compliance.Warnings} warnings) - {r.Compliance.Disclaimer}");
        sb.AppendLine($"- Built-up area: {m.BuiltUpArea() / 1e6:0.0} m² ({r.Cost.BuiltUpSqft:0} sq ft)");
        sb.AppendLine($"- Estimated cost: {r.Cost.Currency} {ReportComposer.Money(r.Cost.Total)} ({ReportComposer.Money(r.Cost.CostPerSqft)}/sq ft incl. GST)");
        sb.AppendLine($"- Structure: {m.Structure.Columns.Count} columns, {m.Structure.Beams.Count} beams, {m.Structure.Footings.Count} footings, steel {m.Structure.Bbs.Sum(b => b.Weight):0} kg (PRELIMINARY - engineer must verify)");
        sb.AppendLine($"- Drawings: {string.Join(", ", sheets.Select(s => s.Title))}").AppendLine();
        sb.AppendLine("## Rooms").AppendLine().AppendLine("| Floor | Room | Zone | Target m² | Actual m² |").AppendLine("|---|---|---|---|---|");
        foreach (var a in r.Areas) sb.AppendLine($"| {a.Level} | {a.Room} | {a.Zone} | {a.TargetSqm} | {a.ActualSqm} |");
        return sb.ToString();
    }
}
