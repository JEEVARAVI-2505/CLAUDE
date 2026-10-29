using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Engine.Brief;
using BuildingStudio.Engine.Structure;

namespace BuildingStudio.Engine.Structure.Tests;

public class StructureEngineTests
{
    [Fact]
    public void Generates_columns_on_walls_never_inside_rooms()
    {
        var m = TestModels.FromPreset(BriefPresets.North30x50ThreeBhk);
        var st = new StructureEngine().Generate(m);
        st.Columns.Should().NotBeEmpty();
        var walls = m.Storeys[0].Walls.Select(w => w.Segment).ToList();
        foreach (var c in st.Columns)
        {
            walls.Any(w => w.ContainsPoint(c.Position, 2)).Should().BeTrue($"{c.Mark} sits on a wall");
            m.Storeys[0].Rooms.Any(r => r.Bounds.Inflate(-5).Contains(c.Position, 0)).Should().BeFalse($"{c.Mark} is not inside a room");
        }
        st.Footings.Should().HaveCount(st.Columns.Count);
        st.Beams.Should().Contain(b => b.Level == 0).And.Contain(b => b.Level == m.Storeys.Count);
    }

    [Fact]
    public void Column_spacing_along_beams_within_six_metres()
    {
        var m = TestModels.FromPreset(BriefPresets.West40x60FourBhk);
        var st = new StructureEngine().Generate(m);
        st.Beams.Should().OnlyContain(b => new Segment2(b.Start, b.End).Length <= 6000);
    }

    [Fact]
    public void Designs_are_populated_and_code_limits_hold()
    {
        var m = TestModels.FromPreset(BriefPresets.North30x50ThreeBhk);
        var st = new StructureEngine().Generate(m);
        foreach (var c in st.Columns)
        {
            c.Pu.Should().BeGreaterThan(0);
            c.AscRequired.Should().BeGreaterThanOrEqualTo(0.008 * c.B * c.D - 1e-6);
            Is456.ColumnCapacity(c.B * c.D, c.AscRequired, 20, 500).Should().BeGreaterThanOrEqualTo(c.Pu * 1000 - 1);
            Math.Min(c.B, c.D).Should().BeGreaterThanOrEqualTo(300, "IS 13920 minimum dimension in zone III");
        }
        st.Beams.Should().OnlyContain(b => b.AstRequired > 0 && b.Stirrups.Length > 0);
        st.Slabs.Should().OnlyContain(s => s.Thickness >= 120 && s.Reinforcement.Length > 0);
        st.Bbs.Sum(r => r.Weight).Should().BeGreaterThan(500);
        st.SeismicBaseShear.Should().BeGreaterThan(0);
        st.Notes.Should().Contain(n => n.Contains("IS 1893"));
    }

    [Fact]
    public void Heavier_building_gets_heavier_columns()
    {
        var g1 = TestModels.FromPreset(BriefPresets.East30x40TwoBhk);
        var g2 = TestModels.FromPreset(BriefPresets.North30x50ThreeBhk);
        var a = new StructureEngine().Generate(g1).Columns.Max(c => c.Pu);
        var b = new StructureEngine().Generate(g2).Columns.Max(c => c.Pu);
        b.Should().BeGreaterThan(a);
    }
}
