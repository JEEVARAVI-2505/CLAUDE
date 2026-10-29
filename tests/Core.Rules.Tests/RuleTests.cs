using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;

namespace BuildingStudio.Core.Rules.Tests;

public class RuleTests
{
    private static readonly RuleRepository Rules = RuleRepository.Load();

    [Fact]
    public void Loads_every_pack()
    {
        Rules.Vastu.Rooms.Should().ContainKey(RoomType.Kitchen);
        Rules.Vastu.Rooms[RoomType.Kitchen].Preferred.Should().Equal(Direction.SE);
        Rules.Bylaws.MinRoomSizes[RoomType.Living].AreaSqm.Should().Be(9.5);
        Rules.Rates.Get("STEEL").Unit.Should().Be("kg");
        Rules.Furniture.Find("bed-king").Should().NotBeNull();
        Rules.Styles.Styles.Should().HaveCount(4);
    }

    [Fact]
    public void Every_template_item_exists_in_catalog()
    {
        foreach (var ids in Rules.Furniture.Templates.Values.SelectMany(v => v).SelectMany(v => v.Split('|')))
            Rules.Furniture.Find(ids).Should().NotBeNull(ids);
    }

    [Fact]
    public void Plan_table_rows_are_encoded()
    {
        // MASTER_PLAN 6.2 table spot checks.
        Rules.Vastu.Rooms[RoomType.MasterBedroom].Preferred.Should().Equal(Direction.SW);
        Rules.Vastu.Rooms[RoomType.Pooja].Preferred.Should().Equal(Direction.NE);
        Rules.Vastu.Rooms[RoomType.Bath].Avoid.Should().Contain([Direction.NE, Direction.SW, Direction.Centre]);
        Rules.Vastu.Rooms[RoomType.Staircase].Avoid.Should().Contain([Direction.NE, Direction.Centre]);
        Rules.Vastu.SiteItems["Sump"].Preferred.Should().Equal(Direction.NE);
    }

    [Theory]
    [InlineData(90, 1.5, 0)]
    [InlineData(111.5, 1.5, 0.75)]
    [InlineData(300, 3.0, 1.0)]
    [InlineData(5000, 3.0, 1.5)]
    public void Setback_band_lookup(double area, double front, double side)
    {
        var b = Rules.Bylaws.SetbacksFor(area);
        b.FrontM.Should().Be(front);
        b.SideM.Should().Be(side);
    }

    [Fact]
    public void Strictness_factor() => Rules.Vastu.Strictness.For(VastuStrictness.Strict).Should().Be(1.5);
}
