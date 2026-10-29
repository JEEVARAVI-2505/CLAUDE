using System.Text.Json;
using BuildingStudio.Core.Model;
using BuildingStudio.Engine.Brief;

namespace BuildingStudio.AI.Services;

/// <summary>
/// Turns free text into a Brief. Uses the AI provider when enabled (JSON, schema-checked) and falls back to the
/// offline rule parser. The result is always shown to the user as an editable form before generation.
/// </summary>
public sealed class BriefAssistant(IAiProvider ai)
{
    public const string Schema = """
{ "projectName": string, "floors": int (1 = ground only), "vastu": bool, "strictness": "Strict"|"Moderate"|"Flexible",
  "style": "Modern"|"Contemporary"|"Traditional"|"Colonial", "carParks": int,
  "site": { "plotWidth": mm, "plotDepth": mm, "facing": "N"|"E"|"S"|"W" },
  "rooms": [ { "type": "Living"|"Kitchen"|"Dining"|"MasterBedroom"|"Bedroom"|"Bath"|"Pooja"|"Study"|"Store"|"Parking"|"Utility"|"Balcony"|"Lounge",
               "floor": int (0 = ground), "count": int, "width": mm, "depth": mm, "attachedBath": bool } ] }
""";

    public async Task<(BriefParseResult Result, string Source)> ParseAsync(string text, CancellationToken ct = default)
    {
        if (ai.IsEnabled)
        {
            var prompt = $"Convert this house brief into JSON matching this schema. Feet must be converted to millimetres (1 ft = 304.8 mm). " +
                         $"Output JSON only.\nSchema:\n{Schema}\nBrief:\n{text}";
            var json = await ai.CompleteAsync(prompt, json: true, ct);
            var brief = TryRead(json);
            if (brief is not null)
                return (new BriefParseResult(brief, [$"Parsed by {ai.Name}. Please confirm every field."], []), ai.Name);
        }
        var offline = BriefTextParser.Parse(text);
        return (offline, ai.IsEnabled ? "Offline rules (AI reply was unusable)" : "Offline rules");
    }

    /// <summary>Reads and sanity-checks an AI JSON brief; null when invalid.</summary>
    public static Core.Model.Brief? TryRead(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var b = JsonSerializer.Deserialize<Core.Model.Brief>(json, ProjectSerializer.Options);
            if (b is null || b.Site.PlotWidth < 3000 || b.Site.PlotDepth < 3000 || b.Rooms.Count == 0 || b.Floors < 1 || b.Floors > 6) return null;
            if (b.Rooms.Any(r => r.Width < 600 || r.Depth < 600 || r.Count < 1 || r.Floor < 0 || r.Floor >= b.Floors)) return null;
            return b;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
