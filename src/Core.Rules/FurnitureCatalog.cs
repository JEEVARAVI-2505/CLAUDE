using BuildingStudio.Core.Model;

namespace BuildingStudio.Core.Rules;

public enum FurniturePlacement { Wall, Centre, Corner, BedWall }

public sealed class CatalogItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double Width { get; set; }
    public double Depth { get; set; }
    public double Height { get; set; }
    public string Color { get; set; } = "#a0a0a0";
    public FurniturePlacement Placement { get; set; } = FurniturePlacement.Wall;

    /// <summary>Clear space required in front of the item, mm.</summary>
    public double FrontClearance { get; set; } = 600;
}

public sealed class RoomFinish
{
    public string Floor { get; set; } = "";
    public string Wall { get; set; } = "#f5f2ea";
    public string Ceiling { get; set; } = "Plain";
    public double Lux { get; set; } = 150;
    public int SocketsPer10Sqm { get; set; } = 2;
}

/// <summary>Furniture catalog, room templates, default finishes and lighting levels.</summary>
public sealed class FurnitureCatalog
{
    public string Id { get; set; } = "";
    public List<CatalogItem> Items { get; set; } = [];

    /// <summary>Item ids per room type in priority order; alternative variants separated by '|'.</summary>
    public Dictionary<RoomType, List<string>> Templates { get; set; } = [];

    public Dictionary<RoomType, RoomFinish> Finishes { get; set; } = [];
    public double FixtureLumens { get; set; } = 1200;
    public double UtilisationTimesMaintenance { get; set; } = 0.5;

    public CatalogItem? Find(string id) => Items.FirstOrDefault(i => i.Id == id);
}
