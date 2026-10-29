namespace BuildingStudio.Core.Geometry;

/// <summary>Line segment in plan.</summary>
public readonly record struct Segment2(Vec2 A, Vec2 B)
{
    public double Length => A.DistanceTo(B);
    public Vec2 Midpoint => new((A.X + B.X) / 2, (A.Y + B.Y) / 2);
    public Vec2 Direction => (B - A).Normalized();
    public bool IsHorizontal => Math.Abs(A.Y - B.Y) < Tolerance.Mm;
    public bool IsVertical => Math.Abs(A.X - B.X) < Tolerance.Mm;

    /// <summary>Point at a distance from A along the segment.</summary>
    public Vec2 PointAtDistance(double d) => A + Direction * d;

    /// <summary>True when the point lies on the segment within tolerance.</summary>
    public bool ContainsPoint(Vec2 p, double tol = Tolerance.Mm)
    {
        var ab = B - A;
        var len2 = ab.Dot(ab);
        if (len2 < Tolerance.Eps) return p.DistanceTo(A) <= tol;
        var t = (p - A).Dot(ab) / len2;
        if (t < -tol / Math.Sqrt(len2) || t > 1 + tol / Math.Sqrt(len2)) return false;
        var proj = A + ab * Math.Clamp(t, 0, 1);
        return proj.DistanceTo(p) <= tol;
    }

    /// <summary>Same segment with A/B ordered so that A has the smaller X (or Y when vertical).</summary>
    public Segment2 Canonical() =>
        (A.X < B.X - Tolerance.Mm || (Math.Abs(A.X - B.X) <= Tolerance.Mm && A.Y <= B.Y)) ? this : new(B, A);
}
