using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Core.Model;

public sealed class SiteItem
{
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public Rect2 Bounds { get; set; }
    public double Height { get; set; }
    public string Color { get; set; } = "#888888";

    /// <summary>Vastu note for the placement, if any.</summary>
    public string Note { get; set; } = "";
}

/// <summary>Exterior package: style, palette, facade elements and site items.</summary>
public sealed class ExteriorSpec
{
    public ExteriorStyle Style { get; set; } = ExteriorStyle.Contemporary;
    public string WallColor { get; set; } = "#f2efe8";
    public string AccentColor { get; set; } = "#8a5a3c";
    public string TrimColor { get; set; } = "#3a3a3a";
    public string GlassColor { get; set; } = "#7fb3d5";
    public string Cladding { get; set; } = "";
    public double ParapetHeight { get; set; } = 1000;
    public double ChajjaDepth { get; set; } = 600;
    public RoofForm Roof { get; set; } = RoofForm.Flat;
    public bool CorniceBand { get; set; }
    public double CompoundWallHeight { get; set; } = 1500;
    public double GateWidth { get; set; } = 3000;
    public List<string> MaterialNotes { get; set; } = [];
    public List<SiteItem> SiteItems { get; set; } = [];
}
