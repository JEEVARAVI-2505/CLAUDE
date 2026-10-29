using System.Globalization;
using System.Text.RegularExpressions;
using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Brief;

/// <summary>Result of parsing a natural-language brief. Must be shown to the user as an editable form.</summary>
public sealed record BriefParseResult(Core.Model.Brief Brief, IReadOnlyList<string> Understood, IReadOnlyList<string> Warnings);

/// <summary>
/// Offline, rule-based parser for briefs such as
/// "Plot 30x40 ft, east-facing, G+1. Ground floor: living 14x16, kitchen 10x12 ...".
/// Sizes default to feet. Works with no AI provider.
/// </summary>
public static partial class BriefTextParser
{
    private static readonly (string Pattern, RoomType Type)[] Keywords =
    [
        (@"master", RoomType.MasterBedroom),
        (@"bed\s*rooms?|bedrooms?|\bbeds?\b", RoomType.Bedroom),
        (@"living|\bhall\b|drawing", RoomType.Living),
        (@"kitchen", RoomType.Kitchen),
        (@"dining", RoomType.Dining),
        (@"pooja|puja|prayer", RoomType.Pooja),
        (@"study|office", RoomType.Study),
        (@"store", RoomType.Store),
        (@"utility|wash\s*area", RoomType.Utility),
        (@"parking|car\s*park|car\s*porch|\bcars?\b", RoomType.Parking),
        (@"balcon|sit-?out", RoomType.Balcony),
        (@"lounge|family\s*(room|area)", RoomType.Lounge),
        (@"\bbath|toilet|\bwc\b|washroom", RoomType.Bath),
    ];

    private static readonly Dictionary<string, int> Words = new()
    {
        ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5, ["single"] = 1, ["double"] = 2,
    };

    public static BriefParseResult Parse(string text)
    {
        var brief = new Core.Model.Brief();
        var understood = new List<string>();
        var warnings = new List<string>();
        var t = text.ToLowerInvariant().Replace('×', 'x');

        ParsePlot(t, brief, understood, warnings);
        ParseFacingFloorsVastu(t, brief, understood);

        var sections = SplitSections(t);
        var explicitSections = sections.Count > 1 || sections[0].Floor != 0 || SectionRegex().IsMatch(t);
        foreach (var (floor, segment) in sections) ParseRooms(segment, floor, brief);

        var maxFloor = brief.Rooms.Count == 0 ? 0 : brief.Rooms.Max(r => r.Floor);
        if (maxFloor + 1 > brief.Floors)
        {
            warnings.Add($"Rooms were listed for {maxFloor + 1} floors; floors set to G+{maxFloor}.");
            brief.Floors = maxFloor + 1;
        }
        if (!explicitSections && brief.Floors > 1) DistributeAcrossFloors(brief, understood);
        Complete(brief, understood);

        for (var f = 0; f < brief.Floors; f++)
        {
            var list = brief.Rooms.Where(r => r.Floor == f).Select(Describe);
            understood.Add($"{(f == 0 ? "Ground floor" : $"Floor {f}")}: {string.Join(", ", list)}");
        }
        return new BriefParseResult(brief, understood, warnings);
    }

    private static void ParsePlot(string t, Core.Model.Brief brief, List<string> understood, List<string> warnings)
    {
        var start = t.IndexOf("plot", StringComparison.Ordinal);
        var m = SizeRegex().Match(t, Math.Max(0, start));
        if (!m.Success) m = SizeRegex().Match(t);
        if (!m.Success)
        {
            warnings.Add("Plot size not found; using 30 x 40 ft.");
            return;
        }
        var metres = IsMetres(m.Groups[3].Value);
        var w = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var d = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        brief.Site.PlotWidth = metres ? w * 1000 : Units.FtToMm(w);
        brief.Site.PlotDepth = metres ? d * 1000 : Units.FtToMm(d);
        understood.Add($"Plot {w:0.##} x {d:0.##} {(metres ? "m" : "ft")} ({brief.Site.PlotWidth / 1000:0.0} x {brief.Site.PlotDepth / 1000:0.0} m)");
    }

    private static void ParseFacingFloorsVastu(string t, Core.Model.Brief brief, List<string> understood)
    {
        var f = FacingRegex().Match(t);
        if (f.Success)
        {
            var word = f.Groups[1].Success && f.Groups[1].Value.Length > 0 ? f.Groups[1].Value : f.Groups[2].Value;
            brief.Site.Facing = word[0] switch { 'n' => CardinalSide.N, 's' => CardinalSide.S, 'w' => CardinalSide.W, _ => CardinalSide.E };
            understood.Add($"Facing {brief.Site.Facing}");
        }

        var g = FloorsRegex().Match(t);
        if (g.Success) brief.Floors = int.Parse(g.Groups[1].Value, CultureInfo.InvariantCulture) + 1;
        understood.Add(brief.Floors == 1 ? "Floors: G" : $"Floors: G+{brief.Floors - 1}");

        if (Regex.IsMatch(t, @"\b(no|without|ignore)\s+vastu")) brief.Vastu = false;
        else if (t.Contains("vastu", StringComparison.Ordinal)) brief.Vastu = true;
        if (Regex.IsMatch(t, @"strict(ly)?\s+(follow\s+)?vastu|vastu\s+strict")) brief.Strictness = VastuStrictness.Strict;
        else if (t.Contains("flexible", StringComparison.Ordinal)) brief.Strictness = VastuStrictness.Flexible;
        understood.Add(brief.Vastu ? $"Vastu: on ({brief.Strictness})" : "Vastu: off");

        foreach (var style in Enum.GetValues<ExteriorStyle>())
            if (t.Contains(style.ToString().ToLowerInvariant(), StringComparison.Ordinal)) brief.Style = style;
        if (t.Contains("chettinad", StringComparison.Ordinal)) brief.Style = ExteriorStyle.Traditional;
    }

    private static List<(int Floor, string Text)> SplitSections(string t)
    {
        var matches = SectionRegex().Matches(t);
        if (matches.Count == 0) return [(0, t)];
        var list = new List<(int, string)>();
        if (matches[0].Index > 0) list.Add((0, t[..matches[0].Index]));
        for (var i = 0; i < matches.Count; i++)
        {
            var end = i + 1 < matches.Count ? matches[i + 1].Index : t.Length;
            var start = matches[i].Index + matches[i].Length;
            list.Add((FloorIndex(matches[i].Groups[1].Value), t[start..end]));
        }
        return list;
    }

    private static int FloorIndex(string s) => s switch
    {
        var x when x.StartsWith("ground") || x == "gf" => 0,
        var x when x.StartsWith("first") || x.StartsWith("1st") || x == "ff" => 1,
        var x when x.StartsWith("second") || x.StartsWith("2nd") => 2,
        var x when x.StartsWith("third") || x.StartsWith("3rd") => 3,
        _ => 4,
    };

    private static void ParseRooms(string segment, int floor, Core.Model.Brief brief)
    {
        var bhk = BhkRegex().Match(segment);
        if (bhk.Success)
        {
            var n = int.Parse(bhk.Groups[1].Value, CultureInfo.InvariantCulture);
            brief.Rooms.Add(RoomDefaults.Request(RoomType.Bedroom, floor, n));
            if (!segment.Contains("living") && !segment.Contains("hall")) brief.Rooms.Add(RoomDefaults.Request(RoomType.Living, floor));
            if (!segment.Contains("kitchen")) brief.Rooms.Add(RoomDefaults.Request(RoomType.Kitchen, floor));
            segment = segment.Remove(bhk.Index, bhk.Length);
        }

        RoomRequest? lastBedroom = null;
        foreach (var raw in Regex.Split(segment, @",|\+|;|\.(?!\d)|\band\b|\bwith\b|\n"))
        {
            var token = raw.Trim();
            if (token.Length == 0 || token.Contains("plot") || token.Contains("facing") || token.Contains("g+")) continue;

            if (Regex.IsMatch(token, @"^attached\s+(bath|toilet)") || token == "attached")
            {
                if (lastBedroom is not null) lastBedroom.AttachedBath = true;
                continue;
            }

            var type = Keywords.Where(k => Regex.IsMatch(token, k.Pattern)).Select(k => (RoomType?)k.Type).FirstOrDefault();
            if (type is null) continue;

            var count = 1;
            var c = Regex.Match(token, @"^(\d+)\s*(?!x|\d)");
            if (c.Success) count = int.Parse(c.Groups[1].Value, CultureInfo.InvariantCulture);
            else
            {
                var word = Regex.Match(token, @"^(one|two|three|four|five|single|double)\b");
                if (word.Success) count = Words[word.Groups[1].Value];
            }

            var req = RoomDefaults.Request(type.Value, floor, count);
            var size = SizeRegex().Match(token);
            if (size.Success)
            {
                var metres = IsMetres(size.Groups[3].Value);
                var w = double.Parse(size.Groups[1].Value, CultureInfo.InvariantCulture);
                var d = double.Parse(size.Groups[2].Value, CultureInfo.InvariantCulture);
                req.Width = metres ? w * 1000 : Units.FtToMm(w);
                req.Depth = metres ? d * 1000 : Units.FtToMm(d);
            }
            if (type == RoomType.Parking)
            {
                brief.CarParks += count;
                req.Width *= count;
                req.Count = 1;
            }
            if (type is RoomType.Bedroom or RoomType.MasterBedroom)
            {
                req.AttachedBath = token.Contains("attached");
                lastBedroom = req;
            }
            brief.Rooms.Add(req);
        }
    }

    private static void DistributeAcrossFloors(Core.Model.Brief brief, List<string> understood)
    {
        var beds = brief.Rooms.Where(r => r.Type is RoomType.Bedroom && r.Floor == 0).ToList();
        var total = beds.Sum(b => b.Count) + brief.Rooms.Count(r => r.Type == RoomType.MasterBedroom);
        if (total <= 1) return;
        foreach (var b in beds) b.Floor = 1;
        brief.Rooms.Add(RoomDefaults.Request(RoomType.Lounge, 1));
        understood.Add("No floor-wise list given: bedrooms moved to the first floor with a family lounge.");
    }

    private static void Complete(Core.Model.Brief brief, List<string> understood)
    {
        if (!brief.Rooms.Any(r => r.Type == RoomType.MasterBedroom))
        {
            var first = brief.Rooms.Where(r => r.Type == RoomType.Bedroom).OrderBy(r => r.Floor).FirstOrDefault();
            if (first is not null)
            {
                var (w, d) = RoomDefaults.SizeMm(RoomType.MasterBedroom);
                var master = new RoomRequest { Type = RoomType.MasterBedroom, Floor = first.Floor, Count = 1, AttachedBath = true, Width = Math.Max(first.Width, w), Depth = Math.Max(first.Depth, d) };
                if (first.Count > 1) first.Count--; else brief.Rooms.Remove(first);
                brief.Rooms.Insert(0, master);
                understood.Add("First bedroom set as master bedroom with attached bath.");
            }
        }
        if (!brief.Rooms.Any(r => r.Type == RoomType.Living)) brief.Rooms.Insert(0, RoomDefaults.Request(RoomType.Living, 0));
        if (!brief.Rooms.Any(r => r.Type == RoomType.Kitchen)) brief.Rooms.Add(RoomDefaults.Request(RoomType.Kitchen, 0));

        for (var f = 0; f < brief.Floors; f++)
        {
            var floor = f;
            var needsBath = brief.Rooms.Any(r => r.Floor == floor && r.Type is RoomType.Bedroom or RoomType.Living or RoomType.Lounge && !r.AttachedBath);
            if (needsBath && !brief.Rooms.Any(r => r.Floor == floor && r.Type == RoomType.Bath))
                brief.Rooms.Add(RoomDefaults.Request(RoomType.Bath, floor));
        }
    }

    private static bool IsMetres(string unit) => unit is "m" or "mt" or "metre" or "meter" or "metres" or "meters";

    private static string Describe(RoomRequest r)
    {
        var name = RoomTypes.DisplayName(r.Type);
        var count = r.Count > 1 ? $"{r.Count} x " : "";
        var bath = r.AttachedBath ? " + attached bath" : "";
        return $"{count}{name} {Units.MmToFt(r.Width):0.#}x{Units.MmToFt(r.Depth):0.#} ft{bath}";
    }

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(?:ft|feet|')?\s*x\s*(\d+(?:\.\d+)?)\s*(ft|feet|'|metres|meters|metre|meter|mt|m)?\b")]
    private static partial Regex SizeRegex();

    [GeneratedRegex(@"\b(north|south|east|west)[\s-]*facing\b|\bfacing\s+(north|south|east|west)\b")]
    private static partial Regex FacingRegex();

    [GeneratedRegex(@"\bg\s*\+\s*(\d)\b")]
    private static partial Regex FloorsRegex();

    [GeneratedRegex(@"\b(ground floor|first floor|second floor|third floor|1st floor|2nd floor|3rd floor|gf|ff)\s*[:\-]")]
    private static partial Regex SectionRegex();

    [GeneratedRegex(@"\b(\d)\s*bhk\b")]
    private static partial Regex BhkRegex();
}
