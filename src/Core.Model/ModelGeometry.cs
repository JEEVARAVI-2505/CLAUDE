using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Core.Model;

/// <summary>Geometric queries on a storey shared by several engines.</summary>
public static class ModelGeometry
{
    /// <summary>
    /// Outward unit normal (plan) of an exterior wall, based on which side of the envelope it lies on.
    /// Returns (0,0) for interior walls.
    /// </summary>
    public static Vec2 OutwardNormal(Storey storey, Wall wall)
    {
        if (wall.Type != WallType.Exterior) return new Vec2(0, 0);
        var env = storey.Envelope;
        var m = wall.Segment.Midpoint;
        if (wall.Segment.IsVertical)
            return Math.Abs(m.X - env.MinX) < Math.Abs(m.X - env.MaxX) ? new Vec2(-1, 0) : new Vec2(1, 0);
        return Math.Abs(m.Y - env.MinY) < Math.Abs(m.Y - env.MaxY) ? new Vec2(0, -1) : new Vec2(0, 1);
    }

    /// <summary>Compass side a plan direction points to, given the north angle.</summary>
    public static CardinalSide CompassOf(Vec2 planDirection, double northAngle)
    {
        var t = planDirection.Rotate(-northAngle);
        if (Math.Abs(t.X) >= Math.Abs(t.Y)) return t.X >= 0 ? CardinalSide.E : CardinalSide.W;
        return t.Y >= 0 ? CardinalSide.N : CardinalSide.S;
    }

    /// <summary>Plan direction (unit) of a compass side, given the north angle.</summary>
    public static Vec2 PlanDirectionOf(CardinalSide side, double northAngle)
    {
        var v = side switch
        {
            CardinalSide.N => new Vec2(0, 1),
            CardinalSide.S => new Vec2(0, -1),
            CardinalSide.E => new Vec2(1, 0),
            _ => new Vec2(-1, 0),
        };
        return v.Rotate(northAngle);
    }

    /// <summary>Centre point of an opening along its wall.</summary>
    public static Vec2 OpeningCenter(Wall wall, Opening o) => wall.Segment.PointAtDistance(o.Offset + o.Width / 2);

    /// <summary>Exterior side (plan axis) of the envelope that an exterior wall lies on.</summary>
    public static RectSide EnvelopeSide(Storey storey, Wall wall)
    {
        var n = OutwardNormal(storey, wall);
        if (n.X < -0.5) return RectSide.Left;
        if (n.X > 0.5) return RectSide.Right;
        return n.Y < 0 ? RectSide.Bottom : RectSide.Top;
    }

    /// <summary>Room edges that lie on the envelope boundary, with their envelope side.</summary>
    public static IEnumerable<(RectSide Side, Segment2 Edge)> ExteriorEdges(Rect2 room, Rect2 envelope)
    {
        foreach (var (side, edge) in room.Edges())
        {
            var onBoundary = side switch
            {
                RectSide.Left => Tolerance.Near(room.MinX, envelope.MinX),
                RectSide.Right => Tolerance.Near(room.MaxX, envelope.MaxX),
                RectSide.Bottom => Tolerance.Near(room.MinY, envelope.MinY),
                _ => Tolerance.Near(room.MaxY, envelope.MaxY),
            };
            if (onBoundary) yield return (side, edge);
        }
    }

    /// <summary>Plan direction of an envelope side.</summary>
    public static Vec2 SideNormal(RectSide side) => side switch
    {
        RectSide.Left => new Vec2(-1, 0),
        RectSide.Right => new Vec2(1, 0),
        RectSide.Bottom => new Vec2(0, -1),
        _ => new Vec2(0, 1),
    };

    /// <summary>Envelope side that faces the road (front), given the plot facing and north angle.</summary>
    public static RectSide FrontSide(Site site)
    {
        var d = PlanDirectionOf(site.Facing, site.NorthAngle);
        if (Math.Abs(d.X) >= Math.Abs(d.Y)) return d.X >= 0 ? RectSide.Right : RectSide.Left;
        return d.Y >= 0 ? RectSide.Top : RectSide.Bottom;
    }
}
