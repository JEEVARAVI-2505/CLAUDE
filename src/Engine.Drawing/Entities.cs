using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Engine.Drawing;

/// <summary>CAD layer names (AIA-style).</summary>
public static class Layers
{
    public const string Wall = "A-WALL";
    public const string Room = "A-AREA";
    public const string Door = "A-DOOR";
    public const string Glazing = "A-GLAZ";
    public const string Text = "A-ANNO-TEXT";
    public const string Dims = "A-ANNO-DIMS";
    public const string Furniture = "A-FURN";
    public const string Elevation = "A-ELEV";
    public const string Section = "A-SECT";
    public const string Site = "C-SITE";
    public const string Column = "S-COLM";
    public const string Beam = "S-BEAM";
    public const string Footing = "S-FNDN";
    public const string Grid = "S-GRID";
    public const string Title = "G-TTLB";

    public static readonly IReadOnlyDictionary<string, string> Colors = new Dictionary<string, string>
    {
        [Wall] = "#1b1b1b", [Room] = "#cccccc", [Door] = "#8e44ad", [Glazing] = "#2471a3", [Text] = "#1b1b1b",
        [Dims] = "#616a6b", [Furniture] = "#a04000", [Elevation] = "#1b1b1b", [Section] = "#1b1b1b", [Site] = "#196f3d",
        [Column] = "#c0392b", [Beam] = "#d35400", [Footing] = "#7d3c98", [Grid] = "#5d6d7e", [Title] = "#1b1b1b",
    };
}

public enum TextAlign { Left, Center, Right }

/// <summary>Base drawing entity. Coordinates in millimetres (model space), Y up.</summary>
public abstract record DrawEntity(string Layer);

public sealed record LineEntity(string Layer, Vec2 A, Vec2 B, double Weight = 0.25, bool Dashed = false) : DrawEntity(Layer);

public sealed record PolygonEntity(string Layer, IReadOnlyList<Vec2> Points, string? Fill = null, double Weight = 0.25, bool Closed = true, bool Dashed = false)
    : DrawEntity(Layer)
{
    public static PolygonEntity Rect(string layer, Rect2 r, string? fill = null, double weight = 0.25, bool dashed = false) =>
        new(layer, r.Corners(), fill, weight, true, dashed);
}

public sealed record TextEntity(string Layer, Vec2 Position, string Text, double Height, TextAlign Align = TextAlign.Center, bool Bold = false)
    : DrawEntity(Layer);

/// <summary>Circular arc, angles in radians counter-clockwise from +X.</summary>
public sealed record ArcEntity(string Layer, Vec2 Center, double Radius, double StartAngle, double EndAngle, double Weight = 0.18) : DrawEntity(Layer);

public sealed record CircleEntity(string Layer, Vec2 Center, double Radius, string? Fill = null) : DrawEntity(Layer);

/// <summary>Linear dimension between two points, drawn offset to the left of A→B.</summary>
public sealed record DimensionEntity(string Layer, Vec2 A, Vec2 B, double Offset, string? Text = null, double TextHeight = 180) : DrawEntity(Layer)
{
    public IEnumerable<DrawEntity> Expand()
    {
        var dir = (B - A).Normalized();
        var normal = new Vec2(-dir.Y, dir.X);
        var a = A + normal * Offset;
        var b = B + normal * Offset;
        yield return new LineEntity(Layer, a, b, 0.18);
        yield return new LineEntity(Layer, A + normal * (Offset * 0.15), a + normal * (Math.Sign(Offset) * 100), 0.13);
        yield return new LineEntity(Layer, B + normal * (Offset * 0.15), b + normal * (Math.Sign(Offset) * 100), 0.13);
        var tick = (dir + normal) * 70;
        yield return new LineEntity(Layer, a - tick, a + tick, 0.35);
        yield return new LineEntity(Layer, b - tick, b + tick, 0.35);
        var label = Text ?? $"{A.DistanceTo(B):0}";
        var mid = (a + b) * 0.5 + normal * (TextHeight * 0.7 * Math.Sign(Offset == 0 ? 1 : Offset));
        yield return new TextEntity(Layer, mid, label, TextHeight);
    }
}

/// <summary>A simple table expanded into lines and text.</summary>
public sealed record TableEntity(string Layer, Vec2 TopLeft, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<double> ColumnWidths, double TextHeight = 200) : DrawEntity(Layer)
{
    public double RowHeight => TextHeight * 2;
    public double Width => ColumnWidths.Sum();
    public double Height => RowHeight * (Rows.Count + 1);

    public IEnumerable<DrawEntity> Expand()
    {
        var all = new List<IReadOnlyList<string>> { Headers };
        all.AddRange(Rows);
        for (var r = 0; r <= all.Count; r++)
        {
            var y = TopLeft.Y - r * RowHeight;
            yield return new LineEntity(Layer, new Vec2(TopLeft.X, y), new Vec2(TopLeft.X + Width, y), r <= 1 ? 0.35 : 0.13);
        }
        double x = TopLeft.X;
        for (var c = 0; c <= ColumnWidths.Count; c++)
        {
            yield return new LineEntity(Layer, new Vec2(x, TopLeft.Y), new Vec2(x, TopLeft.Y - Height), 0.13);
            if (c < ColumnWidths.Count) x += ColumnWidths[c];
        }
        for (var r = 0; r < all.Count; r++)
        {
            x = TopLeft.X;
            for (var c = 0; c < ColumnWidths.Count; c++)
            {
                var text = c < all[r].Count ? all[r][c] : "";
                yield return new TextEntity(Layer, new Vec2(x + TextHeight * 0.4, TopLeft.Y - r * RowHeight - RowHeight * 0.68), text, TextHeight, TextAlign.Left, r == 0);
                x += ColumnWidths[c];
            }
        }
    }
}

/// <summary>One drawing sheet (plan, elevation, section, schedule...).</summary>
public sealed class DrawingSheet
{
    public string Name { get; init; } = "";
    public string Title { get; init; } = "";
    public int Scale { get; init; } = 100;
    public List<DrawEntity> Entities { get; } = [];
    public List<string> Notes { get; } = [];

    /// <summary>Entities with composites (dimensions, tables) expanded into primitives.</summary>
    public IEnumerable<DrawEntity> Primitives() => Entities.SelectMany(e => e switch
    {
        DimensionEntity d => d.Expand(),
        TableEntity t => t.Expand(),
        _ => [e],
    });

    public Rect2 Extents()
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        void Add(Vec2 p) { minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y); maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y); }
        foreach (var e in Primitives())
        {
            switch (e)
            {
                case LineEntity l: Add(l.A); Add(l.B); break;
                case PolygonEntity p: foreach (var q in p.Points) Add(q); break;
                case TextEntity t:
                    var w = t.Text.Length * t.Height * 0.6;
                    var x0 = t.Align switch { TextAlign.Left => t.Position.X, TextAlign.Right => t.Position.X - w, _ => t.Position.X - w / 2 };
                    Add(new Vec2(x0, t.Position.Y - t.Height * 0.3)); Add(new Vec2(x0 + w, t.Position.Y + t.Height)); break;
                case ArcEntity a: Add(a.Center - new Vec2(a.Radius, a.Radius)); Add(a.Center + new Vec2(a.Radius, a.Radius)); break;
                case CircleEntity c: Add(c.Center - new Vec2(c.Radius, c.Radius)); Add(c.Center + new Vec2(c.Radius, c.Radius)); break;
            }
        }
        return minX == double.MaxValue ? new Rect2(0, 0, 1000, 1000) : Rect2.FromMinMax(minX, minY, maxX, maxY);
    }
}
