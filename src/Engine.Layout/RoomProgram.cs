using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Layout;

/// <summary>One room to be placed by the generator.</summary>
public sealed record RoomSpec(string Key, RoomType Type, string Name, double TargetArea, int Floor, string? ParentKey);

/// <summary>Expands a brief into the per-floor list of rooms (counts, attached baths, staircase).</summary>
public static class RoomProgram
{
    /// <summary>Staircase footprint: dog-legged stair 2.4 m x 4.2 m.</summary>
    public const double StairWidth = 2400;
    public const double StairLength = 4200;

    public static List<RoomSpec>[] Build(Core.Model.Brief brief)
    {
        var floors = Enumerable.Range(0, brief.Floors).Select(_ => new List<RoomSpec>()).ToArray();
        var totals = new Dictionary<RoomType, int>();
        var bathSize = Brief.RoomDefaults.SizeMm(RoomType.Bath);

        IEnumerable<(RoomRequest Req, int Index)> Expand() =>
            brief.Rooms.Where(r => r.Floor >= 0 && r.Floor < brief.Floors)
                .SelectMany(r => Enumerable.Range(0, Math.Max(1, r.Count)).Select(i => (r, i)));

        foreach (var (req, _) in Expand())
        {
            totals[req.Type] = totals.GetValueOrDefault(req.Type) + 1;
            if (req.AttachedBath) totals[RoomType.Bath] = totals.GetValueOrDefault(RoomType.Bath) + 1;
        }

        var counters = new Dictionary<RoomType, int>();
        string NameFor(RoomType t, string? custom)
        {
            if (!string.IsNullOrWhiteSpace(custom)) return custom;
            counters[t] = counters.GetValueOrDefault(t) + 1;
            var baseName = RoomTypes.DisplayName(t);
            return totals.GetValueOrDefault(t) > 1 ? $"{baseName} {counters[t]}" : baseName;
        }

        foreach (var (req, i) in Expand().OrderBy(x => x.Req.Floor).ThenBy(x => x.Req.Type))
        {
            var name = NameFor(req.Type, req.Count > 1 ? null : req.Name);
            var key = $"{req.Floor}:{name}";
            floors[req.Floor].Add(new RoomSpec(key, req.Type, name, req.TargetArea, req.Floor, null));
            if (req.AttachedBath)
            {
                var bathName = NameFor(RoomType.Bath, null);
                floors[req.Floor].Add(new RoomSpec($"{req.Floor}:{bathName}", RoomType.Bath, bathName,
                    bathSize.Width * bathSize.Depth, req.Floor, key));
            }
        }

        if (brief.Floors > 1)
            for (var f = 0; f < brief.Floors; f++)
                floors[f].Add(new RoomSpec($"{f}:Staircase", RoomType.Staircase, "Staircase", StairWidth * StairLength, f, null));
        return floors;
    }
}
