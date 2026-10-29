using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Vastu;

namespace BuildingStudio.Engine.Layout;

/// <summary>A ranked plan option ready to apply to the model.</summary>
/// <param name="VastuScore">Full Vastu score of the detailed plan (includes entrance and opening rules).</param>
public sealed record LayoutOption(
    string Name, int Rank, LayoutMetrics Metrics, IReadOnlyList<Storey> Storeys, string Summary,
    IReadOnlyList<string> TradeOffs, LayoutConfig Config, int Seed, double VastuScore);

public sealed class LayoutSettings
{
    public int Iterations { get; init; } = 1200;
    public int SeedsPerConfig { get; init; } = 2;
    public double CorridorWidth { get; init; } = 1050;
}

/// <summary>
/// Generates ranked floor-plan options: corridor families (axis, stair position) × seeds, each refined by
/// simulated annealing over room order, band assignment and small-room stacking. Deterministic for a seed.
/// </summary>
public sealed class LayoutGenerator(VastuEngine vastu, BylawRulePack bylaws, LayoutSettings? settings = null)
{
    private readonly LayoutSettings _settings = settings ?? new LayoutSettings();
    private readonly LayoutScorer _scorer = new(vastu, bylaws);

    public LayoutScorer Scorer => _scorer;

    public IReadOnlyList<LayoutOption> Generate(Core.Model.Brief brief, int count = 5, int seed = 1,
        IReadOnlyDictionary<string, Direction>? locks = null)
    {
        var env = brief.Site.Envelope();
        var program = RoomProgram.Build(brief);
        var stairs = program.Select(f => f.FirstOrDefault(s => s.Type == RoomType.Staircase)).ToArray();
        var placeable = program.Select(f => f.Where(s => s.Type != RoomType.Staircase).ToList()).ToArray();

        var candidates = new List<(Genome G, List<Storey> Storeys, LayoutMetrics M, int Seed)>();
        var configs = Configs(brief, env);
        var seedsPerConfig = Math.Max(_settings.SeedsPerConfig, (int)Math.Ceiling(count * 2.0 / Math.Max(1, configs.Count)));
        for (var ci = 0; ci < configs.Count; ci++)
            for (var k = 0; k < seedsPerConfig; k++)
            {
                var s = seed * 7919 + ci * 101 + k;
                var rng = new Random(s);
                var g = Initial(configs[ci], placeable, rng);
                foreach (var (best, storeys, m) in Anneal(g, brief, env, stairs, locks, rng))
                    candidates.Add((best, storeys, m, s));
            }

        var ordered = candidates.OrderByDescending(c => c.M.Fitness).ToList();
        var zones = ordered.Select(c => Zones(c.Storeys, brief.Site)).ToList();
        var picked = new List<(Genome G, List<Storey> Storeys, LayoutMetrics M, int Seed)>();
        var pickedZones = new List<List<string>>();
        // Options must differ: at least 2 real rooms in other zones, relaxed if the brief allows few variants.
        foreach (var minDiff in new[] { 2, 1 })
            for (var i = 0; i < ordered.Count && picked.Count < count; i++)
            {
                if (picked.Contains(ordered[i])) continue;
                if (pickedZones.Any(z => Differences(z, zones[i]) < minDiff)) continue;
                picked.Add(ordered[i]);
                pickedZones.Add(zones[i]);
            }
        picked = picked.OrderByDescending(c => c.M.Fitness).ToList();

        return picked.Select((c, i) =>
        {
            foreach (var st in c.Storeys) PlanDetailer.Detail(st, brief);
            var name = $"Option {(char)('A' + i)}";
            var stairNote = brief.Floors > 1 ? $", stair {StairZone(c.Storeys[0], brief.Site)}" : "";
            var full = vastu.Evaluate(new BuildingModel { Brief = brief, Storeys = c.Storeys }).Score;
            var summary = $"Vastu {full:0} · area match {c.M.AreaMatch:P0} · adjacency {c.M.Adjacency:P0} · {c.G.Config.Describe()}{stairNote}";
            return new LayoutOption(name, i + 1, c.M, c.Storeys, summary, TradeOffs(c.Storeys, brief, c.M), c.G.Config, c.Seed, full);
        }).ToList();
    }

    /// <summary>
    /// Re-solves around locked rooms: locked rooms must stay in their current Vastu zone (strong penalty otherwise).
    /// </summary>
    public IReadOnlyList<LayoutOption> Resolve(BuildingModel model, int count = 3, int seed = 11)
    {
        var locks = new Dictionary<string, Direction>();
        foreach (var s in model.Storeys)
        {
            var grid = new ZoneGrid(s.Envelope, model.Site.NorthAngle);
            foreach (var r in s.Rooms.Where(r => r.Locked)) locks[$"{s.Level}:{r.Name}"] = grid.ZoneOf(r.Bounds);
        }
        var options = Generate(model.Brief, count, seed, locks);
        foreach (var o in options)
            foreach (var s in o.Storeys)
                foreach (var r in s.Rooms.Where(r => locks.ContainsKey($"{s.Level}:{r.Name}"))) r.Locked = true;
        return options;
    }

    private List<LayoutConfig> Configs(Core.Model.Brief brief, Rect2 env)
    {
        var list = new List<LayoutConfig>();
        var c = _settings.CorridorWidth;
        foreach (var alongY in new[] { true, false })
        {
            var across = alongY ? env.Width : env.Height;
            var single = (across - c) / 2 < 2700;
            if (across - c < 2400) continue;
            foreach (var low in single ? new[] { false, true } : [false])
            {
                if (brief.Floors == 1)
                {
                    list.Add(new LayoutConfig(alongY, 0, true, single, c, low));
                    continue;
                }
                foreach (var band in single ? new[] { 0 } : [0, 1])
                    foreach (var atStart in new[] { true, false })
                        list.Add(new LayoutConfig(alongY, band, atStart, single, c, low));
            }
        }
        return list;
    }

    private static Genome Initial(LayoutConfig cfg, List<RoomSpec>[] floors, Random rng)
    {
        var bandCount = cfg.SingleBand ? 1 : 2;
        var result = new List<Slot>[floors.Length][];
        for (var f = 0; f < floors.Length; f++)
        {
            var bands = Enumerable.Range(0, bandCount).Select(_ => new List<Slot>()).ToArray();
            var sums = new double[bandCount];
            foreach (var spec in floors[f].Where(s => s.ParentKey is null).OrderByDescending(s => s.TargetArea))
            {
                var b = bandCount == 1 ? 0 : sums[0] <= sums[1] ? 0 : 1;
                var group = new List<Slot> { new([spec]) };
                group.AddRange(floors[f].Where(c => c.ParentKey == spec.Key).Select(c => new Slot([c])));
                bands[b].AddRange(group);
                sums[b] += group.Sum(g => g.Area);
            }
            foreach (var band in bands) Shuffle(band, rng);
            if (bandCount == 2 && bands[1].Count == 0 && bands[0].Count > 1) { bands[1].Add(bands[0][^1]); bands[0].RemoveAt(bands[0].Count - 1); }
            result[f] = bands;
        }
        return new Genome(cfg, result);
    }

    private static void Shuffle(List<Slot> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>Simulated annealing. Returns the best few distinct layouts visited (best first).</summary>
    private IEnumerable<(Genome, List<Storey>, LayoutMetrics)> Anneal(Genome start, Core.Model.Brief brief, Rect2 env,
        IReadOnlyList<RoomSpec?> stairs, IReadOnlyDictionary<string, Direction>? locks, Random rng)
    {
        (List<Storey>, LayoutMetrics) Eval(Genome g)
        {
            var st = BandGeometry.Build(g, brief, env, stairs);
            return (st, _scorer.Score(st, brief, locks));
        }

        var pool = new Dictionary<string, (Genome G, List<Storey> S, LayoutMetrics M)>();
        void Keep(Genome g, List<Storey> st, LayoutMetrics m)
        {
            var sig = Signature(st, brief.Site);
            if (!pool.TryGetValue(sig, out var old) || old.M.Fitness < m.Fitness) pool[sig] = (g, st, m);
            if (pool.Count > 60)
                foreach (var k in pool.OrderBy(p => p.Value.M.Fitness).Take(20).Select(p => p.Key).ToList()) pool.Remove(k);
        }

        var cur = start;
        var (curSt, curM) = Eval(cur);
        Keep(cur, curSt, curM);
        double t = 8;
        var cool = Math.Pow(0.05 / 8, 1.0 / Math.Max(1, _settings.Iterations));
        for (var i = 0; i < _settings.Iterations; i++, t *= cool)
        {
            var cand = GenomeMoves.Mutate(cur, rng);
            if (cand is null) continue;
            var (st, m) = Eval(cand);
            if (m.Fitness >= curM.Fitness || rng.NextDouble() < Math.Exp((m.Fitness - curM.Fitness) / t))
            {
                (cur, curSt, curM) = (cand, st, m);
                if (i > _settings.Iterations / 3) Keep(cand, st, m);
            }
        }
        return pool.Values.OrderByDescending(p => p.M.Fitness).Take(4).Select(p => (p.G, p.S, p.M));
    }

    private static string Signature(IReadOnlyList<Storey> storeys, Site site) =>
        string.Join("|", storeys.SelectMany(s =>
        {
            var grid = new ZoneGrid(s.Envelope, site.NorthAngle);
            return s.Rooms.Where(r => r.Type != RoomType.Passage).Select(r => $"{s.Level}{r.Name}{grid.ZoneOf(r.Bounds)}").OrderBy(x => x, StringComparer.Ordinal);
        }));

    /// <summary>Multiset of "level:type:zone" for real rooms (identical rooms swapped count as the same plan).</summary>
    private static List<string> Zones(IReadOnlyList<Storey> storeys, Site site)
    {
        var list = new List<string>();
        foreach (var s in storeys)
        {
            var grid = new ZoneGrid(s.Envelope, site.NorthAngle);
            list.AddRange(s.Rooms.Where(r => r.TargetArea > 0).Select(r => $"{s.Level}:{r.Type}:{grid.ZoneOf(r.Bounds)}"));
        }
        return list;
    }

    private static int Differences(List<string> a, List<string> b)
    {
        var rest = b.ToList();
        var diff = 0;
        foreach (var x in a)
            if (!rest.Remove(x)) diff++;
        return diff;
    }

    private static Direction StairZone(Storey s, Site site) =>
        s.Rooms.Where(r => r.Type == RoomType.Staircase).Select(r => new ZoneGrid(s.Envelope, site.NorthAngle).ZoneOf(r.Bounds)).FirstOrDefault(Direction.Centre);

    private List<string> TradeOffs(IReadOnlyList<Storey> storeys, Core.Model.Brief brief, LayoutMetrics m)
    {
        var notes = new List<string>();
        if (brief.Vastu)
            foreach (var s in storeys)
            {
                var grid = new ZoneGrid(s.Envelope, brief.Site.NorthAngle);
                foreach (var r in s.Rooms)
                {
                    var res = vastu.EvaluateRoom(r, s.Level, grid, brief.Strictness, s.Rooms);
                    if (res.Fit is VastuFit.Avoid or VastuFit.Neutral) notes.Add($"Vastu relaxed: {res.Explanation}");
                }
            }
        foreach (var s in storeys)
        {
            foreach (var r in s.Rooms)
                if (bylaws.MinRoomSizes.TryGetValue(r.Type, out var min) && r.Bounds.ShortSide < min.WidthM * 1000)
                    notes.Add($"{r.Name} ({s.Name}) is {r.Bounds.ShortSide / 1000:0.00} m wide, below the {min.WidthM} m minimum.");
            var kitchen = s.Rooms.FirstOrDefault(r => r.Type == RoomType.Kitchen);
            var dining = s.Rooms.FirstOrDefault(r => r.Type == RoomType.Dining);
            if (kitchen is not null && dining is not null && kitchen.Bounds.SharedEdgeLength(dining.Bounds) < LayoutScorer.MinDoorEdge)
                notes.Add($"Kitchen and dining are not adjacent ({s.Name}).");
        }
        if (m.Unreachable > 0) notes.Add($"{m.Unreachable} room(s) not reachable from the passage.");
        return notes;
    }
}
