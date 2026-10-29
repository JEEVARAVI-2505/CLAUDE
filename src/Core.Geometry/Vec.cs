namespace BuildingStudio.Core.Geometry;

/// <summary>2D vector / point in plan coordinates (millimetres). X = plan right, Y = plan up.</summary>
public readonly record struct Vec2(double X, double Y)
{
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, double s) => new(a.X * s, a.Y * s);

    public double Length() => Math.Sqrt(X * X + Y * Y);
    public double Dot(Vec2 o) => X * o.X + Y * o.Y;
    public double DistanceTo(Vec2 o) => (this - o).Length();

    public Vec2 Normalized()
    {
        var l = Length();
        return l < Tolerance.Eps ? new Vec2(0, 0) : new Vec2(X / l, Y / l);
    }

    /// <summary>Rotates counter-clockwise by <paramref name="radians"/>.</summary>
    public Vec2 Rotate(double radians)
    {
        var c = Math.Cos(radians);
        var s = Math.Sin(radians);
        return new Vec2(X * c - Y * s, X * s + Y * c);
    }
}

/// <summary>3D vector. Y is up (elevation); plan Y maps to -Z.</summary>
public readonly record struct Vec3(double X, double Y, double Z)
{
    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);

    public Vec3 Cross(Vec3 o) => new(Y * o.Z - Z * o.Y, Z * o.X - X * o.Z, X * o.Y - Y * o.X);
    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

    public Vec3 Normalized()
    {
        var l = Length();
        return l < Tolerance.Eps ? new Vec3(0, 0, 0) : this * (1 / l);
    }

    /// <summary>Maps a plan point and an elevation to 3D (plan Y becomes -Z).</summary>
    public static Vec3 FromPlan(Vec2 p, double elevation) => new(p.X, elevation, -p.Y);
}
