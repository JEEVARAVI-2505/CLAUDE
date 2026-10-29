using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BuildingStudio.Core.Model;

/// <summary>A migration that upgrades the raw JSON of a project from one version to the next.</summary>
public interface IProjectMigration
{
    int FromVersion { get; }
    void Apply(JsonObject project);
}

/// <summary>Saves and loads <see cref="BuildingModel"/> as versioned JSON.</summary>
public static class ProjectSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Registered migrations (v0 → v1 is the pre-release format that lacked a version field).</summary>
    public static IReadOnlyList<IProjectMigration> Migrations { get; } = [new V0ToV1()];

    public static string Serialize(BuildingModel model) => JsonSerializer.Serialize(model, Options);

    public static BuildingModel Deserialize(string json)
    {
        var node = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("Project JSON must be an object.");
        var version = node["version"]?.GetValue<int>() ?? 0;
        if (version > BuildingModel.CurrentVersion)
            throw new InvalidDataException($"Project version {version} is newer than this app ({BuildingModel.CurrentVersion}).");
        foreach (var m in Migrations.Where(m => m.FromVersion >= version).OrderBy(m => m.FromVersion))
        {
            m.Apply(node);
            node["version"] = m.FromVersion + 1;
        }
        return node.Deserialize<BuildingModel>(Options) ?? throw new InvalidDataException("Empty project.");
    }

    public static void Save(BuildingModel model, string path) => File.WriteAllText(path, Serialize(model));

    public static BuildingModel Load(string path) => Deserialize(File.ReadAllText(path));

    /// <summary>Deep copy through JSON.</summary>
    public static T Clone<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!;

    private sealed class V0ToV1 : IProjectMigration
    {
        public int FromVersion => 0;

        public void Apply(JsonObject project)
        {
            // v0 stored the project title as "title".
            if (project["name"] is null && project["title"] is { } t)
            {
                project["name"] = t.GetValue<string>();
                project.Remove("title");
            }
        }
    }
}
