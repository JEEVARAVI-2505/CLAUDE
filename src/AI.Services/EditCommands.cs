using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingStudio.Core.Model;

namespace BuildingStudio.AI.Services;

public enum EditKind { Swap, MoveToZone, Resize, Lock, Unlock }

/// <summary>A parsed natural-language edit. Room names are matched against the model by the pipeline.</summary>
public sealed record EditIntent(EditKind Kind, string RoomA, string? RoomB = null, Direction? Zone = null, double? WidthMm = null, double? DepthMm = null);

/// <summary>Parses edit commands ("swap kitchen and store", "move pooja to NE", "make living 16x14", "lock kitchen").</summary>
public sealed partial class EditCommandParser(IAiProvider ai)
{
    public static EditIntent? ParseOffline(string text)
    {
        var t = text.Trim().ToLowerInvariant();
        Match m;
        if ((m = Regex.Match(t, @"^swap\s+(?:the\s+)?(.+?)\s+(?:and|with)\s+(?:the\s+)?(.+)$")).Success)
            return new EditIntent(EditKind.Swap, m.Groups[1].Value, m.Groups[2].Value);
        if ((m = Regex.Match(t, @"^move\s+(?:the\s+)?(.+?)\s+to\s+(?:the\s+)?(north[\s-]?east|north[\s-]?west|south[\s-]?east|south[\s-]?west|north|south|east|west|ne|nw|se|sw|n|s|e|w|centre|center)\b")).Success)
            return new EditIntent(EditKind.MoveToZone, m.Groups[1].Value, Zone: ZoneOf(m.Groups[2].Value));
        if ((m = Regex.Match(t, @"^(?:make|resize)\s+(?:the\s+)?(.+?)\s+(\d+(?:\.\d+)?)\s*x\s*(\d+(?:\.\d+)?)\s*(m|ft|feet)?")).Success)
        {
            var metres = m.Groups[4].Value == "m";
            double V(int g) => double.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture) * (metres ? 1000 : 304.8);
            return new EditIntent(EditKind.Resize, m.Groups[1].Value, WidthMm: V(2), DepthMm: V(3));
        }
        if ((m = Regex.Match(t, @"^(un)?lock\s+(?:the\s+)?(.+)$")).Success)
            return new EditIntent(m.Groups[1].Success ? EditKind.Unlock : EditKind.Lock, m.Groups[2].Value);
        return null;
    }

    public async Task<EditIntent?> ParseAsync(string text, CancellationToken ct = default)
    {
        var offline = ParseOffline(text);
        if (offline is not null || !ai.IsEnabled) return offline;
        var json = await ai.CompleteAsync(
            "Convert the house-plan edit into JSON {\"kind\":\"Swap|MoveToZone|Resize|Lock|Unlock\",\"roomA\":string,\"roomB\":string|null," +
            "\"zone\":\"N|NE|E|SE|S|SW|W|NW|Centre\"|null,\"widthMm\":number|null,\"depthMm\":number|null}. JSON only.\nEdit: " + text, true, ct);
        if (json is null) return null;
        try
        {
            var intent = JsonSerializer.Deserialize<EditIntent>(json, ProjectSerializer.Options);
            return intent is { RoomA.Length: > 0 } ? intent : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static Direction ZoneOf(string s) => s.Replace(" ", "").Replace("-", "") switch
    {
        "northeast" or "ne" => Direction.NE,
        "northwest" or "nw" => Direction.NW,
        "southeast" or "se" => Direction.SE,
        "southwest" or "sw" => Direction.SW,
        "north" or "n" => Direction.N,
        "south" or "s" => Direction.S,
        "east" or "e" => Direction.E,
        "west" or "w" => Direction.W,
        _ => Direction.Centre,
    };
}

/// <summary>Design review notes. Facts come from the engines; AI may only rephrase them.</summary>
public sealed class DesignReviewer(IAiProvider ai)
{
    public async Task<string> SummariseAsync(IReadOnlyList<string> facts, CancellationToken ct = default)
    {
        var plain = string.Join(Environment.NewLine, facts.Select(f => $"- {f}"));
        if (!ai.IsEnabled) return plain;
        var reply = await ai.CompleteAsync(
            "Rewrite these design-review findings for a home owner in short plain sentences. Keep every number exactly as given, " +
            "do not add new claims, and never say the structure is safe.\n" + plain, false, ct);
        return string.IsNullOrWhiteSpace(reply) ? plain : reply.Trim();
    }
}

/// <summary>Scanned plan → editable plan (Phase 9.4). Not implemented yet.</summary>
public interface IPlanImageRecognizer
{
    Task<(bool Ok, string Message, IReadOnlyList<Room> Rooms)> RecognizeAsync(string imagePath, CancellationToken ct = default);
}

public sealed class PlanImageRecognizerNotAvailable : IPlanImageRecognizer
{
    public Task<(bool Ok, string Message, IReadOnlyList<Room> Rooms)> RecognizeAsync(string imagePath, CancellationToken ct = default) =>
        Task.FromResult((false, "Scanned-plan recognition is not built yet (planned: OpenCV wall detection + OCR). Draw the plan or use the brief form.", (IReadOnlyList<Room>)[]));
}
