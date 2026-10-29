using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Vastu;

namespace BuildingStudio.Engine.Layout;

/// <summary>Fitness terms (each 0..1 except Vastu 0..100) and the weighted total (0..100 minus penalties).</summary>
public sealed record LayoutMetrics(
    double Vastu, double AreaMatch, double Adjacency, double Daylight, double Frontage, double Compactness,
    int Unreachable, double LockPenalty, double Fitness);

/// <summary>Scores a candidate layout. Deterministic.</summary>
public sealed class LayoutScorer(VastuEngine vastu, BylawRulePack bylaws)
{
    public const double MinDoorEdge = 750;

    /// <summary>No room (other than passages) should be narrower than this, mm.</summary>
    public const double MinUsableWidth = 1200;

    private static readonly (RoomType A, RoomType B, double W)[] Pairs =
    [
        (RoomType.Kitchen, RoomType.Dining, 1.0),
        (RoomType.Dining, RoomType.Living, 0.6),
        (RoomType.Utility, RoomType.Kitchen, 0.4),
    ];

    public LayoutMetrics Score(IReadOnlyList<Storey> storeys, Core.Model.Brief brief, IReadOnlyDictionary<string, Direction>? locks = null)
    {
        var site = brief.Site;
        var vastuScore = storeys.Average(s => vastu.QuickScore(s.Rooms, new ZoneGrid(s.Envelope, site.NorthAngle), brief.Strictness));
        var area = VastuEngine.AreaMatch(storeys.SelectMany(s => s.Rooms));
        var adjacency = Adjacency(storeys);
        var daylight = Daylight(storeys);
        var frontage = Frontage(storeys[0], site);
        var compact = Compactness(storeys);
        var unreachable = storeys.Sum(Unreachable);

        double lockPenalty = 0;
        if (locks is { Count: > 0 })
            foreach (var s in storeys)
            {
                var grid = new ZoneGrid(s.Envelope, site.NorthAngle);
                foreach (var r in s.Rooms)
                    if (locks.TryGetValue($"{s.Level}:{r.Name}", out var z) && grid.ZoneOf(r.Bounds) != z) lockPenalty += 30;
            }

        double wv = brief.Vastu ? 0.30 : 0, wa = 0.15, wj = 0.15, wd = 0.10, wf = 0.12, wc = 0.18;
        var weighted = (wv * vastuScore / 100 + wa * area + wj * adjacency + wd * daylight + wf * frontage + wc * compact)
                       / (wv + wa + wj + wd + wf + wc);
        var rooms = storeys.SelectMany(s => s.Rooms).Where(r => r.Type is not (RoomType.Passage or RoomType.Staircase)).ToList();
        var narrow = rooms.Count(r => r.Bounds.ShortSide < MinUsableWidth);
        var elongated = rooms.Count(r => r.Bounds.Aspect > 3);
        var front = ModelGeometry.FrontSide(site);
        var parkingOffRoad = storeys[0].Rooms.Count(r => r.Type == RoomType.Parking
            && !ModelGeometry.ExteriorEdges(r.Bounds, storeys[0].Envelope).Any(e => e.Side == front && e.Edge.Length >= 2400));
        var fitness = 100 * weighted - unreachable * 15 - lockPenalty - narrow * 6 - elongated * 3 - parkingOffRoad * 10;
        return new LayoutMetrics(vastuScore, area, adjacency, daylight, frontage, compact, unreachable, lockPenalty, fitness);
    }

    private static double Adjacency(IReadOnlyList<Storey> storeys)
    {
        double got = 0, total = 0;
        foreach (var s in storeys)
        {
            foreach (var (a, b, w) in Pairs)
            {
                var ra = s.Rooms.Where(r => r.Type == a).ToList();
                var rb = s.Rooms.Where(r => r.Type == b).ToList();
                if (ra.Count == 0 || rb.Count == 0) continue;
                total += w;
                if (ra.Any(x => rb.Any(y => x.Bounds.SharedEdgeLength(y.Bounds) >= MinDoorEdge))) got += w;
            }
            foreach (var child in s.Rooms.Where(r => r.ParentId is not null))
            {
                total += 1;
                var parent = s.FindRoom(child.ParentId!.Value);
                if (parent is not null && child.Bounds.SharedEdgeLength(parent.Bounds) >= MinDoorEdge) got += 1;
            }
            var below = storeys.FirstOrDefault(x => x.Level == s.Level - 1);
            if (below is null) continue;
            foreach (var bath in s.Rooms.Where(r => r.Type == RoomType.Bath))
            {
                total += 0.3;
                if (below.Rooms.Any(r => RoomTypes.IsWet(r.Type) && r.Bounds.OverlapArea(bath.Bounds) > 0.3 * bath.Area)) got += 0.3;
            }
        }
        return total == 0 ? 1 : got / total;
    }

    private static double Daylight(IReadOnlyList<Storey> storeys)
    {
        double got = 0, total = 0;
        foreach (var s in storeys)
            foreach (var r in s.Rooms)
            {
                var w = RoomTypes.IsHabitable(r.Type) ? 1.0 : r.Type == RoomType.Bath ? 0.5 : 0;
                if (w == 0) continue;
                total += w;
                if (ModelGeometry.ExteriorEdges(r.Bounds, s.Envelope).Any(e => e.Edge.Length >= 900)) got += w;
            }
        return total == 0 ? 1 : got / total;
    }

    private static double Frontage(Storey ground, Site site)
    {
        var front = ModelGeometry.FrontSide(site);
        bool Touches(Room r) => ModelGeometry.ExteriorEdges(r.Bounds, ground.Envelope).Any(e => e.Side == front && e.Edge.Length >= 900);
        double got = 0, total = 0;
        foreach (var r in ground.Rooms.Where(r => r.Type is RoomType.Living or RoomType.Parking))
        {
            var w = r.Type == RoomType.Living ? 1.5 : 1.0;
            total += w;
            if (Touches(r)) got += w;
        }
        foreach (var r in ground.Rooms.Where(r => r.Type is RoomType.Passage))
        {
            total += 0.5;
            if (Touches(r)) got += 0.5;
        }
        return total == 0 ? 1 : got / total;
    }

    private double Compactness(IReadOnlyList<Storey> storeys)
    {
        var scores = new List<double>();
        foreach (var r in storeys.SelectMany(s => s.Rooms))
        {
            if (r.Type is RoomType.Passage or RoomType.Staircase) continue;
            var s = 1.0;
            if (r.Bounds.Aspect > 2) s -= 0.5 * Math.Min(1, (r.Bounds.Aspect - 2) / 2);
            if (bylaws.MinRoomSizes.TryGetValue(r.Type, out var min) && r.Bounds.ShortSide < min.WidthM * 1000) s -= 0.5;
            if (r.Bounds.ShortSide < MinUsableWidth) s -= 0.7;
            scores.Add(Math.Max(0, s));
        }
        return scores.Count == 0 ? 1 : scores.Average();
    }

    /// <summary>Rooms people may walk through to reach other rooms.</summary>
    public static bool IsPublic(RoomType t) => t is RoomType.Passage or RoomType.Living or RoomType.Dining or RoomType.Lounge or RoomType.Kitchen;

    /// <summary>
    /// Rooms that cannot be reached through door-width shared edges from the main passage. Private rooms
    /// (bedrooms, baths) are not walked through, except into their own attached bath or a balcony.
    /// Parking is reached from the road.
    /// </summary>
    public static int Unreachable(Storey s)
    {
        var reached = new HashSet<Guid>(s.Rooms.Where(r => r.Type == RoomType.Passage && r.Name.StartsWith("Passage", StringComparison.Ordinal)).Select(r => r.Id));
        foreach (var p in s.Rooms.Where(r => r.Type is RoomType.Parking)) reached.Add(p.Id);
        bool changed;
        do
        {
            changed = false;
            foreach (var r in s.Rooms.Where(r => !reached.Contains(r.Id)))
            {
                var via = s.Rooms.Any(o => reached.Contains(o.Id) && o.Type is not (RoomType.Parking or RoomType.Staircase)
                    && o.Bounds.SharedEdgeLength(r.Bounds) >= MinDoorEdge
                    && (IsPublic(o.Type) || r.ParentId == o.Id || r.Type == RoomType.Balcony));
                if (!via) continue;
                reached.Add(r.Id);
                changed = true;
            }
        } while (changed);
        return s.Rooms.Count(r => !reached.Contains(r.Id));
    }
}
