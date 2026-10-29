using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Layout;

/// <summary>
/// Interactive edits that keep the floor fully tiled: swapping rooms and moving a shared wall
/// (all rooms along the wall line adjust). Returns new bounds for a SetRoomBoundsCommand, or null if invalid.
/// </summary>
public static class PlanEditor
{
    public const double MinRoomSize = 900;

    /// <summary>Swaps the positions of two rooms. Locked rooms cannot move.</summary>
    public static Dictionary<Guid, Rect2>? Swap(Storey storey, Guid a, Guid b)
    {
        var ra = storey.FindRoom(a);
        var rb = storey.FindRoom(b);
        if (ra is null || rb is null || a == b || ra.Locked || rb.Locked) return null;
        return new Dictionary<Guid, Rect2> { [a] = rb.Bounds, [b] = ra.Bounds };
    }

    /// <summary>
    /// Moves the wall on one side of a room to a new coordinate. Every room whose edge lies on the same
    /// continuous wall line moves with it. Exterior walls cannot move.
    /// </summary>
    public static Dictionary<Guid, Rect2>? MoveEdge(Storey storey, Guid roomId, RectSide side, double newCoordinate,
        double module = 150, double minSize = MinRoomSize)
    {
        var room = storey.FindRoom(roomId);
        if (room is null) return null;
        var vertical = side is RectSide.Left or RectSide.Right;
        var c = room.Bounds.SideCoordinate(side);
        var env = storey.Envelope;
        if (vertical ? Tolerance.Near(c, env.MinX) || Tolerance.Near(c, env.MaxX) : Tolerance.Near(c, env.MinY) || Tolerance.Near(c, env.MaxY))
            return null;

        var target = module > 0 ? Tolerance.Snap(newCoordinate, module) : newCoordinate;
        if (Tolerance.Near(target, c)) return null;

        (double Lo, double Hi) Span(Rect2 r) => vertical ? (r.MinY, r.MaxY) : (r.MinX, r.MaxX);
        bool OnLine(Room r) => vertical
            ? Tolerance.Near(r.Bounds.MaxX, c) || Tolerance.Near(r.Bounds.MinX, c)
            : Tolerance.Near(r.Bounds.MaxY, c) || Tolerance.Near(r.Bounds.MinY, c);

        var set = new List<Room> { room };
        bool grew;
        do
        {
            grew = false;
            foreach (var r in storey.Rooms.Where(r => !set.Contains(r) && OnLine(r)))
            {
                var (lo, hi) = Span(r.Bounds);
                if (!set.Any(x => Math.Min(hi, Span(x.Bounds).Hi) - Math.Max(lo, Span(x.Bounds).Lo) > Tolerance.Mm)) continue;
                set.Add(r);
                grew = true;
            }
        } while (grew);

        if (set.Any(r => r.Locked)) return null;
        var result = new Dictionary<Guid, Rect2>();
        foreach (var r in set)
        {
            var b = r.Bounds;
            var lowSide = vertical ? Tolerance.Near(b.MaxX, c) : Tolerance.Near(b.MaxY, c);
            var moved = lowSide
                ? b.WithSide(vertical ? RectSide.Right : RectSide.Top, target)
                : b.WithSide(vertical ? RectSide.Left : RectSide.Bottom, target);
            if (moved.Width < minSize || moved.Height < minSize) return null;
            result[r.Id] = moved;
        }
        return result;
    }

    /// <summary>True when rooms tile the envelope exactly with no overlaps.</summary>
    public static bool IsValidTiling(Storey storey)
    {
        var rooms = storey.Rooms;
        for (var i = 0; i < rooms.Count; i++)
        {
            if (!storey.Envelope.Contains(rooms[i].Bounds)) return false;
            for (var j = i + 1; j < rooms.Count; j++)
                if (rooms[i].Bounds.Overlaps(rooms[j].Bounds)) return false;
        }
        return Math.Abs(rooms.Sum(r => r.Area) - storey.Envelope.Area) < 1e-3 * storey.Envelope.Area;
    }
}
