using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Brief;

/// <summary>Default room sizes (feet, width x depth) used when the brief gives no size.</summary>
public static class RoomDefaults
{
    private static readonly Dictionary<RoomType, (double W, double D)> SizesFt = new()
    {
        [RoomType.Living] = (12, 14),
        [RoomType.Kitchen] = (9, 10),
        [RoomType.Dining] = (9, 10),
        [RoomType.MasterBedroom] = (11, 13),
        [RoomType.Bedroom] = (10, 11),
        [RoomType.Bath] = (5, 7),
        [RoomType.Pooja] = (4, 5),
        [RoomType.Study] = (9, 10),
        [RoomType.Store] = (5, 6),
        [RoomType.Utility] = (5, 6),
        [RoomType.Parking] = (9, 16),
        [RoomType.Balcony] = (4, 10),
        [RoomType.Lounge] = (10, 11),
        [RoomType.Staircase] = (8, 13),
        [RoomType.Passage] = (3.5, 20),
    };

    /// <summary>Default width and depth in mm.</summary>
    public static (double Width, double Depth) SizeMm(RoomType t)
    {
        var (w, d) = SizesFt[t];
        return (Units.FtToMm(w), Units.FtToMm(d));
    }

    public static RoomRequest Request(RoomType t, int floor, int count = 1, bool attachedBath = false)
    {
        var (w, d) = SizeMm(t);
        return new RoomRequest { Type = t, Floor = floor, Count = count, Width = w, Depth = d, AttachedBath = attachedBath };
    }

    public static RoomRequest RequestFt(RoomType t, int floor, double wFt, double dFt, int count = 1, bool attachedBath = false) =>
        new() { Type = t, Floor = floor, Count = count, Width = Units.FtToMm(wFt), Depth = Units.FtToMm(dFt), AttachedBath = attachedBath };
}
