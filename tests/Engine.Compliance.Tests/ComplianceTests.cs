using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Compliance;

namespace BuildingStudio.Engine.Compliance.Tests;

public class ComplianceTests
{
    private static readonly BylawRulePack Pack = RuleRepository.Load().Bylaws;

    private static BuildingModel Sample()
    {
        var m = new BuildingModel();
        m.Brief.Site = new Site { PlotWidth = 9144, PlotDepth = 12192, Facing = CardinalSide.E, Setbacks = new Setbacks { Front = 1500, Rear = 1000, Side = 750 } };
        m.Brief.FloorHeight = 3000;
        var env = m.Site.Envelope();
        var s = new Storey { Level = 0, Height = 3000, Envelope = env };
        s.Rooms.Add(new Room { Type = RoomType.Living, Name = "Living", Bounds = new Rect2(env.X, env.Y, 3600, 4200) });
        s.Rooms.Add(new Room { Type = RoomType.Bedroom, Name = "Bedroom", Bounds = new Rect2(env.X + 3600, env.Y, 2000, 4200) });
        s.Rooms.Add(new Room { Type = RoomType.Staircase, Name = "Stair", Bounds = new Rect2(env.X, env.Y + 4200, 2400, 4200) });
        m.Storeys.Add(s);
        return m;
    }

    [Fact]
    public void Setbacks_pass_for_bylaw_values()
    {
        var r = new ComplianceEngine(Pack).Evaluate(Sample());
        r.Items.Where(i => i.RuleId.StartsWith("B-SETBACK")).Should().OnlyContain(i => i.Status == ComplianceStatus.Pass);
    }

    [Fact]
    public void Narrow_bedroom_fails_min_width()
    {
        var r = new ComplianceEngine(Pack).Evaluate(Sample());
        r.Items.Single(i => i.RuleId == "B-MIN-WIDTH" && i.Description.StartsWith("Bedroom")).Status.Should().Be(ComplianceStatus.Fail);
        r.Items.Single(i => i.RuleId == "B-MIN-AREA" && i.Description.StartsWith("Living")).Status.Should().Be(ComplianceStatus.Pass);
    }

    [Fact]
    public void Far_and_coverage_are_computed()
    {
        // Built-up = 3.6x4.2 + 2.0x4.2 + 2.4x4.2 = 33.6 m²; plot = 111.48 m² -> FAR 0.30, coverage 30.1%.
        var r = new ComplianceEngine(Pack).Evaluate(Sample());
        r.Items.Single(i => i.RuleId == "B-FAR").Actual.Should().Be("0.3");
        r.Items.Single(i => i.RuleId == "B-COVERAGE").Actual.Should().Be("30.14 %");
    }

    [Fact]
    public void Stair_geometry_golden()
    {
        // 3000 / 190 = 15.8 -> 16 risers of 187.5 mm; flight width (2400 - 100) / 2 = 1150;
        // 7 treads per flight x 250 + 1150 landing = 2900 <= 4200.
        var g = StairGeometry.For(new Rect2(0, 0, 2400, 4200), 3000, Pack.Stair);
        g.Risers.Should().Be(16);
        g.Riser.Should().Be(187.5);
        g.FlightWidth.Should().Be(1150);
        g.RequiredLength.Should().Be(2900);
    }

    [Fact]
    public void Missing_parking_is_a_warning()
    {
        var r = new ComplianceEngine(Pack).Evaluate(Sample());
        r.Items.Single(i => i.RuleId == "B-PARKING").Status.Should().Be(ComplianceStatus.Warning);
    }

    [Fact]
    public void Height_limit_fails_when_exceeded()
    {
        var m = Sample();
        for (var i = 1; i < 5; i++) m.Storeys.Add(new Storey { Level = i, Elevation = i * 3000, Height = 3000, Envelope = m.Storeys[0].Envelope });
        new ComplianceEngine(Pack).Evaluate(m).Items.Single(i => i.RuleId == "B-HEIGHT").Status.Should().Be(ComplianceStatus.Fail);
    }
}
