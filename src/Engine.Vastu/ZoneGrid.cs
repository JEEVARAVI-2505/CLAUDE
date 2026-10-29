using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Vastu;

/// <summary>
/// 3x3 Vastu grid (9 zones) over a rectangle, oriented by the north angle. Points are normalised to the
/// rectangle, rotated into the true-north frame, then binned into thirds.
/// </summary>
public sealed class ZoneGrid(Rect2 area, double northAngle)
{
    public Rect2 Area { get; } = area;
    public double NorthAngle { get; } = northAngle;

    public Direction ZoneOf(Vec2 p)
    {
        var c = Area.Center;
        var u = (p.X - c.X) / (Area.Width / 2);
        var v = (p.Y - c.Y) / (Area.Height / 2);
        var t = new Vec2(u, v).Rotate(-NorthAngle);
        const double third = 1.0 / 3.0;
        var col = t.X < -third ? -1 : t.X > third ? 1 : 0;
        var row = t.Y < -third ? -1 : t.Y > third ? 1 : 0;
        return (col, row) switch
        {
            (0, 0) => Direction.Centre,
            (0, 1) => Direction.N,
            (1, 1) => Direction.NE,
            (1, 0) => Direction.E,
            (1, -1) => Direction.SE,
            (0, -1) => Direction.S,
            (-1, -1) => Direction.SW,
            (-1, 0) => Direction.W,
            _ => Direction.NW,
        };
    }

    public Direction ZoneOf(Rect2 r) => ZoneOf(r.Center);
}
