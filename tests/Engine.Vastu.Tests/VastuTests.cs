using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Vastu;

namespace BuildingStudio.Engine.Vastu.Tests;

public class VastuTests
{
    private static readonly VastuEngine Engine = new(RuleRepository.Load().Vastu);
    private static readonly Rect2 Env = new(0, 0, 9000, 9000);

    /// <summary>Rect of a 3x3 cell for a zone in a 9 m square envelope with north up.</summary>
    private static Rect2 Cell(Direction z)
    {
        var (c, r) = z switch
        {
            Direction.SW => (0, 0), Direction.S => (1, 0), Direction.SE => (2, 0),
            Direction.W => (0, 1), Direction.Centre => (1, 1), Direction.E => (2, 1),
            Direction.NW => (0, 2), Direction.N => (1, 2), _ => (2, 2),
        };
        return new Rect2(c * 3000, r * 3000, 3000, 3000);
    }

    private static Room R(RoomType t, Direction z, string? name = null) =>
        new() { Type = t, Name = name ?? t.ToString(), Bounds = Cell(z) };

    [Theory]
    [InlineData(Direction.SW)] [InlineData(Direction.S)] [InlineData(Direction.SE)]
    [InlineData(Direction.W)] [InlineData(Direction.Centre)] [InlineData(Direction.E)]
    [InlineData(Direction.NW)] [InlineData(Direction.N)] [InlineData(Direction.NE)]
    public void Grid_maps_cells_to_zones(Direction z) => new ZoneGrid(Env, 0).ZoneOf(Cell(z)).Should().Be(z);

    [Fact]
    public void Grid_rotates_with_north_angle()
    {
        // North rotated 90° counter-clockwise: plan -X is true north, so the plan W cell is the N zone
        // and the plan NE corner (+X = south, +Y = east) is SE.
        new ZoneGrid(Env, Math.PI / 2).ZoneOf(Cell(Direction.W)).Should().Be(Direction.N);
        new ZoneGrid(Env, Math.PI / 2).ZoneOf(Cell(Direction.NE)).Should().Be(Direction.SE);
    }

    // One row per line of MASTER_PLAN section 6.2: (room, preferred zone, avoid zone).
    [Theory]
    [InlineData(RoomType.Living, Direction.NE, Direction.SW)]
    [InlineData(RoomType.Kitchen, Direction.SE, Direction.NE)]
    [InlineData(RoomType.Kitchen, Direction.SE, Direction.Centre)]
    [InlineData(RoomType.MasterBedroom, Direction.SW, Direction.NE)]
    [InlineData(RoomType.Bedroom, Direction.W, Direction.SE)]
    [InlineData(RoomType.Pooja, Direction.NE, Direction.S)]
    [InlineData(RoomType.Dining, Direction.W, Direction.SW)]
    [InlineData(RoomType.Study, Direction.W, Direction.SW)]
    [InlineData(RoomType.Bath, Direction.NW, Direction.NE)]
    [InlineData(RoomType.Bath, Direction.W, Direction.Centre)]
    [InlineData(RoomType.Staircase, Direction.S, Direction.NE)]
    [InlineData(RoomType.Staircase, Direction.SW, Direction.Centre)]
    [InlineData(RoomType.Store, Direction.NW, Direction.NE)]
    [InlineData(RoomType.Parking, Direction.NW, Direction.SW)]
    public void Table_rows(RoomType type, Direction preferred, Direction avoid)
    {
        var grid = new ZoneGrid(Env, 0);
        var good = Engine.EvaluateRoom(R(type, preferred), 0, grid, VastuStrictness.Moderate);
        var bad = Engine.EvaluateRoom(R(type, avoid), 0, grid, VastuStrictness.Moderate);
        good.Fit.Should().Be(VastuFit.Preferred);
        good.Penalty.Should().Be(0);
        bad.Fit.Should().Be(VastuFit.Avoid);
        bad.Penalty.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Penalty_is_base_times_weight_times_strictness()
    {
        // Kitchen weight 1.2, avoid base 12, strict 1.5 -> 21.6
        var r = Engine.EvaluateRoom(R(RoomType.Kitchen, Direction.NE), 0, new ZoneGrid(Env, 0), VastuStrictness.Strict);
        r.Penalty.Should().BeApproximately(21.6, 1e-9);
    }

    private static BuildingModel HandDrawnSample(VastuStrictness s = VastuStrictness.Moderate)
    {
        var m = new BuildingModel();
        m.Brief.Strictness = s;
        var st = new Storey { Envelope = Env };
        st.Rooms.AddRange([
            R(RoomType.Kitchen, Direction.SE), R(RoomType.MasterBedroom, Direction.SW), R(RoomType.Pooja, Direction.NE),
            R(RoomType.Living, Direction.N), R(RoomType.Bath, Direction.Centre), R(RoomType.Bedroom, Direction.E),
            R(RoomType.Store, Direction.NW), R(RoomType.Dining, Direction.W), R(RoomType.Staircase, Direction.S),
        ]);
        m.Storeys.Add(st);
        return m;
    }

    [Fact]
    public void Gate1_hand_drawn_plan_matches_manual_check()
    {
        // Manual: Bath in Centre = avoid 12 x 0.8 = 9.6; Bedroom in E = acceptable 3 x 0.8 = 2.4; rest preferred.
        // Score = 100 - 12.0 = 88.
        var report = Engine.Evaluate(HandDrawnSample());
        report.Score.Should().Be(88);
        report.Rooms.Single(r => r.Type == RoomType.Bath).Penalty.Should().BeApproximately(9.6, 1e-9);
        report.Rooms.Single(r => r.Type == RoomType.Bedroom).Fit.Should().Be(VastuFit.Acceptable);
        Engine.Evaluate(HandDrawnSample(VastuStrictness.Strict)).Score.Should().Be(82);
    }

    [Fact]
    public void Explanation_names_the_occupant_of_the_preferred_zone()
    {
        var m = HandDrawnSample();
        var st = m.Storeys[0];
        var kitchen = st.Rooms.First(r => r.Type == RoomType.Kitchen);
        var bedroom = st.Rooms.First(r => r.Type == RoomType.Bedroom);
        (kitchen.Bounds, bedroom.Bounds) = (bedroom.Bounds, kitchen.Bounds);
        var r = Engine.Evaluate(m).Rooms.Single(x => x.Type == RoomType.Kitchen);
        r.Explanation.Should().Contain("Kitchen in E").And.Contain("Preferred: SE").And.Contain("occupied by Bedroom");
    }

    [Fact]
    public void Pooja_next_to_toilet_is_penalised()
    {
        var st = new Storey { Envelope = Env };
        st.Rooms.Add(R(RoomType.Pooja, Direction.NE));
        st.Rooms.Add(R(RoomType.Bath, Direction.N));
        var (_, hits) = Engine.EvaluateStoreyDetail(st, new Site(), VastuStrictness.Moderate);
        hits.Should().ContainSingle(h => h.RuleId == "V-X01");
    }

    [Fact]
    public void Main_entrance_zone_is_scored()
    {
        var st = new Storey { Envelope = Env };
        var living = R(RoomType.Living, Direction.NE);
        st.Rooms.Add(living);
        var wall = new Wall { Start = new(9000, 6000), End = new(9000, 9000), Type = WallType.Exterior, RoomA = living.Id };
        st.Walls.Add(wall);
        st.Openings.Add(new Opening { WallId = wall.Id, Offset = 1000, Width = 1050, Height = 2100, Kind = OpeningKind.Door, IsMainEntrance = true });
        var (_, hits) = Engine.EvaluateStoreyDetail(st, new Site(), VastuStrictness.Moderate);
        hits.Single(h => h.RuleId == "V-ENT").Penalty.Should().Be(0);
    }

    [Fact]
    public void Suggests_swap_that_fixes_kitchen_and_pooja()
    {
        var m = new BuildingModel();
        var st = new Storey { Envelope = Env };
        st.Rooms.Add(new Room { Type = RoomType.Kitchen, Name = "Kitchen", Bounds = Cell(Direction.NE), TargetArea = 9e6 });
        st.Rooms.Add(new Room { Type = RoomType.Pooja, Name = "Pooja", Bounds = Cell(Direction.SE), TargetArea = 9e6 });
        m.Storeys.Add(st);
        var fixes = Engine.SuggestFixes(m, 0);
        fixes.Should().NotBeEmpty();
        fixes[0].Description.Should().StartWith("Swap Kitchen and Pooja");
        fixes[0].VastuGain.Should().BeGreaterThan(20);
    }

    [Fact]
    public void Locked_rooms_are_not_suggested()
    {
        var m = new BuildingModel();
        var st = new Storey { Envelope = Env };
        st.Rooms.Add(new Room { Type = RoomType.Kitchen, Name = "Kitchen", Bounds = Cell(Direction.NE), Locked = true });
        st.Rooms.Add(new Room { Type = RoomType.Pooja, Name = "Pooja", Bounds = Cell(Direction.SE) });
        m.Storeys.Add(st);
        Engine.SuggestFixes(m, 0).Should().BeEmpty();
    }

    [Fact]
    public void Site_items_scored_on_plot_grid()
    {
        var m = HandDrawnSample();
        m.Brief.Site.PlotWidth = 9000;
        m.Brief.Site.PlotDepth = 9000;
        m.Exterior.SiteItems.Add(new SiteItem { Kind = "Sump", Name = "Sump", Bounds = new Rect2(8000, 8000, 800, 800) });
        m.Exterior.SiteItems.Add(new SiteItem { Kind = "Septic", Name = "Septic", Bounds = new Rect2(8000, 8000, 800, 800) });
        var hits = Engine.Evaluate(m).Rules;
        hits.Single(h => h.RuleId == "V-SITE-Sump").Penalty.Should().Be(0);
        hits.Single(h => h.RuleId == "V-SITE-Septic").Penalty.Should().BeGreaterThan(0);
    }
}
