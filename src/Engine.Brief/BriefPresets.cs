using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Brief;

/// <summary>Sample briefs used for demos and the Phase 2 gate.</summary>
public static class BriefPresets
{
    public const string VisionText =
        "Plot 30x40 ft, east-facing, G+1. Ground floor: living 14x16, kitchen 10x12, dining, 1 bedroom + attached bath, pooja, parking. " +
        "First floor: 3 bedrooms, family lounge, balcony. Follow Vastu.";

    public static IReadOnlyList<(string Name, Func<Core.Model.Brief> Create)> All { get; } =
    [
        ("30x40 East 2BHK (G)", East30x40TwoBhk),
        ("30x50 North 3BHK (G+1)", North30x50ThreeBhk),
        ("40x60 West 4BHK (G+1)", West40x60FourBhk),
        ("20x30 South 1BHK (G)", South20x30OneBhk),
        ("30x60 East vision brief (G+1)", VisionOn30x60),
    ];

    /// <summary>
    /// The vision brief needs about 93 m² per floor, which does not fit a 30x40 ft plot after TN setbacks
    /// (71 m² buildable); the validator reports that. This preset runs the same programme on 30x60 ft (30x50 is still ~9 m² short).
    /// </summary>
    public static Core.Model.Brief VisionOn30x60()
    {
        var b = BriefTextParser.Parse(VisionText.Replace("30x40", "30x60")).Brief;
        b.ProjectName = "30x60 East vision brief";
        return b;
    }

    private static Core.Model.Brief Make(string name, double wFt, double dFt, CardinalSide facing, int floors, params RoomRequest[] rooms)
    {
        var b = new Core.Model.Brief
        {
            ProjectName = name,
            Floors = floors,
            Site = new Site { PlotWidth = Units.FtToMm(wFt), PlotDepth = Units.FtToMm(dFt), Facing = facing },
        };
        b.Rooms.AddRange(rooms);
        return b;
    }

    public static Core.Model.Brief East30x40TwoBhk() => Make("30x40 East 2BHK", 30, 40, CardinalSide.E, 1,
        RoomDefaults.RequestFt(RoomType.Living, 0, 12, 13),
        RoomDefaults.RequestFt(RoomType.Kitchen, 0, 8, 9),
        RoomDefaults.RequestFt(RoomType.Dining, 0, 8, 9),
        RoomDefaults.RequestFt(RoomType.MasterBedroom, 0, 10, 11, attachedBath: true),
        RoomDefaults.RequestFt(RoomType.Bedroom, 0, 10, 10),
        RoomDefaults.RequestFt(RoomType.Bath, 0, 5, 7),
        RoomDefaults.RequestFt(RoomType.Pooja, 0, 4, 5));

    public static Core.Model.Brief North30x50ThreeBhk() => Make("30x50 North 3BHK", 30, 50, CardinalSide.N, 2,
        RoomDefaults.RequestFt(RoomType.Living, 0, 13, 14),
        RoomDefaults.RequestFt(RoomType.Kitchen, 0, 9, 10),
        RoomDefaults.RequestFt(RoomType.Dining, 0, 9, 10),
        RoomDefaults.RequestFt(RoomType.Bedroom, 0, 10, 11),
        RoomDefaults.RequestFt(RoomType.Bath, 0, 5, 7),
        RoomDefaults.RequestFt(RoomType.Pooja, 0, 4, 5),
        RoomDefaults.RequestFt(RoomType.MasterBedroom, 1, 12, 13, attachedBath: true),
        RoomDefaults.RequestFt(RoomType.Bedroom, 1, 10, 11, attachedBath: true),
        RoomDefaults.RequestFt(RoomType.Lounge, 1, 10, 11),
        RoomDefaults.RequestFt(RoomType.Balcony, 1, 4, 10));

    public static Core.Model.Brief West40x60FourBhk() => Make("40x60 West 4BHK", 40, 60, CardinalSide.W, 2,
        RoomDefaults.RequestFt(RoomType.Living, 0, 15, 16),
        RoomDefaults.RequestFt(RoomType.Kitchen, 0, 10, 12),
        RoomDefaults.RequestFt(RoomType.Dining, 0, 11, 12),
        RoomDefaults.RequestFt(RoomType.Bedroom, 0, 11, 12, attachedBath: true),
        RoomDefaults.RequestFt(RoomType.Pooja, 0, 5, 6),
        RoomDefaults.RequestFt(RoomType.Parking, 0, 10, 18),
        RoomDefaults.RequestFt(RoomType.Bath, 0, 5, 7),
        RoomDefaults.RequestFt(RoomType.MasterBedroom, 1, 13, 14, attachedBath: true),
        RoomDefaults.RequestFt(RoomType.Bedroom, 1, 11, 12, count: 2, attachedBath: true),
        RoomDefaults.RequestFt(RoomType.Lounge, 1, 12, 13),
        RoomDefaults.RequestFt(RoomType.Study, 1, 9, 10),
        RoomDefaults.RequestFt(RoomType.Balcony, 1, 4, 12));

    public static Core.Model.Brief South20x30OneBhk() => Make("20x30 South 1BHK", 20, 30, CardinalSide.S, 1,
        RoomDefaults.RequestFt(RoomType.Living, 0, 10, 12),
        RoomDefaults.RequestFt(RoomType.Kitchen, 0, 7, 8),
        RoomDefaults.RequestFt(RoomType.MasterBedroom, 0, 10, 10),
        RoomDefaults.RequestFt(RoomType.Bath, 0, 5, 6));
}
