using System.Text.Json;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Core.Rules;

/// <summary>Loads every rule pack from a rules directory.</summary>
public sealed class RuleRepository
{
    public required VastuRulePack Vastu { get; init; }
    public required BylawRulePack Bylaws { get; init; }
    public required RateTable Rates { get; init; }
    public required FurnitureCatalog Furniture { get; init; }
    public required ExteriorStylePack Styles { get; init; }
    public required string Directory { get; init; }

    /// <summary>
    /// Loads rule packs. With no directory, looks next to the executable, then walks up from the
    /// working directory looking for a "rules" folder.
    /// </summary>
    public static RuleRepository Load(string? directory = null, string bylawPack = "bylaws.tn.json", string ratePack = "rates.tn.json")
    {
        var dir = directory ?? FindRulesDirectory();
        return new RuleRepository
        {
            Directory = dir,
            Vastu = Read<VastuRulePack>(dir, "vastu.default.json"),
            Bylaws = Read<BylawRulePack>(dir, bylawPack),
            Rates = Read<RateTable>(dir, ratePack),
            Furniture = Read<FurnitureCatalog>(dir, "furniture.catalog.json"),
            Styles = Read<ExteriorStylePack>(dir, "exterior.styles.json"),
        };
    }

    public static T Read<T>(string dir, string file)
    {
        var path = Path.Combine(dir, file);
        if (!File.Exists(path)) throw new FileNotFoundException($"Rule pack not found: {path}", path);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), ProjectSerializer.Options)
            ?? throw new InvalidDataException($"Rule pack {file} is empty.");
    }

    public static string FindRulesDirectory()
    {
        var local = Path.Combine(AppContext.BaseDirectory, "rules");
        if (File.Exists(Path.Combine(local, "vastu.default.json"))) return local;
        for (var d = new DirectoryInfo(System.IO.Directory.GetCurrentDirectory()); d is not null; d = d.Parent)
        {
            var candidate = Path.Combine(d.FullName, "rules");
            if (File.Exists(Path.Combine(candidate, "vastu.default.json"))) return candidate;
        }
        throw new DirectoryNotFoundException("Could not find the 'rules' directory.");
    }
}
