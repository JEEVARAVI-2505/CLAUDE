using System.Diagnostics;
using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Brief;
using BuildingStudio.Engine.Layout;
using BuildingStudio.Engine.Vastu;
using Xunit.Abstractions;

namespace BuildingStudio.Engine.Layout.Tests;

public class LayoutTests(ITestOutputHelper output)
{
    private static readonly RuleRepository Rules = RuleRepository.Load();
    private static readonly LayoutGenerator Generator = new(new VastuEngine(Rules.Vastu), Rules.Bylaws);

    private static Core.Model.Brief Prepared(Func<Core.Model.Brief> create)
    {
        var b = create();
        new BriefValidator(Rules.Bylaws).ApplyBylawSetbacks(b);
        return b;
    }

    public static IEnumerable<object[]> Presets() => BriefPresets.All.Select((p, i) => new object[] { i, p.Name });

    [Theory]
    [MemberData(nameof(Presets))]
    public void Gate2_presets_give_three_valid_options(int index, string name)
    {
        var brief = Prepared(BriefPresets.All[index].Create);
        var sw = Stopwatch.StartNew();
        var options = Generator.Generate(brief, 5, seed: 1);
        sw.Stop();
        output.WriteLine($"{name}: {options.Count} options in {sw.ElapsedMilliseconds} ms");

        options.Count.Should().BeGreaterThanOrEqualTo(3, name);
        (sw.Elapsed.TotalSeconds / options.Count).Should().BeLessThan(10, "benchmark < 10 s per option");
        foreach (var o in options)
        {
            output.WriteLine($"  {o.Name} fitness {o.Metrics.Fitness:0.0}: {o.Summary}");
            o.Storeys.Should().HaveCount(brief.Floors);
            foreach (var s in o.Storeys)
            {
                PlanEditor.IsValidTiling(s).Should().BeTrue($"{name} {o.Name} {s.Name} has no overlaps or gaps");
                LayoutScorer.Unreachable(s).Should().Be(0, $"{name} {o.Name} {s.Name}: all rooms reachable");
                s.Walls.Should().NotBeEmpty();
                s.Openings.Should().Contain(x => x.Kind == OpeningKind.Window);
            }
            o.Storeys[0].Openings.Should().ContainSingle(x => x.IsMainEntrance);
            o.Metrics.Vastu.Should().BeInRange(0, 100);
            var requested = brief.Rooms.Sum(r => r.Count);
            o.Storeys.SelectMany(s => s.Rooms).Count(r => r.Type is not (RoomType.Passage or RoomType.Staircase))
                .Should().BeGreaterThanOrEqualTo(requested);
        }
        options.Select(o => o.Metrics.Fitness).Should().BeInDescendingOrder();
    }

    [Fact]
    public void Same_seed_gives_same_options()
    {
        var a = Generator.Generate(Prepared(BriefPresets.East30x40TwoBhk), 3, seed: 5);
        var b = Generator.Generate(Prepared(BriefPresets.East30x40TwoBhk), 3, seed: 5);
        a.Select(o => o.Metrics.Fitness).Should().Equal(b.Select(o => o.Metrics.Fitness));
        a[0].Storeys[0].Rooms.Select(r => (r.Name, r.Bounds)).Should().Equal(b[0].Storeys[0].Rooms.Select(r => (r.Name, r.Bounds)));
    }

    [Fact]
    public void Multi_floor_stairs_align()
    {
        var o = Generator.Generate(Prepared(BriefPresets.North30x50ThreeBhk), 3)[0];
        var stairs = o.Storeys.Select(s => s.Rooms.Single(r => r.Type == RoomType.Staircase).Bounds).Distinct().ToList();
        stairs.Should().ContainSingle("staircase is in the same place on every floor");
    }

    [Fact]
    public void Attached_baths_open_into_their_bedroom()
    {
        var o = Generator.Generate(Prepared(BriefPresets.West40x60FourBhk), 3)[0];
        foreach (var s in o.Storeys)
            foreach (var bath in s.Rooms.Where(r => r.ParentId is not null))
            {
                var parent = s.FindRoom(bath.ParentId!.Value)!;
                bath.Bounds.SharedEdgeLength(parent.Bounds).Should().BeGreaterThan(0, $"{bath.Name} touches {parent.Name}");
            }
    }

    [Fact]
    public void Vastu_off_still_generates()
    {
        var b = Prepared(BriefPresets.East30x40TwoBhk);
        b.Vastu = false;
        Generator.Generate(b, 3).Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Resolve_keeps_locked_room_zone()
    {
        var brief = Prepared(BriefPresets.East30x40TwoBhk);
        var option = Generator.Generate(brief, 3)[2];
        var model = new BuildingModel { Brief = brief, Storeys = option.Storeys.ToList() };
        var living = model.Storeys[0].Rooms.First(r => r.Type == RoomType.Living);
        living.Locked = true;
        var zone = new ZoneGrid(model.Storeys[0].Envelope, 0).ZoneOf(living.Bounds);
        var resolved = Generator.Resolve(model, 2);
        foreach (var o in resolved)
        {
            var l = o.Storeys[0].Rooms.First(r => r.Name == living.Name);
            new ZoneGrid(o.Storeys[0].Envelope, 0).ZoneOf(l.Bounds).Should().Be(zone);
            l.Locked.Should().BeTrue();
        }
    }

    private static Storey Grid2x2()
    {
        var s = new Storey { Envelope = new Rect2(0, 0, 8000, 6000) };
        s.Rooms.Add(new Room { Name = "A", Bounds = new Rect2(0, 0, 4000, 3000) });
        s.Rooms.Add(new Room { Name = "B", Bounds = new Rect2(4000, 0, 4000, 3000) });
        s.Rooms.Add(new Room { Name = "C", Bounds = new Rect2(0, 3000, 4000, 3000) });
        s.Rooms.Add(new Room { Name = "D", Bounds = new Rect2(4000, 3000, 4000, 3000) });
        return s;
    }

    [Fact]
    public void Move_shared_wall_moves_the_connected_wall_segment()
    {
        var s = Grid2x2();
        var a = s.Rooms[0];
        var result = PlanEditor.MoveEdge(s, a.Id, RectSide.Right, 4520)!;
        result.Keys.Should().BeEquivalentTo([s.Rooms[0].Id, s.Rooms[1].Id], "only rooms sharing the A|B wall move; C|D keep theirs (T-junction)");
        result[a.Id].Width.Should().Be(4500, "snapped to 150 mm module");
        foreach (var (id, r) in result) s.FindRoom(id)!.Bounds = r;
        PlanEditor.IsValidTiling(s).Should().BeTrue();
    }

    [Fact]
    public void Move_edge_rejects_exterior_too_small_and_locked()
    {
        var s = Grid2x2();
        PlanEditor.MoveEdge(s, s.Rooms[0].Id, RectSide.Left, 300).Should().BeNull("exterior wall");
        PlanEditor.MoveEdge(s, s.Rooms[0].Id, RectSide.Right, 7500).Should().BeNull("B would be 450 mm wide");
        s.Rooms[1].Locked = true;
        PlanEditor.MoveEdge(s, s.Rooms[0].Id, RectSide.Right, 4500).Should().BeNull("B is locked");
    }

    [Fact]
    public void Move_edge_follows_a_continuous_wall_through_several_rooms()
    {
        var s = new Storey { Envelope = new Rect2(0, 0, 8000, 6000) };
        s.Rooms.Add(new Room { Name = "Tall", Bounds = new Rect2(0, 0, 4000, 6000) });
        s.Rooms.Add(new Room { Name = "B", Bounds = new Rect2(4000, 0, 4000, 3000) });
        s.Rooms.Add(new Room { Name = "D", Bounds = new Rect2(4000, 3000, 4000, 3000) });
        var r = PlanEditor.MoveEdge(s, s.Rooms[1].Id, RectSide.Left, 3600)!;
        r.Should().HaveCount(3);
        foreach (var (id, b) in r) s.FindRoom(id)!.Bounds = b;
        PlanEditor.IsValidTiling(s).Should().BeTrue();
    }

    [Fact]
    public void Swap_exchanges_bounds()
    {
        var s = Grid2x2();
        var r = PlanEditor.Swap(s, s.Rooms[0].Id, s.Rooms[3].Id)!;
        r[s.Rooms[0].Id].Should().Be(new Rect2(4000, 3000, 4000, 3000));
        s.Rooms[0].Locked = true;
        PlanEditor.Swap(s, s.Rooms[0].Id, s.Rooms[3].Id).Should().BeNull();
    }

    [Fact]
    public void Detailer_creates_interior_and_exterior_walls()
    {
        var s = Grid2x2();
        s.Rooms.ForEach(r => r.Type = RoomType.Bedroom);
        s.Rooms[0].Type = RoomType.Passage;
        PlanDetailer.Detail(s, new Core.Model.Brief());
        s.Walls.Count(w => w.Type == WallType.Interior).Should().Be(4);
        s.Walls.Count(w => w.Type == WallType.Exterior).Should().Be(8);
        s.Walls.Where(w => w.Type == WallType.Exterior).Sum(w => w.Length).Should().Be(28000);
    }
}
