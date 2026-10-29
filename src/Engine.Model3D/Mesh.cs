using System.Globalization;
using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Engine.Model3D;

/// <summary>Triangle mesh with flat normals, one material (colour + opacity). Millimetres, Y up.</summary>
public sealed class MeshPart(string group, string color, double opacity = 1)
{
    public string Group { get; } = group;
    public string Color { get; } = color;
    public double Opacity { get; } = opacity;
    public List<Vec3> Positions { get; } = [];
    public List<Vec3> Normals { get; } = [];
    public List<int> Indices { get; } = [];

    public int TriangleCount => Indices.Count / 3;

    /// <summary>Adds a planar quad (a, b, c, d counter-clockwise seen from the front).</summary>
    public void AddQuad(Vec3 a, Vec3 b, Vec3 c, Vec3 d)
    {
        var n = (b - a).Cross(c - a).Normalized();
        var i = Positions.Count;
        Positions.AddRange([a, b, c, d]);
        Normals.AddRange([n, n, n, n]);
        Indices.AddRange([i, i + 1, i + 2, i, i + 2, i + 3]);
    }

    public void AddTriangle(Vec3 a, Vec3 b, Vec3 c)
    {
        var n = (b - a).Cross(c - a).Normalized();
        var i = Positions.Count;
        Positions.AddRange([a, b, c]);
        Normals.AddRange([n, n, n]);
        Indices.AddRange([i, i + 1, i + 2]);
    }

    /// <summary>Axis-aligned box from plan rectangle and elevation range.</summary>
    public void AddBox(Rect2 plan, double z0, double z1) =>
        AddOrientedBox(new Vec2(plan.MinX, plan.MinY), new Vec2(1, 0), plan.Width, plan.Height, z0, z1);

    /// <summary>
    /// Box with one corner at <paramref name="origin"/>, length along <paramref name="dir"/>, width along the
    /// left normal of dir, from elevation z0 to z1.
    /// </summary>
    public void AddOrientedBox(Vec2 origin, Vec2 dir, double length, double width, double z0, double z1)
    {
        if (length <= 0.5 || width <= 0.5 || z1 - z0 <= 0.5) return;
        var n = new Vec2(-dir.Y, dir.X);
        var p = new[] { origin, origin + dir * length, origin + dir * length + n * width, origin + n * width };
        Vec3 B(int k) => Vec3.FromPlan(p[k], z0);
        Vec3 T(int k) => Vec3.FromPlan(p[k], z1);
        AddQuad(T(0), T(1), T(2), T(3));
        AddQuad(B(3), B(2), B(1), B(0));
        for (var k = 0; k < 4; k++)
        {
            var j = (k + 1) % 4;
            AddQuad(B(k), B(j), T(j), T(k));
        }
    }

    public (float R, float G, float B) Rgb()
    {
        var hex = Color.TrimStart('#');
        if (hex.Length != 6) return (0.7f, 0.7f, 0.7f);
        int V(int i) => int.Parse(hex.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (V(0) / 255f, V(2) / 255f, V(4) / 255f);
    }
}

/// <summary>A scene: mesh parts grouped by layer and material.</summary>
public sealed class Scene3D
{
    private readonly Dictionary<string, MeshPart> _parts = [];

    public IEnumerable<MeshPart> Parts => _parts.Values;

    public MeshPart Part(string group, string color, double opacity = 1)
    {
        var key = $"{group}|{color}|{opacity:0.00}";
        if (!_parts.TryGetValue(key, out var p)) _parts[key] = p = new MeshPart(group, color, opacity);
        return p;
    }

    public int TriangleCount => _parts.Values.Sum(p => p.TriangleCount);

    public (Vec3 Min, Vec3 Max) Bounds()
    {
        var all = _parts.Values.SelectMany(p => p.Positions).ToList();
        if (all.Count == 0) return (new Vec3(0, 0, 0), new Vec3(0, 0, 0));
        return (new Vec3(all.Min(v => v.X), all.Min(v => v.Y), all.Min(v => v.Z)), new Vec3(all.Max(v => v.X), all.Max(v => v.Y), all.Max(v => v.Z)));
    }
}
