using System.Globalization;
using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Engine.Compliance;
using BuildingStudio.Engine.Drawing;
using BuildingStudio.IO.Pdf;

namespace BuildingStudio.Studio.Pipeline;

/// <summary>Builds the combined client report (plans, Vastu, compliance, structure, BOQ) from engine results.</summary>
public static class ReportComposer
{
    private static readonly CultureInfo Inr = CultureInfo.GetCultureInfo("en-IN");

    public static string Money(double v) => v.ToString("#,##0", Inr);

    public static ReportContent Compose(BuildingModel m, ModelReport r, IReadOnlyList<DrawingSheet> sheets)
    {
        var b = m.Brief;
        var site = m.Site;
        var sections = new List<ReportSection>
        {
            new("1. Project brief", [
                new KeyValueBlock([
                    ("Project", m.Name),
                    ("Plot", $"{Units.MmToFt(site.PlotWidth):0.#} x {Units.MmToFt(site.PlotDepth):0.#} ft ({site.PlotArea / 1e6:0.0} m²), {site.Facing}-facing"),
                    ("Floors", b.Floors == 1 ? "G" : $"G+{b.Floors - 1}, floor height {b.FloorHeight / 1000:0.00} m"),
                    ("Setbacks", $"Front {site.Setbacks.Front / 1000:0.00} m · Rear {site.Setbacks.Rear / 1000:0.00} m · Sides {site.Setbacks.Side / 1000:0.00} m"),
                    ("Built-up area", $"{m.BuiltUpArea() / 1e6:0.0} m² ({r.Cost.BuiltUpSqft:0} sq ft)"),
                    ("Vastu", b.Vastu ? $"On ({b.Strictness}), score {r.Vastu.Score:0}/100" : "Off"),
                    ("Exterior style", m.Exterior.Style.ToString()),
                    ("Materials", $"Concrete M{b.ConcreteFck:0}, steel Fe{b.SteelFy:0}, soil SBC {site.SoilSbc:0} kN/m², seismic zone {site.SeismicZone}"),
                    ("Estimated cost", $"{r.Cost.Currency} {Money(r.Cost.Total)} (≈ {Money(r.Cost.CostPerSqft)}/sq ft incl. GST)"),
                ]),
            ]),
        };

        var plans = sheets.Where(s => s.Name.StartsWith("A-1", StringComparison.Ordinal) || s.Name == "A-001").ToList();
        sections.Add(new ReportSection("2. Drawings", plans.Select(s => (ReportBlock)new SvgBlock(SvgWriter.Write(s, 900), 600, s.Title)).ToList(), true));
        var elevations = sheets.Where(s => s.Name.StartsWith("A-2", StringComparison.Ordinal) || s.Name.StartsWith("A-3", StringComparison.Ordinal)).ToList();
        sections.Add(new ReportSection("3. Elevations and section", elevations.Select(s => (ReportBlock)new SvgBlock(SvgWriter.Write(s, 900), 220, s.Title)).ToList(), true));

        sections.Add(new ReportSection("4. Vastu report", [
            new ParagraphBlock($"Score {r.Vastu.Score:0}/100 using '{(m.Brief.Vastu ? "default Vastu pack" : "Vastu off")}'. Rules are soft: trade-offs are listed, not hidden."),
            new TableBlock(["Floor", "Room", "Zone", "Fit", "Penalty"], r.Vastu.Rooms.Where(x => x.Fit != Engine.Vastu.VastuFit.NotApplicable)
                .Select(x => (IReadOnlyList<string>)[x.Level == 0 ? "G" : x.Level.ToString(), x.RoomName, x.Zone.ToString(), x.Fit.ToString(), $"{x.Penalty:0.#}"]).ToList()),
            new TableBlock(["Rule", "Finding", "Penalty"], r.Vastu.Rules.Select(x => (IReadOnlyList<string>)[x.RuleId, x.Message, $"{x.Penalty:0.#}"]).ToList()),
            .. r.Fixes.Select(f => (ReportBlock)new ParagraphBlock($"Suggestion: {f.Description}")),
        ], true));

        sections.Add(new ReportSection("5. Bylaw compliance", [
            new ParagraphBlock(r.Compliance.Disclaimer, true),
            new TableBlock(["Rule", "Check", "Required", "Actual", "Status"], r.Compliance.Items
                .Select(i => (IReadOnlyList<string>)[i.RuleId, i.Description, i.Required, i.Actual, i.Status.ToString()]).ToList()),
        ], true));

        var st = m.Structure;
        sections.Add(new ReportSection("6. Structure (preliminary)", [
            new ParagraphBlock(StructuralDrawer.Banner, true),
            .. st.Notes.Select(n => (ReportBlock)new ParagraphBlock(n)),
            .. st.Warnings.Select(w => (ReportBlock)new ParagraphBlock("Warning: " + w, true)),
            new TableBlock(["Column", "Size", "Pu (kN)", "Bars", "Ties"], st.Columns.Select(c => (IReadOnlyList<string>)[c.Mark, $"{c.B:0}x{c.D:0}", $"{c.Pu:0}", c.Reinforcement, c.Ties]).ToList()),
            new TableBlock(["Beam", "Size", "Span (m)", "Mu (kNm)", "Bottom", "Stirrups"], st.Beams.Where(x => x.Level == 1 || x.Level == m.Storeys.Count)
                .Select(x => (IReadOnlyList<string>)[x.Mark, $"{x.B:0}x{x.D:0}", $"{x.Start.DistanceTo(x.End) / 1000:0.00}", $"{x.Mu:0.0}", x.Bottom, x.Stirrups]).ToList()),
            new TableBlock(["Footing", "Size", "Depth", "Mesh"], st.Footings.Select(f => (IReadOnlyList<string>)[f.Mark, $"{f.L:0}x{f.B:0}", $"{f.Depth:0}", f.Reinforcement]).ToList()),
            new ParagraphBlock($"Steel from BBS: {st.Bbs.Sum(x => x.Weight):0} kg ({st.Bbs.Count} bar marks)."),
        ], true));

        var c = r.Cost;
        sections.Add(new ReportSection("7. Cost estimate", [
            new ParagraphBlock(c.Rates.Disclaimer, true),
            new TableBlock(["Group", $"Amount ({c.Currency})"], c.ByGroup().Select(g => (IReadOnlyList<string>)[g.Group, Money(g.Amount)]).ToList()),
            new KeyValueBlock([
                ("Subtotal", Money(c.Subtotal)), ($"Contingency {c.Rates.ContingencyPct}%", Money(c.Contingency)),
                ($"Contractor margin {c.Rates.ContractorMarginPct}%", Money(c.Margin)), ($"GST {c.Rates.GstPct}%", Money(c.Gst)),
                ("Total", Money(c.Total)), ("Per sq ft (built-up)", Money(c.CostPerSqft)),
            ]),
        ], true));

        sections.Add(new ReportSection("8. Interiors and electrical", [
            new TableBlock(["Floor", "Room", "Floor finish", "Lights", "Fans", "Sockets", "Power"],
                m.Storeys.SelectMany(s => s.Rooms.Select(room => (s, room))).Select(x =>
                {
                    var e = r.Electrical.FirstOrDefault(row => row.Room == x.room.Name && row.Level == x.s.Level);
                    return (IReadOnlyList<string>)[x.s.Level == 0 ? "G" : x.s.Level.ToString(), x.room.Name, x.room.FloorFinish,
                        $"{e?.Lights}", $"{e?.Fans}", $"{e?.Sockets}", $"{e?.PowerSockets}"];
                }).ToList()),
            new ParagraphBlock("Exterior materials: " + string.Join("; ", m.Exterior.MaterialNotes)),
        ], true));

        sections.Add(new ReportSection("9. Assumptions and limits", [
            new ParagraphBlock("AI proposes, deterministic rules verify, the engineer approves. This report is a design aid, not an approval drawing."),
            new ParagraphBlock("Layouts are rectangular rooms on a 150 mm module with a corridor spine; plot shapes other than rectangles are not supported yet."),
            new ParagraphBlock("Structural sizing is gravity-only and preliminary; lateral analysis, detailing and certification by a licensed engineer are required.", true),
        ]));

        return new ReportContent($"{m.Name} - design report", $"Generated {DateTime.Now:dd MMM yyyy} by AI Building Studio · {m.Metadata.GetValueOrDefault("option", "")}",
            "Engineer must verify: preliminary design, not for construction.", sections);
    }

    public static IReadOnlyList<string> ReviewFacts(BuildingModel m, ModelReport r)
    {
        var facts = new List<string>
        {
            $"Vastu score {r.Vastu.Score:0}/100.",
            $"Bylaw checks: {r.Compliance.Items.Count - r.Compliance.Failures - r.Compliance.Warnings} pass, {r.Compliance.Warnings} warnings, {r.Compliance.Failures} failures.",
            $"Estimated cost {r.Cost.Currency} {Money(r.Cost.Total)} ({Money(r.Cost.CostPerSqft)} per sq ft).",
        };
        facts.AddRange(r.Compliance.Items.Where(i => i.Status == ComplianceStatus.Fail).Take(5).Select(i => $"{i.Description}: required {i.Required}, actual {i.Actual}."));
        facts.AddRange(r.Vastu.Rooms.Where(x => x.Fit == Engine.Vastu.VastuFit.Avoid).Take(3).Select(x => x.Explanation));
        facts.AddRange(r.Fixes.Take(2).Select(f => f.Description));
        facts.AddRange(m.Structure.Warnings);
        return facts;
    }
}
