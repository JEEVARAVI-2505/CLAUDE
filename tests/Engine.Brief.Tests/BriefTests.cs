using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Brief;

namespace BuildingStudio.Engine.Brief.Tests;

public class BriefTests
{
    private static readonly BriefValidator Validator = new(RuleRepository.Load().Bylaws);

    [Fact]
    public void Parses_the_vision_brief()
    {
        var r = BriefTextParser.Parse(BriefPresets.VisionText);
        var b = r.Brief;
        b.Site.PlotWidth.Should().BeApproximately(Units.FtToMm(30), 1e-6);
        b.Site.PlotDepth.Should().BeApproximately(Units.FtToMm(40), 1e-6);
        b.Site.Facing.Should().Be(CardinalSide.E);
        b.Floors.Should().Be(2);
        b.Vastu.Should().BeTrue();
        var living = b.Rooms.Single(x => x.Type == RoomType.Living);
        living.Floor.Should().Be(0);
        living.Width.Should().BeApproximately(Units.FtToMm(14), 1e-6);
        living.Depth.Should().BeApproximately(Units.FtToMm(16), 1e-6);
        b.Rooms.Single(x => x.Type == RoomType.MasterBedroom).Should().Match<RoomRequest>(x => x.Floor == 0 && x.AttachedBath);
        b.Rooms.Where(x => x.Type == RoomType.Bedroom && x.Floor == 1).Sum(x => x.Count).Should().Be(3);
        b.Rooms.Should().Contain(x => x.Type == RoomType.Lounge && x.Floor == 1);
        b.Rooms.Should().Contain(x => x.Type == RoomType.Balcony && x.Floor == 1);
        b.Rooms.Should().Contain(x => x.Type == RoomType.Pooja && x.Floor == 0);
        b.CarParks.Should().Be(1);
        r.Understood.Should().Contain(s => s.StartsWith("Facing E"));
    }

    [Fact]
    public void Parses_metres_bhk_and_vastu_off()
    {
        var b = BriefTextParser.Parse("12 x 15 m plot, north facing, 3BHK, no vastu").Brief;
        b.Site.PlotWidth.Should().Be(12000);
        b.Site.Facing.Should().Be(CardinalSide.N);
        b.Vastu.Should().BeFalse();
        b.Rooms.Where(x => x.Type is RoomType.Bedroom or RoomType.MasterBedroom).Sum(x => x.Count).Should().Be(3);
        b.Rooms.Should().Contain(x => x.Type == RoomType.Kitchen);
    }

    [Fact]
    public void Distributes_bedrooms_when_no_floor_sections()
    {
        var b = BriefTextParser.Parse("30x50 west facing G+1 with 3 bedrooms, kitchen, living, pooja").Brief;
        b.Rooms.Where(x => x.Floor == 1 && x.Type == RoomType.Bedroom).Should().NotBeEmpty();
        b.Rooms.Should().Contain(x => x.Floor == 1 && x.Type == RoomType.Lounge);
    }

    [Fact]
    public void Floors_grow_to_match_sections()
    {
        var r = BriefTextParser.Parse("30x40 east facing. ground floor: living, kitchen. first floor: 2 bedrooms");
        r.Brief.Floors.Should().Be(2);
        r.Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public void Validator_flags_overfull_floor_with_suggestion()
    {
        var b = BriefPresets.South20x30OneBhk();
        b.Rooms.Add(RoomDefaults.RequestFt(RoomType.Bedroom, 0, 12, 12, count: 3));
        Validator.ApplyBylawSetbacks(b);
        var issues = Validator.Validate(b);
        issues.Should().Contain(i => i.Severity == IssueSeverity.Error && i.Suggestion.Contains("Reduce room areas"));
    }

    [Fact]
    public void Vision_brief_on_30x40_is_reported_as_overfull()
    {
        var b = BriefTextParser.Parse(BriefPresets.VisionText).Brief;
        Validator.ApplyBylawSetbacks(b);
        Validator.Validate(b).Should().Contain(i => i.Severity == IssueSeverity.Error && i.Message.StartsWith("Ground floor needs"));
    }

    [Fact]
    public void Validator_rejects_too_tall_building()
    {
        var b = BriefPresets.East30x40TwoBhk();
        b.Floors = 6;
        b.FloorHeight = 3200;
        Validator.Validate(b).Should().Contain(i => i.Message.Contains("height"));
    }

    [Fact]
    public void All_presets_validate_without_errors()
    {
        foreach (var (name, create) in BriefPresets.All)
        {
            var b = create();
            Validator.ApplyBylawSetbacks(b);
            Validator.Validate(b).Where(i => i.Severity == IssueSeverity.Error).Should().BeEmpty(name);
        }
    }

    [Fact]
    public void Bylaw_setbacks_applied_by_plot_area()
    {
        var b = BriefPresets.East30x40TwoBhk(); // 111.5 m²
        Validator.ApplyBylawSetbacks(b);
        b.Site.Setbacks.Front.Should().Be(1500);
        b.Site.Setbacks.Side.Should().Be(750);
    }
}
