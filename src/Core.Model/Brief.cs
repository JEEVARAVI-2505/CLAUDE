using System.Text.Json.Serialization;

namespace BuildingStudio.Core.Model;

/// <summary>One requested room (or a group of identical rooms) on a floor.</summary>
public sealed class RoomRequest
{
    public RoomType Type { get; set; }

    /// <summary>0 = ground floor.</summary>
    public int Floor { get; set; }

    public int Count { get; set; } = 1;

    /// <summary>Target width in mm.</summary>
    public double Width { get; set; }

    /// <summary>Target depth in mm.</summary>
    public double Depth { get; set; }

    /// <summary>Adds an attached bath per room (bedrooms).</summary>
    public bool AttachedBath { get; set; }

    public string? Name { get; set; }

    [JsonIgnore] public double TargetArea => Width * Depth;
}

/// <summary>The client requirement ("Brief"). Confirmed by the user before generation.</summary>
public sealed class Brief
{
    public string ProjectName { get; set; } = "New House";
    public Site Site { get; set; } = new();

    /// <summary>Number of storeys including ground (G+1 = 2).</summary>
    public int Floors { get; set; } = 1;

    /// <summary>Floor-to-floor height in mm.</summary>
    public double FloorHeight { get; set; } = 3000;

    public bool Vastu { get; set; } = true;
    public VastuStrictness Strictness { get; set; } = VastuStrictness.Moderate;
    public ExteriorStyle Style { get; set; } = ExteriorStyle.Contemporary;

    /// <summary>Characteristic concrete strength fck, N/mm² (M20 = 20).</summary>
    public double ConcreteFck { get; set; } = 20;

    /// <summary>Steel yield strength fy, N/mm² (Fe500 = 500).</summary>
    public double SteelFy { get; set; } = 500;

    public int CarParks { get; set; }

    public List<RoomRequest> Rooms { get; set; } = [];
}
