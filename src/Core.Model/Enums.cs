namespace BuildingStudio.Core.Model;

/// <summary>Room programme types.</summary>
public enum RoomType
{
    Living, Kitchen, Dining, MasterBedroom, Bedroom, Bath, Pooja, Study, Store,
    Staircase, Parking, Utility, Balcony, Lounge, Passage,
}

/// <summary>Vastu / compass zone. Centre is the Brahmasthan.</summary>
public enum Direction { N, NE, E, SE, S, SW, W, NW, Centre }

/// <summary>Cardinal side of the plot (used for facing and elevations).</summary>
public enum CardinalSide { N, E, S, W }

public enum VastuStrictness { Strict, Moderate, Flexible }

public enum WallType { Exterior, Interior }

public enum OpeningKind { Door, Window, Ventilator, Archway }

public enum ExteriorStyle { Modern, Contemporary, Traditional, Colonial }

public enum RoofForm { Flat, SlopedTile, Pergola }

/// <summary>Helpers for room-type semantics.</summary>
public static class RoomTypes
{
    /// <summary>Rooms people live in (need daylight, minimum size).</summary>
    public static bool IsHabitable(RoomType t) => t is RoomType.Living or RoomType.Kitchen or RoomType.Dining
        or RoomType.MasterBedroom or RoomType.Bedroom or RoomType.Study or RoomType.Lounge;

    /// <summary>Small rooms that may share a slot (stacked across a band) in the layout generator.</summary>
    public static bool IsSmall(RoomType t) => t is RoomType.Bath or RoomType.Pooja or RoomType.Store or RoomType.Utility;

    public static bool IsWet(RoomType t) => t is RoomType.Bath or RoomType.Kitchen or RoomType.Utility;

    public static string DisplayName(RoomType t) => t switch
    {
        RoomType.MasterBedroom => "Master Bedroom",
        RoomType.Bath => "Bath/WC",
        RoomType.Staircase => "Staircase",
        _ => t.ToString(),
    };
}

public static class Directions
{
    public static CardinalSide Opposite(CardinalSide s) => s switch
    {
        CardinalSide.N => CardinalSide.S,
        CardinalSide.S => CardinalSide.N,
        CardinalSide.E => CardinalSide.W,
        _ => CardinalSide.E,
    };

    public static Direction ToDirection(CardinalSide s) => s switch
    {
        CardinalSide.N => Direction.N,
        CardinalSide.E => Direction.E,
        CardinalSide.S => Direction.S,
        _ => Direction.W,
    };
}
