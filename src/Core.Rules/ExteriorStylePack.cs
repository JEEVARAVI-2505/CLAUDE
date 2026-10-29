using BuildingStudio.Core.Model;

namespace BuildingStudio.Core.Rules;

public sealed class ExteriorStyleDef
{
    public string Description { get; set; } = "";
    public string WallColor { get; set; } = "#f2efe8";
    public string AccentColor { get; set; } = "#8a5a3c";
    public string TrimColor { get; set; } = "#3a3a3a";
    public string GlassColor { get; set; } = "#7fb3d5";
    public string Cladding { get; set; } = "";
    public double ParapetHeight { get; set; } = 1000;
    public double ChajjaDepth { get; set; } = 600;
    public RoofForm Roof { get; set; } = RoofForm.Flat;
    public bool CorniceBand { get; set; }
    public List<string> Materials { get; set; } = [];
}

/// <summary>Exterior style presets (rules/exterior.styles.json).</summary>
public sealed class ExteriorStylePack
{
    public string Id { get; set; } = "";
    public Dictionary<ExteriorStyle, ExteriorStyleDef> Styles { get; set; } = [];
}
