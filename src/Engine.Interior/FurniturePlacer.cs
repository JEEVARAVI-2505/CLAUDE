using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;

namespace BuildingStudio.Engine.Interior;

/// <summary>
/// Places catalog items in one room against walls, in corners or in the centre while keeping door swings,
/// windows (for tall items) and each item's front clearance free. Deterministic.
/// </summary>
public sealed class FurniturePlacer(Storey storey, Room room, double northAngle)
{
    public const double WallInset = 120;
    public const double Gap = 50;
    public const double Step = 150;

    private readonly List<Rect2> _blocked = [];
    private readonly List<Rect2> _reserved = [];
    private readonly List<(Rect2 Zone, bool Tall)> _windows = [];

    public Rect2 Inner { get; } = room.Bounds.Inflate(-WallInset);
    public List<FurnitureItem> Placed { get; } = [];

    public void Prepare()
    {
        foreach (var o in storey.Openings)
        {
            var wall = storey.FindWall(o.WallId);
            if (wall is null || (wall.RoomA != room.Id && wall.RoomB != room.Id)) continue;
            var a = wall.Segment.PointAtDistance(o.Offset);
            var b = wall.Segment.PointAtDistance(o.Offset + o.Width);
            var inward = InwardNormal(wall);
            if (o.Kind == OpeningKind.Archway && room.Type == RoomType.Parking) continue; // the car drives through it
            if (o.Kind is OpeningKind.Door or OpeningKind.Archway)
            {
                // Door leaf swing radius = leaf width; archways only need a 600 mm landing.
                var depth = o.Kind == OpeningKind.Door ? o.Width : 600;
                _blocked.Add(Rect2.FromCorners(a, b + inward * depth));
            }
            else
                _windows.Add((Rect2.FromCorners(a, b + inward * 400), true));
        }
    }

    private Vec2 InwardNormal(Wall wall)
    {
        var d = wall.Segment.Direction;
        var n = new Vec2(-d.Y, d.X);
        return (room.Bounds.Center - wall.Segment.Midpoint).Dot(n) >= 0 ? n : n * -1;
    }

    /// <summary>Tries to place one catalog item; returns true when placed.</summary>
    public bool TryPlace(CatalogItem item)
    {
        var best = Candidates(item)
            .Where(c => Fits(c.Footprint, c.Clearance, item.Height))
            .OrderBy(c => c.Cost)
            .FirstOrDefault();
        if (best.Footprint.Area <= 0) return false;
        Placed.Add(new FurnitureItem
        {
            CatalogId = item.Id, Name = item.Name, RoomId = room.Id, Bounds = best.Footprint,
            Height = item.Height, Rotation = best.Rotation, Color = item.Color,
        });
        _reserved.Add(best.Footprint.Inflate(Gap));
        if (best.Clearance.Area > 0) _reserved.Add(best.Clearance);
        return true;
    }

    private bool Fits(Rect2 footprint, Rect2 clearance, double height)
    {
        if (!Inner.Contains(footprint)) return false;
        if (_blocked.Any(b => b.Overlaps(footprint))) return false;
        if (height > 1000 && _windows.Any(w => w.Zone.Overlaps(footprint))) return false;
        if (_reserved.Any(r => r.Overlaps(footprint))) return false;
        if (clearance.Area > 0)
        {
            var clipped = Rect2.FromMinMax(Math.Max(clearance.MinX, Inner.MinX), Math.Max(clearance.MinY, Inner.MinY),
                Math.Min(clearance.MaxX, Inner.MaxX), Math.Min(clearance.MaxY, Inner.MaxY));
            if (clipped.Width <= 0 || clipped.Height <= 0 || clipped.Area < 0.6 * clearance.Area) return false;
            if (Placed.Any(p => p.Bounds.Overlaps(clipped))) return false;
        }
        return true;
    }

    private IEnumerable<(Rect2 Footprint, Rect2 Clearance, double Rotation, double Cost)> Candidates(CatalogItem item)
    {
        if (item.Placement == FurniturePlacement.Centre)
        {
            var c = Inner.Center;
            for (var dx = -3; dx <= 3; dx++)
                for (var dy = -3; dy <= 3; dy++)
                    foreach (var rotated in new[] { false, true })
                    {
                        var (w, d) = rotated ? (item.Depth, item.Width) : (item.Width, item.Depth);
                        if (item.Id == "car" && rotated == (Inner.Width > Inner.Height)) continue;
                        var r = new Rect2(c.X + dx * Step - w / 2, c.Y + dy * Step - d / 2, w, d);
                        yield return (r, default, rotated ? Math.PI / 2 : 0, Math.Abs(dx) + Math.Abs(dy) + (rotated ? 0.5 : 0));
                    }
            yield break;
        }

        foreach (var side in Enum.GetValues<RectSide>())
        {
            var sideCost = SideCost(item, side);
            var along = side is RectSide.Bottom or RectSide.Top ? Inner.Width : Inner.Height;
            if (item.Width > along) continue;
            var positions = new List<double>();
            for (var t = 0.0; t <= along - item.Width + 1; t += Step) positions.Add(t);
            if (positions.Count == 0 || Math.Abs(positions[^1] - (along - item.Width)) > 1) positions.Add(along - item.Width);
            foreach (var t in positions)
            {
                var (fp, cl, rot) = Footprint(item, side, t);
                var centred = Math.Abs(t + item.Width / 2 - along / 2) / along;
                var cornered = Math.Min(t, along - item.Width - t) / along;
                var posCost = item.Placement switch
                {
                    FurniturePlacement.Corner => cornered * 10,
                    FurniturePlacement.BedWall => centred * 2,
                    _ => item.Id.StartsWith("sofa") || item.Id.StartsWith("tv") ? centred * 2 : cornered,
                };
                yield return (fp, cl, rot, sideCost + posCost);
            }
        }
    }

    /// <summary>Lower is better: Vastu bed head to S/W, cooking facing east (counter on the east wall), TV opposite the sofa.</summary>
    private double SideCost(CatalogItem item, RectSide side)
    {
        var compass = ModelGeometry.CompassOf(ModelGeometry.SideNormal(side), northAngle);
        if (item.Placement == FurniturePlacement.BedWall)
            return compass is CardinalSide.S or CardinalSide.W ? 0 : 5;
        if (item.Id.StartsWith("kitchen-counter"))
            return compass == CardinalSide.E ? 0 : 3;
        if (item.Id == "tv-unit")
        {
            var sofa = Placed.FirstOrDefault(p => p.CatalogId.StartsWith("sofa"));
            if (sofa is not null) return OppositeOf(sofa) == side ? 0 : 6;
        }
        return 1;
    }

    private RectSide OppositeOf(FurnitureItem f)
    {
        var dists = new Dictionary<RectSide, double>
        {
            [RectSide.Bottom] = f.Bounds.MinY - Inner.MinY, [RectSide.Top] = Inner.MaxY - f.Bounds.MaxY,
            [RectSide.Left] = f.Bounds.MinX - Inner.MinX, [RectSide.Right] = Inner.MaxX - f.Bounds.MaxX,
        };
        var against = dists.OrderBy(kv => kv.Value).First().Key;
        return against switch
        {
            RectSide.Bottom => RectSide.Top,
            RectSide.Top => RectSide.Bottom,
            RectSide.Left => RectSide.Right,
            _ => RectSide.Left,
        };
    }

    private (Rect2 Footprint, Rect2 Clearance, double Rotation) Footprint(CatalogItem item, RectSide side, double t)
    {
        var w = item.Width;
        var d = item.Depth;
        var c = item.FrontClearance;
        return side switch
        {
            RectSide.Bottom => (new Rect2(Inner.MinX + t, Inner.MinY, w, d), c > 0 ? new Rect2(Inner.MinX + t, Inner.MinY + d, w, c) : default, 0),
            RectSide.Top => (new Rect2(Inner.MinX + t, Inner.MaxY - d, w, d), c > 0 ? new Rect2(Inner.MinX + t, Inner.MaxY - d - c, w, c) : default, Math.PI),
            RectSide.Left => (new Rect2(Inner.MinX, Inner.MinY + t, d, w), c > 0 ? new Rect2(Inner.MinX + d, Inner.MinY + t, c, w) : default, -Math.PI / 2),
            _ => (new Rect2(Inner.MaxX - d, Inner.MinY + t, d, w), c > 0 ? new Rect2(Inner.MaxX - d - c, Inner.MinY + t, c, w) : default, Math.PI / 2),
        };
    }

    /// <summary>Door clearance zones in this room (for tests).</summary>
    public IReadOnlyList<Rect2> DoorZones => _blocked;
}
