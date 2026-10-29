using System.Text.Json.Serialization;

namespace BuildingStudio.Core.Geometry;

/// <summary>Axis-aligned rectangle in plan (millimetres).</summary>
public readonly record struct Rect2(double X, double Y, double Width, double Height)
{
    [JsonIgnore] public double MinX => X;
    [JsonIgnore] public double MinY => Y;
    [JsonIgnore] public double MaxX => X + Width;
    [JsonIgnore] public double MaxY => Y + Height;
    [JsonIgnore] public double Area => Width * Height;
    [JsonIgnore] public Vec2 Center => new(X + Width / 2, Y + Height / 2);
    [JsonIgnore] public double ShortSide => Math.Min(Width, Height);
    [JsonIgnore] public double LongSide => Math.Max(Width, Height);

    /// <summary>Long side / short side (1 = square).</summary>
    [JsonIgnore] public double Aspect => ShortSide < Tolerance.Eps ? double.PositiveInfinity : LongSide / ShortSide;

    public static Rect2 FromCorners(Vec2 a, Vec2 b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    public static Rect2 FromMinMax(double minX, double minY, double maxX, double maxY) =>
        new(minX, minY, maxX - minX, maxY - minY);

    public bool Contains(Vec2 p, double tol = Tolerance.Mm) =>
        p.X >= MinX - tol && p.X <= MaxX + tol && p.Y >= MinY - tol && p.Y <= MaxY + tol;

    public bool Contains(Rect2 r, double tol = Tolerance.Mm) =>
        r.MinX >= MinX - tol && r.MaxX <= MaxX + tol && r.MinY >= MinY - tol && r.MaxY <= MaxY + tol;

    /// <summary>True when the two rectangles share a positive-area region (touching edges do not count).</summary>
    public bool Overlaps(Rect2 o, double tol = Tolerance.Mm) =>
        Math.Min(MaxX, o.MaxX) - Math.Max(MinX, o.MinX) > tol &&
        Math.Min(MaxY, o.MaxY) - Math.Max(MinY, o.MinY) > tol;

    public double OverlapArea(Rect2 o)
    {
        var w = Math.Min(MaxX, o.MaxX) - Math.Max(MinX, o.MinX);
        var h = Math.Min(MaxY, o.MaxY) - Math.Max(MinY, o.MinY);
        return w > 0 && h > 0 ? w * h : 0;
    }

    public Rect2 Inflate(double d) => new(X - d, Y - d, Width + 2 * d, Height + 2 * d);

    public IReadOnlyList<Vec2> Corners() =>
        [new(MinX, MinY), new(MaxX, MinY), new(MaxX, MaxY), new(MinX, MaxY)];

    /// <summary>The four edges, counter-clockwise from the bottom edge, with their side.</summary>
    public IReadOnlyList<(RectSide Side, Segment2 Edge)> Edges() =>
    [
        (RectSide.Bottom, new Segment2(new(MinX, MinY), new(MaxX, MinY))),
        (RectSide.Right, new Segment2(new(MaxX, MinY), new(MaxX, MaxY))),
        (RectSide.Top, new Segment2(new(MaxX, MaxY), new(MinX, MaxY))),
        (RectSide.Left, new Segment2(new(MinX, MaxY), new(MinX, MinY))),
    ];

    /// <summary>
    /// Returns the common edge segment when the rectangles touch along a side with positive length.
    /// </summary>
    public bool TrySharedEdge(Rect2 o, out Segment2 shared, double tol = Tolerance.Mm)
    {
        shared = default;
        if (Tolerance.Near(MaxX, o.MinX, tol) || Tolerance.Near(MinX, o.MaxX, tol))
        {
            var x = Tolerance.Near(MaxX, o.MinX, tol) ? MaxX : MinX;
            var lo = Math.Max(MinY, o.MinY);
            var hi = Math.Min(MaxY, o.MaxY);
            if (hi - lo > tol) { shared = new Segment2(new(x, lo), new(x, hi)); return true; }
        }
        if (Tolerance.Near(MaxY, o.MinY, tol) || Tolerance.Near(MinY, o.MaxY, tol))
        {
            var y = Tolerance.Near(MaxY, o.MinY, tol) ? MaxY : MinY;
            var lo = Math.Max(MinX, o.MinX);
            var hi = Math.Min(MaxX, o.MaxX);
            if (hi - lo > tol) { shared = new Segment2(new(lo, y), new(hi, y)); return true; }
        }
        return false;
    }

    public double SharedEdgeLength(Rect2 o) => TrySharedEdge(o, out var s) ? s.Length : 0;

    /// <summary>Coordinate of a side (X for Left/Right, Y for Bottom/Top).</summary>
    public double SideCoordinate(RectSide side) => side switch
    {
        RectSide.Left => MinX,
        RectSide.Right => MaxX,
        RectSide.Bottom => MinY,
        _ => MaxY,
    };

    /// <summary>Returns a copy with one side moved to a new coordinate.</summary>
    public Rect2 WithSide(RectSide side, double coordinate) => side switch
    {
        RectSide.Left => FromMinMax(coordinate, MinY, MaxX, MaxY),
        RectSide.Right => FromMinMax(MinX, MinY, coordinate, MaxY),
        RectSide.Bottom => FromMinMax(MinX, coordinate, MaxX, MaxY),
        _ => FromMinMax(MinX, MinY, MaxX, coordinate),
    };

    public Polygon ToPolygon() => new(Corners());
}

/// <summary>Side of an axis-aligned rectangle.</summary>
public enum RectSide { Bottom, Right, Top, Left }
