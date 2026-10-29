using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Brief;
using BuildingStudio.Engine.Drawing;
using BuildingStudio.Engine.Exterior;
using BuildingStudio.Engine.Layout;
using BuildingStudio.Engine.Vastu;

namespace BuildingStudio.Engine.Exterior.Tests;

public class ExteriorTests
{
    private static readonly RuleRepository Rules = RuleRepository.Load();

    private static BuildingModel Sample(Func<Core.Model.Brief> create)
    {
        var brief = create();
        var rear = brief.Site.Setbacks.Rear;
        new BriefValidator(Rules.Bylaws).ApplyBylawSetbacks(brief);
        brief.Site.Setbacks.Rear = Math.Max(rear, brief.Site.Setbacks.Rear);
        var o = new LayoutGenerator(new VastuEngine(Rules.Vastu), Rules.Bylaws).Generate(brief, 1)[0];
        return new BuildingModel { Brief = brief, Storeys = o.Storeys.ToList() };
    }

    [Fact]
    public void Gate7_four_styles_give_distinct_elevations()
    {
        var m = Sample(BriefPresets.North30x50ThreeBhk);
        var engine = new ExteriorEngine(Rules.Styles, Rules.Vastu);
        var svgs = new List<string>();
        var specs = new List<ExteriorSpec>();
        foreach (var style in Enum.GetValues<ExteriorStyle>())
        {
            specs.Add(ProjectSerializer.Clone(engine.Generate(m, style)));
            var sheets = new[] { CardinalSide.N, CardinalSide.E, CardinalSide.S, CardinalSide.W }.Select(s => ElevationDrawer.Draw(m, s)).ToList();
            sheets.Should().OnlyContain(s => s.Entities.Count > 10);
            svgs.Add(string.Concat(sheets.Select(s => SvgWriter.Write(s))));
        }
        svgs.Distinct().Should().HaveCount(4);
        specs.Select(s => (s.WallColor, s.Roof, s.CorniceBand)).Distinct().Should().HaveCount(4);
        specs.Should().Contain(s => s.Roof == RoofForm.SlopedTile);
    }

    [Fact]
    public void Site_items_are_outside_the_building_and_inside_the_plot()
    {
        var m = Sample(BriefPresets.West40x60FourBhk);
        var spec = new ExteriorEngine(Rules.Styles, Rules.Vastu).Generate(m);
        var building = m.Storeys[0].Envelope;
        foreach (var i in spec.SiteItems.Where(i => i.Kind is "Sump" or "Septic" or "RWH" or "Tree"))
        {
            m.Site.Plot.Contains(i.Bounds).Should().BeTrue(i.Name);
            i.Bounds.Overlaps(building).Should().BeFalse(i.Name);
        }
        spec.SiteItems.Should().Contain(i => i.Kind == "Gate").And.Contain(i => i.Kind == "CompoundWall").And.Contain(i => i.Kind == "Sump");
    }

    [Fact]
    public void Sump_prefers_north_east_when_space_allows()
    {
        // 3 m rear (east) setback leaves room in the north-east.
        var m = Sample(() =>
        {
            var b = BriefPresets.West40x60FourBhk();
            b.Site.Setbacks.Rear = 3000;
            return b;
        });
        m.Brief.Site.Setbacks.Rear = 3000;
        var spec = new ExteriorEngine(Rules.Styles, Rules.Vastu).Generate(m);
        var sump = spec.SiteItems.Single(i => i.Kind == "Sump");
        var zone = new ZoneGrid(m.Site.Plot, 0).ZoneOf(sump.Bounds);
        Rules.Vastu.SiteItems["Sump"].Preferred.Concat(Rules.Vastu.SiteItems["Sump"].Acceptable).Should().Contain(zone);
    }

    [Fact]
    public void Coastal_note_added_for_high_wind()
    {
        var m = Sample(BriefPresets.East30x40TwoBhk);
        new ExteriorEngine(Rules.Styles, Rules.Vastu).Generate(m).MaterialNotes.Should().Contain(n => n.Contains("Coastal"));
    }
}
