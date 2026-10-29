using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Layout;

/// <summary>
/// Turns a <see cref="Genome"/> into room rectangles. The envelope is cut into one or two bands
/// along a corridor; slots are laid along each band with lengths proportional to target area,
/// snapped to a 150 mm module. Every floor fully tiles the envelope.
/// </summary>
public static class BandGeometry
{
    public const double Module = 150;
    public const double MinStackDepth = 1200;

    public static double StairSlotLength(double bandDepth) =>
        bandDepth >= RoomProgram.StairLength ? RoomProgram.StairWidth : RoomProgram.StairLength;

    /// <summary>Depth of band 0 (ground-floor area balance, clamped 35-65%).</summary>
    public static double SplitDepth(Genome g, double usable, bool hasStair)
    {
        if (g.Config.SingleBand) return usable;
        double a0 = g.Floors[0][0].Sum(s => s.Area), a1 = g.Floors[0][1].Sum(s => s.Area);
        if (hasStair)
        {
            if (g.Config.StairBand == 0) a0 += RoomProgram.StairWidth * RoomProgram.StairLength;
            else a1 += RoomProgram.StairWidth * RoomProgram.StairLength;
        }
        var ratio = a0 + a1 <= 0 ? 0.5 : Math.Clamp(a0 / (a0 + a1), 0.35, 0.65);
        return Tolerance.Snap(usable * ratio, Module);
    }

    /// <summary>Builds storeys (rooms only) for a genome. Room names follow the specs.</summary>
    public static List<Storey> Build(Genome g, Core.Model.Brief brief, Rect2 env, IReadOnlyList<RoomSpec?> stairs)
    {
        var cfg = g.Config;
        var along = cfg.CorridorAlongY ? env.Height : env.Width;
        var across = cfg.CorridorAlongY ? env.Width : env.Height;
        var c = cfg.CorridorWidth;
        var hasStair = stairs.Any(s => s is not null);
        var d0 = SplitDepth(g, across - c, hasStair);
        var bands = cfg.SingleBand
            ? new[] { cfg.CorridorLow ? (Lo: c, Hi: across) : (Lo: 0.0, Hi: d0) }
            : [(0.0, d0), (d0 + c, across)];
        var corridor = cfg.SingleBand && cfg.CorridorLow ? (Lo: 0.0, Hi: c) : (Lo: d0, Hi: d0 + c);

        Rect2 ToPlan(double l0, double l1, double t0, double t1) => cfg.CorridorAlongY
            ? Rect2.FromMinMax(env.MinX + t0, env.MinY + l0, env.MinX + t1, env.MinY + l1)
            : Rect2.FromMinMax(env.MinX + l0, env.MinY + t0, env.MinX + l1, env.MinY + t1);

        var storeys = new List<Storey>();
        for (var f = 0; f < g.Floors.Length; f++)
        {
            var storey = new Storey
            {
                Level = f, Elevation = f * brief.FloorHeight, Height = brief.FloorHeight, Envelope = env,
            };
            var keyToRoom = new Dictionary<string, Room>();
            void Add(RoomSpec spec, Rect2 r)
            {
                var room = new Room { Type = spec.Type, Name = spec.Name, Bounds = r, TargetArea = spec.TargetArea };
                storey.Rooms.Add(room);
                keyToRoom[spec.Key] = room;
            }

            for (var b = 0; b < bands.Length; b++)
            {
                var (lo, hi) = bands[b];
                var depth = hi - lo;
                double start = 0, end = along;
                if (stairs[f] is { } stair && b == Math.Min(cfg.StairBand, bands.Length - 1))
                {
                    var sl = StairSlotLength(depth);
                    if (cfg.StairAtStart) { Add(stair, ToPlan(0, sl, lo, hi)); start = sl; }
                    else { Add(stair, ToPlan(along - sl, along, lo, hi)); end = along - sl; }
                }

                var slots = g.Floors[f][b].ToList();
                var lengths = SlotLengths(slots, depth, end - start);
                if (lengths.Count > slots.Count)
                {
                    var name = f == 0 ? "Sit-out" : "Terrace";
                    slots.Add(new Slot([new RoomSpec($"{f}:{name}:{b}", RoomType.Balcony, name, 0, f, null)]));
                }
                var pos = start;
                for (var i = 0; i < slots.Count; i++)
                {
                    var remaining = slots.Count - i - 1;
                    var raw = pos + lengths[i];
                    var next = i == slots.Count - 1 ? end
                        : Math.Clamp(Tolerance.Snap(raw, Module), pos + 300, end - 300 * remaining);
                    PlaceSlot(slots[i], b, lo, hi, pos, next, ToPlan, Add);
                    pos = next;
                }
            }

            storey.Rooms.Add(new Room
            {
                Type = RoomType.Passage, Name = f == 0 ? "Passage / Foyer" : "Passage / Lobby",
                Bounds = ToPlan(0, along, corridor.Lo, corridor.Hi),
            });

            foreach (var spec in g.AllSlots(f).SelectMany(s => s.Rooms).Where(s => s.ParentKey is not null))
                if (keyToRoom.TryGetValue(spec.Key, out var child) && keyToRoom.TryGetValue(spec.ParentKey!, out var parent))
                    child.ParentId = parent.Id;
            storeys.Add(storey);
        }
        return storeys;
    }

    /// <summary>Largest growth over target area before leftover space becomes an open sit-out/terrace.</summary>
    public const double MaxGrowth = 1.35;

    /// <summary>
    /// Slot lengths along a band. When the band is short, slots shrink proportionally. When it is long,
    /// slots grow up to <see cref="MaxGrowth"/> x target area; any remainder (>= 1.5 m) is returned as an
    /// extra trailing length for an open sit-out/terrace.
    /// </summary>
    public static List<double> SlotLengths(IReadOnlyList<Slot> slots, double depth, double available)
    {
        var natural = slots.Select(s => s.Area / depth).ToList();
        var sum = natural.Sum();
        if (slots.Count == 0) return [];
        if (sum <= 0) return slots.Select(_ => available / slots.Count).ToList();
        if (sum * MaxGrowth >= available) return natural.Select(n => n * available / sum).ToList();
        var grown = natural.Select(n => n * MaxGrowth).ToList();
        var extra = available - grown.Sum();
        if (extra >= 1500) return [.. grown, extra];
        return natural.Select(n => n * available / sum).ToList();
    }

    private static void PlaceSlot(Slot slot, int band, double lo, double hi, double l0, double l1,
        Func<double, double, double, double, Rect2> toPlan, Action<RoomSpec, Rect2> add)
    {
        if (slot.Rooms.Count == 1)
        {
            add(slot.Rooms[0], toPlan(l0, l1, lo, hi));
            return;
        }
        var depth = hi - lo;
        var a0 = slot.Rooms[0].TargetArea;
        var a1 = slot.Rooms[1].TargetArea;
        var e = Math.Clamp(Tolerance.Snap(depth * a0 / Math.Max(1, a0 + a1), Module), MinStackDepth, Math.Max(MinStackDepth, depth - MinStackDepth));
        // Room 0 sits on the exterior side: low side for band 0, high side for band 1 (or a single band beside a low corridor).
        if (band == 0 && lo == 0)
        {
            add(slot.Rooms[0], toPlan(l0, l1, lo, lo + e));
            add(slot.Rooms[1], toPlan(l0, l1, lo + e, hi));
        }
        else
        {
            add(slot.Rooms[0], toPlan(l0, l1, hi - e, hi));
            add(slot.Rooms[1], toPlan(l0, l1, lo, hi - e));
        }
    }
}
