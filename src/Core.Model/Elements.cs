using System.Text.Json.Serialization;
using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Core.Model;

/// <summary>A room. Rooms are axis-aligned rectangles in v1.</summary>
public sealed class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RoomType Type { get; set; }
    public string Name { get; set; } = "";
    public Rect2 Bounds { get; set; }
    public double TargetArea { get; set; }

    /// <summary>Locked rooms keep their zone when the generator re-solves.</summary>
    public bool Locked { get; set; }

    /// <summary>Vastu zone of the room centroid (computed).</summary>
    public Direction Zone { get; set; } = Direction.Centre;

    /// <summary>For attached baths: the bedroom they belong to.</summary>
    public Guid? ParentId { get; set; }

    public string FloorFinish { get; set; } = "";
    public string WallColor { get; set; } = "";

    [JsonIgnore] public Polygon Polygon => Bounds.ToPolygon();
    [JsonIgnore] public double Area => Bounds.Area;
}

public sealed class Wall
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Vec2 Start { get; set; }
    public Vec2 End { get; set; }
    public double Thickness { get; set; } = 230;
    public WallType Type { get; set; }
    public Guid? RoomA { get; set; }
    public Guid? RoomB { get; set; }

    [JsonIgnore] public Segment2 Segment => new(Start, End);
    [JsonIgnore] public double Length => Start.DistanceTo(End);
}

public sealed class Opening
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WallId { get; set; }

    /// <summary>Distance from the wall start to the opening's near edge, mm.</summary>
    public double Offset { get; set; }

    public double Width { get; set; }
    public double Height { get; set; }
    public double Sill { get; set; }
    public OpeningKind Kind { get; set; }
    public bool IsMainEntrance { get; set; }
}

/// <summary>Furniture placed in a room. Footprint is an axis-aligned rectangle.</summary>
public sealed class FurnitureItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CatalogId { get; set; } = "";
    public string Name { get; set; } = "";
    public Guid RoomId { get; set; }
    public Rect2 Bounds { get; set; }
    public double Height { get; set; }

    /// <summary>Rotation in radians (0 = back against the room's bottom wall).</summary>
    public double Rotation { get; set; }

    public string Color { get; set; } = "#a0a0a0";
}

/// <summary>A floor of the building.</summary>
public sealed class Storey
{
    /// <summary>0 = ground.</summary>
    public int Level { get; set; }

    public double Elevation { get; set; }
    public double Height { get; set; } = 3000;
    public double SlabThickness { get; set; } = 125;
    public Rect2 Envelope { get; set; }
    public List<Room> Rooms { get; set; } = [];
    public List<Wall> Walls { get; set; } = [];
    public List<Opening> Openings { get; set; } = [];
    public List<FurnitureItem> Furniture { get; set; } = [];

    [JsonIgnore] public string Name => Level == 0 ? "Ground Floor" : $"Floor {Level}";

    public Room? FindRoom(Guid id) => Rooms.FirstOrDefault(r => r.Id == id);
    public Wall? FindWall(Guid id) => Walls.FirstOrDefault(w => w.Id == id);
}
