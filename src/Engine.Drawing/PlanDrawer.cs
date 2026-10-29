using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Drawing;

public sealed record PlanDrawOptions(bool Furniture = true, bool Columns = true, bool Dimensions = true, bool RoomFill = true);

/// <summary>Floor plan sheet: walls with openings cut, door swings, windows, room tags, dimension chains, north arrow.</summary>
public static class PlanDrawer
{
    public static DrawingSheet Draw(BuildingModel model, Storey storey, PlanDrawOptions? options = null)
    {
        var o = options ?? new PlanDrawOptions();
        var sheet = new DrawingSheet { Name = $"A-10{storey.Level + 1}", Title = $"{storey.Name.ToUpperInvariant()} PLAN", Scale = 100 };
        var e = sheet.Entities;

        if (o.RoomFill)
            foreach (var r in storey.Rooms)
                e.Add(PolygonEntity.Rect(Layers.Room, r.Bounds, RoomColors.For(r.Type), 0.05));

        if (o.Furniture)
            foreach (var f in storey.Furniture)
            {
                e.Add(PolygonEntity.Rect(Layers.Furniture, f.Bounds, null, 0.13));
                if (f.Bounds.ShortSide > 500)
                    e.Add(new TextEntity(Layers.Furniture, f.Bounds.Center, f.Name, Math.Min(110, f.Bounds.ShortSide / 5)));
            }

        foreach (var w in storey.Walls) DrawWall(storey, w, e);
        foreach (var r in storey.Rooms) Tag(r, e);
        if (o.Columns)
            foreach (var c in model.Structure.Columns)
            {
                e.Add(PolygonEntity.Rect(Layers.Column, new Rect2(c.Position.X - c.B / 2, c.Position.Y - c.D / 2, c.B, c.D), "#c0392b", 0.1));
            }
        if (o.Dimensions) Dimensions(storey, e);
        NorthArrow(storey.Envelope, model.Site.NorthAngle, e);
        e.Add(new TextEntity(Layers.Title, new Vec2(storey.Envelope.Center.X, storey.Envelope.MinY - 2300),
            $"{storey.Name.ToUpperInvariant()} PLAN  ·  1:{sheet.Scale}  ·  FFL +{storey.Elevation / 1000:0.000}", 300, TextAlign.Center, true));
        return sheet;
    }

    private static void DrawWall(Storey storey, Wall w, List<DrawEntity> e)
    {
        var len = w.Length;
        if (len < 1) return;
        var dir = (w.End - w.Start) * (1 / len);
        var n = new Vec2(-dir.Y, dir.X) * (w.Thickness / 2);
        var openings = storey.Openings.Where(o => o.WallId == w.Id).OrderBy(o => o.Offset).ToList();

        double pos = 0;
        // Exterior walls run past their ends by half a thickness so corners close.
        var ext = w.Type == WallType.Exterior ? w.Thickness / 2 : 0;
        void Solid(double a, double b)
        {
            if (a <= 0) a -= ext;
            if (b >= len) b += ext;
            if (b - a < 1) return;
            var p0 = w.Start + dir * a;
            var p1 = w.Start + dir * b;
            e.Add(new PolygonEntity(Layers.Wall, [p0 + n, p1 + n, p1 - n, p0 - n], "#2c2c2c", 0.35));
        }
        foreach (var o in openings)
        {
            Solid(pos, o.Offset);
            DrawOpening(storey, w, o, dir, n, e);
            pos = o.Offset + o.Width;
        }
        Solid(pos, len);
    }

    private static void DrawOpening(Storey storey, Wall w, Opening o, Vec2 dir, Vec2 n, List<DrawEntity> e)
    {
        var a = w.Start + dir * o.Offset;
        var b = w.Start + dir * (o.Offset + o.Width);
        switch (o.Kind)
        {
            case OpeningKind.Window:
            case OpeningKind.Ventilator:
                e.Add(new LineEntity(Layers.Glazing, a + n, b + n, 0.18));
                e.Add(new LineEntity(Layers.Glazing, a - n, b - n, 0.18));
                e.Add(new LineEntity(Layers.Glazing, a, b, 0.25));
                e.Add(new LineEntity(Layers.Glazing, a + n, a - n, 0.18));
                e.Add(new LineEntity(Layers.Glazing, b + n, b - n, 0.18));
                break;
            case OpeningKind.Archway:
                e.Add(new LineEntity(Layers.Door, a + n, b + n, 0.13, true));
                e.Add(new LineEntity(Layers.Door, a - n, b - n, 0.13, true));
                break;
            default:
                // Leaf swings into the room that is not the passage (into the bath for bath doors).
                var into = SwingRoom(storey, w);
                var side = into is null || (into.Bounds.Center - a).Dot(n) >= 0 ? 1.0 : -1.0;
                var nn = n.Normalized() * side;
                var leafEnd = a + nn * o.Width;
                e.Add(new LineEntity(Layers.Door, a, leafEnd, 0.25));
                var start = Math.Atan2(dir.Y, dir.X);
                var end = Math.Atan2(nn.Y, nn.X);
                var (s0, s1) = NormalizeArc(start, end);
                e.Add(new ArcEntity(Layers.Door, a, o.Width, s0, s1));
                if (o.IsMainEntrance)
                    e.Add(new TextEntity(Layers.Text, (a + b) * 0.5 - nn * 700, "ENTRY", 160, TextAlign.Center, true));
                break;
        }
    }

    private static Room? SwingRoom(Storey storey, Wall w)
    {
        var a = storey.FindRoom(w.RoomA ?? Guid.Empty);
        var b = storey.FindRoom(w.RoomB ?? Guid.Empty);
        if (b is null) return a;
        if (a is null) return b;
        if (a.Type == RoomType.Passage) return b;
        if (b.Type == RoomType.Passage) return a;
        if (b.Type == RoomType.Bath) return b;
        return a.Type == RoomType.Bath ? a : a.Area >= b.Area ? a : b;
    }

    /// <summary>Returns a counter-clockwise arc of at most 180° between the two angles.</summary>
    private static (double, double) NormalizeArc(double a, double b)
    {
        var d = b - a;
        while (d <= -Math.PI) d += 2 * Math.PI;
        while (d > Math.PI) d -= 2 * Math.PI;
        return d >= 0 ? (a, a + d) : (a + d, a);
    }

    private static void Tag(Room r, List<DrawEntity> e)
    {
        var c = r.Bounds.Center;
        var h = Math.Clamp(r.Bounds.ShortSide / 9, 90, 200);
        var vertical = r.Bounds.Height > r.Bounds.Width * 1.6 && r.Bounds.Width < 1800;
        e.Add(new TextEntity(Layers.Text, c + new Vec2(0, h * 0.6), r.Name.ToUpperInvariant(), h, TextAlign.Center, true));
        if (vertical || r.Type == RoomType.Passage) return;
        e.Add(new TextEntity(Layers.Text, c - new Vec2(0, h * 0.8), $"{r.Bounds.Width / 1000:0.00} x {r.Bounds.Height / 1000:0.00}", h * 0.8));
        e.Add(new TextEntity(Layers.Text, c - new Vec2(0, h * 2.0), $"{r.Area / 1e6:0.0} m² / {Units.SqMmToSqFt(r.Area):0} sft", h * 0.7));
    }

    private static void Dimensions(Storey s, List<DrawEntity> e)
    {
        var env = s.Envelope;
        var xs = s.Rooms.Where(r => Tolerance.Near(r.Bounds.MinY, env.MinY)).SelectMany(r => new[] { r.Bounds.MinX, r.Bounds.MaxX })
            .Distinct().OrderBy(x => x).ToList();
        for (var i = 0; i + 1 < xs.Count; i++)
            if (xs[i + 1] - xs[i] > 10)
                e.Add(new DimensionEntity(Layers.Dims, new Vec2(xs[i + 1], env.MinY), new Vec2(xs[i], env.MinY), 700, null, 150));
        e.Add(new DimensionEntity(Layers.Dims, new Vec2(env.MaxX, env.MinY), new Vec2(env.MinX, env.MinY), 1300, $"{env.Width:0}", 170));

        var ys = s.Rooms.Where(r => Tolerance.Near(r.Bounds.MinX, env.MinX)).SelectMany(r => new[] { r.Bounds.MinY, r.Bounds.MaxY })
            .Distinct().OrderBy(y => y).ToList();
        for (var i = 0; i + 1 < ys.Count; i++)
            if (ys[i + 1] - ys[i] > 10)
                e.Add(new DimensionEntity(Layers.Dims, new Vec2(env.MinX, ys[i]), new Vec2(env.MinX, ys[i + 1]), 700, null, 150));
        e.Add(new DimensionEntity(Layers.Dims, new Vec2(env.MinX, env.MinY), new Vec2(env.MinX, env.MaxY), 1300, $"{env.Height:0}", 170));
    }

    public static void NorthArrow(Rect2 env, double northAngle, List<DrawEntity> e)
    {
        var c = new Vec2(env.MaxX + 1600, env.MaxY - 800);
        var up = new Vec2(0, 1).Rotate(northAngle);
        var right = new Vec2(up.Y, -up.X);
        e.Add(new CircleEntity(Layers.Title, c, 600));
        e.Add(new PolygonEntity(Layers.Title, [c + up * 550, c - up * 400 + right * 250, c - up * 200, c - up * 400 - right * 250], "#1b1b1b", 0.1));
        e.Add(new TextEntity(Layers.Title, c + up * 900, "N", 260, TextAlign.Center, true));
    }
}
