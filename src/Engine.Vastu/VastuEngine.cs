using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;

namespace BuildingStudio.Engine.Vastu;

/// <summary>Deterministic Vastu scoring driven by a <see cref="VastuRulePack"/>.</summary>
public sealed class VastuEngine(VastuRulePack pack)
{
    public VastuRulePack Pack { get; } = pack;

    public static VastuFit Classify(VastuZoneRule rule, Direction zone) =>
        rule.Preferred.Contains(zone) ? VastuFit.Preferred
        : rule.Acceptable.Contains(zone) ? VastuFit.Acceptable
        : rule.Avoid.Contains(zone) ? VastuFit.Avoid
        : VastuFit.Neutral;

    public double Penalty(VastuFit fit, double weight, VastuStrictness strictness)
    {
        var basePenalty = fit switch
        {
            VastuFit.Acceptable => Pack.Penalties.Acceptable,
            VastuFit.Neutral => Pack.Penalties.Neutral,
            VastuFit.Avoid => Pack.Penalties.Avoid,
            _ => 0,
        };
        return basePenalty * weight * Pack.Strictness.For(strictness);
    }

    /// <summary>Scores one room in its zone and explains the result.</summary>
    public RoomVastuResult EvaluateRoom(Room room, int level, ZoneGrid grid, VastuStrictness strictness, IReadOnlyList<Room>? floorRooms = null)
    {
        var zone = grid.ZoneOf(room.Bounds);
        if (!Pack.Rooms.TryGetValue(room.Type, out var rule))
            return new RoomVastuResult(room.Id, room.Name, room.Type, level, zone, VastuFit.NotApplicable, 0,
                $"{room.Name} in {zone}: no Vastu rule for this room type.");

        var fit = Classify(rule, zone);
        var penalty = Penalty(fit, rule.Weight, strictness);
        var text = $"{room.Name} in {zone}: {Describe(fit)}";
        if (penalty > 0) text += $", -{penalty:0.#}";
        if (fit != VastuFit.Preferred && rule.Preferred.Count > 0)
        {
            text += $". Preferred: {string.Join("/", rule.Preferred)}";
            var occupant = floorRooms?.FirstOrDefault(r => r.Id != room.Id && rule.Preferred.Contains(grid.ZoneOf(r.Bounds)));
            if (occupant is not null) text += $" (occupied by {occupant.Name})";
        }
        if (!string.IsNullOrEmpty(rule.Note)) text += $". {rule.Note}";
        return new RoomVastuResult(room.Id, room.Name, room.Type, level, zone, fit, penalty, text + ".");
    }

    /// <summary>Room and in-storey rule penalties for one storey (no score clamping).</summary>
    public (List<RoomVastuResult> Rooms, List<VastuRuleHit> Hits) EvaluateStoreyDetail(Storey storey, Site site, VastuStrictness strictness)
    {
        var grid = new ZoneGrid(storey.Envelope, site.NorthAngle);
        var rooms = storey.Rooms.Select(r => EvaluateRoom(r, storey.Level, grid, strictness, storey.Rooms)).ToList();
        var hits = new List<VastuRuleHit>();

        var x01 = Pack.Extra("V-X01");
        foreach (var pooja in storey.Rooms.Where(r => r.Type == RoomType.Pooja))
            foreach (var bath in storey.Rooms.Where(r => r.Type == RoomType.Bath && r.Bounds.SharedEdgeLength(pooja.Bounds) > 1))
                hits.Add(new VastuRuleHit(x01.Id, $"{pooja.Name} shares a wall with {bath.Name} ({storey.Name}).", x01.Penalty));

        var windows = storey.Openings.Where(o => o.Kind is OpeningKind.Window).ToList();
        if (windows.Count > 0)
        {
            double ne = 0, sw = 0;
            foreach (var o in windows)
            {
                var wall = storey.FindWall(o.WallId);
                if (wall is null || wall.Type != WallType.Exterior) continue;
                var side = ModelGeometry.CompassOf(ModelGeometry.OutwardNormal(storey, wall), site.NorthAngle);
                if (side is CardinalSide.N or CardinalSide.E) ne += o.Width * o.Height; else sw += o.Width * o.Height;
            }
            if (sw > ne + 1)
            {
                var x02 = Pack.Extra("V-X02");
                hits.Add(new VastuRuleHit(x02.Id,
                    $"{storey.Name}: south/west glazing ({sw / 1e6:0.0} m²) exceeds north/east ({ne / 1e6:0.0} m²).", x02.Penalty));
            }
        }

        foreach (var door in storey.Openings.Where(o => o.IsMainEntrance))
        {
            var wall = storey.FindWall(door.WallId);
            if (wall is null) continue;
            var center = ModelGeometry.OpeningCenter(wall, door);
            var zone = grid.ZoneOf(center);
            var fit = Classify(Pack.Entrance, zone);
            var p = Penalty(fit, Pack.Entrance.Weight, strictness);
            hits.Add(new VastuRuleHit("V-ENT", $"Main entrance in {zone} pada: {Describe(fit)}" + (p > 0 ? $", -{p:0.#}" : ""), p));
            var into = storey.Rooms.FirstOrDefault(r => r.Id == wall.RoomA);
            if (into?.Type is RoomType.Staircase or RoomType.Bath)
            {
                var x04 = Pack.Extra("V-X04");
                hits.Add(new VastuRuleHit(x04.Id, $"Main entrance opens into {into.Name}.", x04.Penalty));
            }
        }
        return (rooms, hits);
    }

    /// <summary>Full report over every storey plus site items.</summary>
    public VastuReport Evaluate(BuildingModel model)
    {
        var rooms = new List<RoomVastuResult>();
        var hits = new List<VastuRuleHit>();
        var storeyScores = new List<double>();
        foreach (var s in model.Storeys)
        {
            var (r, h) = EvaluateStoreyDetail(s, model.Site, model.Brief.Strictness);
            rooms.AddRange(r);
            hits.AddRange(h);
            storeyScores.Add(Math.Clamp(100 - r.Sum(x => x.Penalty) - h.Sum(x => x.Penalty), 0, 100));
        }

        var siteGrid = new ZoneGrid(model.Site.Plot, model.Site.NorthAngle);
        double sitePenalty = 0;
        foreach (var item in model.Exterior.SiteItems)
        {
            if (!Pack.SiteItems.TryGetValue(item.Kind, out var rule)) continue;
            var zone = siteGrid.ZoneOf(item.Bounds);
            var fit = Classify(rule, zone);
            var p = Penalty(fit, rule.Weight, model.Brief.Strictness);
            sitePenalty += p;
            hits.Add(new VastuRuleHit($"V-SITE-{item.Kind}", $"{item.Name} in {zone}: {Describe(fit)}" + (p > 0 ? $", -{p:0.#}" : ""), p));
        }

        var score = storeyScores.Count == 0 ? 0 : Math.Clamp(storeyScores.Average() - sitePenalty, 0, 100);
        return new VastuReport(Math.Round(score, 1), rooms, hits);
    }

    /// <summary>Fast score for the layout optimiser: rooms only plus pooja/toilet adjacency, clamped 0..100.</summary>
    public double QuickScore(IReadOnlyList<Room> rooms, ZoneGrid grid, VastuStrictness strictness)
    {
        double total = 0;
        foreach (var r in rooms)
        {
            if (!Pack.Rooms.TryGetValue(r.Type, out var rule)) continue;
            total += Penalty(Classify(rule, grid.ZoneOf(r.Bounds)), rule.Weight, strictness);
        }
        var x01 = Pack.Extra("V-X01").Penalty;
        foreach (var p in rooms.Where(r => r.Type == RoomType.Pooja))
            total += rooms.Count(b => b.Type == RoomType.Bath && b.Bounds.SharedEdgeLength(p.Bounds) > 1) * x01;
        return Math.Clamp(100 - total, 0, 100);
    }

    /// <summary>
    /// Suggests room swaps on a storey that raise the Vastu score, best first.
    /// Locked rooms, passages and staircases never move.
    /// </summary>
    public IReadOnlyList<FixSuggestion> SuggestFixes(BuildingModel model, int level, int max = 3)
    {
        var storey = model.GetStorey(level);
        if (storey is null) return [];
        var grid = new ZoneGrid(storey.Envelope, model.Site.NorthAngle);
        var s = model.Brief.Strictness;
        var baseline = QuickScore(storey.Rooms, grid, s);
        var baseArea = AreaMatch(storey.Rooms);
        var movable = storey.Rooms.Where(r => !r.Locked && r.Type is not (RoomType.Passage or RoomType.Staircase)).ToList();
        var result = new List<FixSuggestion>();

        for (var i = 0; i < movable.Count; i++)
        for (var j = i + 1; j < movable.Count; j++)
        {
            var a = movable[i];
            var b = movable[j];
            if (a.Type == b.Type) continue;
            var swapped = storey.Rooms.Select(r =>
                r.Id == a.Id ? Copy(r, b.Bounds) : r.Id == b.Id ? Copy(r, a.Bounds) : r).ToList();
            var gain = QuickScore(swapped, grid, s) - baseline;
            if (gain <= 0.05) continue;
            var areaChange = AreaMatch(swapped) - baseArea;
            var target = grid.ZoneOf(b.Bounds);
            result.Add(new FixSuggestion(a.Id, b.Id, level,
                $"Swap {a.Name} and {b.Name} ({a.Name} to {target}): Vastu +{gain:0.#}, area match {areaChange * 100:+0;-0;0}%",
                Math.Round(gain, 1), areaChange));
        }
        return result.OrderByDescending(f => f.VastuGain).ThenByDescending(f => f.AreaMatchChange).Take(max).ToList();
    }

    /// <summary>1 - mean relative deviation of actual area from target (rooms with a target only).</summary>
    public static double AreaMatch(IEnumerable<Room> rooms)
    {
        var withTarget = rooms.Where(r => r.TargetArea > 0).ToList();
        if (withTarget.Count == 0) return 1;
        return 1 - withTarget.Average(r => Math.Min(1, Math.Abs(r.Area - r.TargetArea) / r.TargetArea));
    }

    private static Room Copy(Room r, Rect2 bounds) => new()
    {
        Id = r.Id, Type = r.Type, Name = r.Name, Bounds = bounds, TargetArea = r.TargetArea, Locked = r.Locked, ParentId = r.ParentId,
    };

    public static string Describe(VastuFit fit) => fit switch
    {
        VastuFit.Preferred => "preferred zone",
        VastuFit.Acceptable => "acceptable",
        VastuFit.Neutral => "neutral (not listed)",
        VastuFit.Avoid => "zone to avoid",
        _ => "not applicable",
    };
}
