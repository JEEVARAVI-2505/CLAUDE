namespace BuildingStudio.Core.Geometry;

/// <summary>Simple polygon (no holes) in plan.</summary>
public sealed class Polygon
{
    public Polygon(IEnumerable<Vec2> points)
    {
        Points = points.ToArray();
        if (Points.Count < 3) throw new ArgumentException("A polygon needs at least 3 points.", nameof(points));
    }

    public IReadOnlyList<Vec2> Points { get; }

    /// <summary>Signed area (shoelace). Positive when counter-clockwise.</summary>
    public double SignedArea()
    {
        double s = 0;
        for (var i = 0; i < Points.Count; i++)
        {
            var a = Points[i];
            var b = Points[(i + 1) % Points.Count];
            s += a.X * b.Y - b.X * a.Y;
        }
        return s / 2;
    }

    public double Area => Math.Abs(SignedArea());

    public Vec2 Centroid()
    {
        double cx = 0, cy = 0, a = SignedArea();
        if (Math.Abs(a) < Tolerance.Eps) return new Vec2(Points.Average(p => p.X), Points.Average(p => p.Y));
        for (var i = 0; i < Points.Count; i++)
        {
            var p = Points[i];
            var q = Points[(i + 1) % Points.Count];
            var f = p.X * q.Y - q.X * p.Y;
            cx += (p.X + q.X) * f;
            cy += (p.Y + q.Y) * f;
        }
        return new Vec2(cx / (6 * a), cy / (6 * a));
    }

    public Rect2 Bounds() => Rect2.FromMinMax(
        Points.Min(p => p.X), Points.Min(p => p.Y), Points.Max(p => p.X), Points.Max(p => p.Y));

    /// <summary>Even-odd point-in-polygon test (boundary counts as inside).</summary>
    public bool Contains(Vec2 p)
    {
        for (var i = 0; i < Points.Count; i++)
            if (new Segment2(Points[i], Points[(i + 1) % Points.Count]).ContainsPoint(p)) return true;

        var inside = false;
        for (int i = 0, j = Points.Count - 1; i < Points.Count; j = i++)
        {
            var a = Points[i];
            var b = Points[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }
}
