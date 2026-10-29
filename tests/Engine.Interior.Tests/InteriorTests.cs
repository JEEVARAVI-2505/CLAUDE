using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Brief;
using BuildingStudio.Engine.Interior;
using BuildingStudio.Engine.Layout;
using BuildingStudio.Engine.Vastu;

namespace BuildingStudio.Engine.Interior.Tests;

public class InteriorTests
{
    private static readonly RuleRepository Rules = RuleRepository.Load();
    private static readonly InteriorEngine Engine = new(Rules.Furniture);

    /// <summary>A room next to a passage with a 900 door near the start of the shared wall.</summary>
    private static (Storey, Room) Setup(RoomType type, double w, double d)
    {
        var s = new Storey { Envelope = new Rect2(0, 0, w + 1050, d) };
        var room = new Room { Type = type, Name = type.ToString(), Bounds = new Rect2(0, 0, w, d) };
        var passage = new Room { Type = RoomType.Passage, Name = "Passage", Bounds = new Rect2(w, 0, 1050, d) };
        s.Rooms.AddRange([room, passage]);
        PlanDetailer.Detail(s, new Core.Model.Brief());
        return (s, room);
    }

    public static IEnumerable<object[]> RoomCases() =>
    [
        [RoomType.MasterBedroom, 3600, 4200, "bed-"], [RoomType.Bedroom, 3000, 3600, "bed-"],
        [RoomType.Living, 3600, 4800, "sofa-"], [RoomType.Lounge, 3000, 3600, "sofa-"],
        [RoomType.Dining, 3000, 3300, "dining-"], [RoomType.Kitchen, 2400, 3000, "kitchen-counter"],
        [RoomType.Study, 2400, 3000, "study-desk"], [RoomType.Pooja, 1500, 1500, "pooja-unit"],
        [RoomType.Bath, 1500, 2400, "wc"], [RoomType.Store, 1800, 2000, "store-rack"],
        [RoomType.Utility, 1500, 1800, "washing-machine"], [RoomType.Parking, 3000, 5400, "car"],
        [RoomType.Balcony, 1500, 3000, "planter"],
    ];

    [Theory]
    [MemberData(nameof(RoomCases))]
    public void Gate6_every_room_type_furnishes_without_clashes(RoomType type, double w, double d, string essential)
    {
        var (s, room) = Setup(type, w, d);
        var placer = new FurniturePlacer(s, room, 0);
        placer.Prepare();
        var items = Engine.FurnishRoom(s, room, 0);

        items.Should().Contain(i => i.CatalogId.StartsWith(essential), $"{type} gets its main item");
        foreach (var i in items)
        {
            room.Bounds.Inflate(-FurniturePlacer.WallInset).Contains(i.Bounds).Should().BeTrue($"{i.Name} stays inside {type}");
            placer.DoorZones.Any(z => z.Overlaps(i.Bounds)).Should().BeFalse($"{i.Name} keeps the door swing clear");
        }
        for (var a = 0; a < items.Count; a++)
            for (var b = a + 1; b < items.Count; b++)
                items[a].Bounds.Overlaps(items[b].Bounds).Should().BeFalse($"{items[a].Name} and {items[b].Name} must not clash");
    }

    [Fact]
    public void Bed_head_goes_to_south_or_west_wall()
    {
        var (s, room) = Setup(RoomType.MasterBedroom, 3600, 4200);
        var bed = Engine.FurnishRoom(s, room, 0).First(i => i.CatalogId.StartsWith("bed-"));
        var inner = room.Bounds.Inflate(-FurniturePlacer.WallInset);
        (Tolerance.Near(bed.Bounds.MinY, inner.MinY) || Tolerance.Near(bed.Bounds.MinX, inner.MinX)).Should().BeTrue();
    }

    [Fact]
    public void Whole_preset_house_furnishes_without_clashes()
    {
        var brief = BriefPresets.North30x50ThreeBhk();
        new BriefValidator(Rules.Bylaws).ApplyBylawSetbacks(brief);
        var o = new LayoutGenerator(new VastuEngine(Rules.Vastu), Rules.Bylaws).Generate(brief, 1)[0];
        var m = new BuildingModel { Brief = brief, Storeys = o.Storeys.ToList() };
        Engine.Furnish(m);
        foreach (var s in m.Storeys)
            for (var a = 0; a < s.Furniture.Count; a++)
                for (var b = a + 1; b < s.Furniture.Count; b++)
                    s.Furniture[a].Bounds.Overlaps(s.Furniture[b].Bounds).Should().BeFalse();
        m.Storeys.SelectMany(s => s.Furniture).Should().Contain(f => f.CatalogId.StartsWith("bed-"));
    }

    [Fact]
    public void Lumen_method_golden()
    {
        // Living 4.0 x 5.0 = 20 m², 150 lux, 1200 lm, UF·MF 0.5 → 20·150 / 600 = 5 lights; fans round(20/12) = 2.
        var m = new BuildingModel();
        var s = new Storey { Envelope = new Rect2(0, 0, 4000, 5000) };
        s.Rooms.Add(new Room { Type = RoomType.Living, Name = "Living", Bounds = new Rect2(0, 0, 4000, 5000) });
        m.Storeys.Add(s);
        var row = Engine.Electrical(m).Single();
        row.Lights.Should().Be(5);
        row.Fans.Should().Be(2);
        row.Sockets.Should().Be(6);
    }

    [Fact]
    public void Finishes_come_from_catalog()
    {
        var m = new BuildingModel();
        m.Storeys.Add(new Storey { Rooms = [new Room { Type = RoomType.Kitchen }] });
        Engine.ApplyFinishes(m);
        m.Storeys[0].Rooms[0].FloorFinish.Should().Be("Anti-skid ceramic");
    }
}
