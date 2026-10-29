using System.Text.Json.Serialization;

namespace BuildingStudio.Core.Model;

/// <summary>The single source of truth. Every engine reads and writes this model.</summary>
public sealed class BuildingModel
{
    public const int CurrentVersion = 1;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New House";
    public int Version { get; set; } = CurrentVersion;
    public Brief Brief { get; set; } = new();
    public List<Storey> Storeys { get; set; } = [];
    public StructureModel Structure { get; set; } = new();
    public ExteriorSpec Exterior { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = [];

    [JsonIgnore] public Site Site => Brief.Site;

    public Storey? GetStorey(int level) => Storeys.FirstOrDefault(s => s.Level == level);

    public IEnumerable<Room> AllRooms() => Storeys.SelectMany(s => s.Rooms);

    /// <summary>Total built-up area of all storeys, mm².</summary>
    public double BuiltUpArea() => Storeys.Sum(s => s.Rooms.Sum(r => r.Area));

    /// <summary>Total building height to top of roof slab (excluding parapet), mm.</summary>
    public double RoofLevel() => Storeys.Count == 0 ? 0 : Storeys.Max(s => s.Elevation + s.Height);
}
