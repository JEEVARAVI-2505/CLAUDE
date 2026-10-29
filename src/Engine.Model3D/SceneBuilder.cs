using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Model3D;

/// <param name="TopLevel">Show storeys up to this level only, without the slab over it (cut-away floor view).</param>
public sealed record SceneOptions(bool Furniture = true, bool Structure = false, bool Site = true, int? TopLevel = null);

/// <summary>Builds the 3D scene from the single <see cref="BuildingModel"/>. Re-run after every model change.</summary>
public static class SceneBuilder
{
    public const string Wood = "#7b4a2a";
    public const string Concrete = "#bfc3c7";
    public const double Plinth = 450;

    public static Scene3D Build(BuildingModel model, SceneOptions? options = null)
    {
        var o = options ?? new SceneOptions();
        var scene = new Scene3D();
        if (model.Storeys.Count == 0) return scene;
        var ext = model.Exterior;
        var env = model.Storeys[0].Envelope;
        var top = o.TopLevel ?? model.Storeys.Max(s => s.Level);
        var cut = o.TopLevel is not null;

        scene.Part("Slabs", ext.AccentColor).AddBox(env.Inflate(115), -Plinth, -150);
        scene.Part("Slabs", Concrete).AddBox(env.Inflate(115), -150, 0);

        foreach (var s in model.Storeys.Where(s => s.Level <= top))
        {
            foreach (var r in s.Rooms) scene.Part("Floors", FloorColor(r)).AddBox(r.Bounds, s.Elevation, s.Elevation + 15);
            foreach (var w in s.Walls) Wall(scene, s, w, ext);
            var isTop = s.Level == top;
            if (!(isTop && cut)) Slab(scene, model, s);
            foreach (var r in s.Rooms.Where(r => r.Type == RoomType.Staircase)) Stairs.Build(scene, r.Bounds, s.Elevation, s.Height);
            if (o.Furniture)
                foreach (var f in s.Furniture)
                    scene.Part("Furniture", f.Color).AddBox(f.Bounds, s.Elevation + 15, s.Elevation + 15 + Math.Max(40, f.Height));
        }

        if (!cut) Roof.Build(scene, model);
        if (o.Structure) StructureMeshes(scene, model);
        if (o.Site) SiteMeshes(scene, model, cut);
        return scene;
    }

    private static string FloorColor(Room r) => r.Type switch
    {
        RoomType.Bath or RoomType.Utility or RoomType.Kitchen => "#d9dde0",
        RoomType.MasterBedroom or RoomType.Study => "#c49a6c",
        RoomType.Parking => "#9ea3a8",
        RoomType.Staircase => "#5d5d5d",
        RoomType.Balcony => "#c8b8a6",
        _ => "#eee6d8",
    };

    private static void Wall(Scene3D scene, Storey s, Wall w, ExteriorSpec ext)
    {
        var len = w.Length;
        if (len < 1) return;
        var dir = (w.End - w.Start) * (1 / len);
        var n = new Vec2(-dir.Y, dir.X);
        var extend = w.Type == WallType.Exterior ? w.Thickness / 2 : 0;
        var origin = w.Start - n * (w.Thickness / 2);
        var z0 = s.Elevation;
        var h = s.Height - s.SlabThickness;
        var color = w.Type == WallType.Exterior ? ext.WallColor : "#f4f1ea";
        var part = scene.Part(w.Type == WallType.Exterior ? "Walls" : "Partitions", color);
        var openings = s.Openings.Where(o => o.WallId == w.Id).OrderBy(o => o.Offset).ToList();

        void Piece(double a, double b, double lo, double hi)
        {
            if (a <= 0) a -= extend;
            if (b >= len) b += extend;
            part.AddOrientedBox(origin + dir * a, dir, b - a, w.Thickness, z0 + lo, z0 + hi);
        }

        double pos = 0;
        foreach (var o in openings)
        {
            Piece(pos, o.Offset, 0, h);
            var top = Math.Min(h, o.Sill + o.Height);
            if (o.Sill > 0) part.AddOrientedBox(origin + dir * o.Offset, dir, o.Width, w.Thickness, z0, z0 + o.Sill);
            if (top < h) part.AddOrientedBox(origin + dir * o.Offset, dir, o.Width, w.Thickness, z0 + top, z0 + h);
            var mid = origin + dir * o.Offset + n * (w.Thickness / 2 - 15);
            switch (o.Kind)
            {
                case OpeningKind.Window or OpeningKind.Ventilator:
                    scene.Part("Glass", ext.GlassColor, 0.45).AddOrientedBox(mid, dir, o.Width, 30, z0 + o.Sill, z0 + top);
                    scene.Part("Frames", ext.TrimColor).AddOrientedBox(origin + dir * o.Offset, dir, o.Width, w.Thickness, z0 + o.Sill - 40, z0 + o.Sill);
                    if (w.Type == WallType.Exterior && o.Kind == OpeningKind.Window && ext.ChajjaDepth > 0)
                    {
                        var outward = ModelGeometry.OutwardNormal(s, w);
                        var face = w.Start + outward * (w.Thickness / 2) + dir * (o.Offset - 150);
                        var sideN = new Vec2(-dir.Y, dir.X);
                        var depthDir = sideN.Dot(outward) > 0 ? 1 : -1;
                        var start = depthDir > 0 ? face : face + outward * ext.ChajjaDepth;
                        scene.Part("Chajja", ext.TrimColor).AddOrientedBox(start, dir, o.Width + 300, ext.ChajjaDepth, z0 + top + 100, z0 + top + 175);
                    }
                    break;
                case OpeningKind.Door:
                    scene.Part("Doors", o.IsMainEntrance ? ext.AccentColor : Wood).AddOrientedBox(mid, dir, o.Width, 40, z0, z0 + top);
                    break;
            }
            pos = o.Offset + o.Width;
        }
        Piece(pos, len, 0, h);
    }

    private static void Slab(Scene3D scene, BuildingModel model, Storey s)
    {
        var env = s.Envelope.Inflate(115);
        var z1 = s.Elevation + s.Height;
        var z0 = z1 - s.SlabThickness;
        var isRoof = s.Level == model.Storeys.Max(x => x.Level);
        var hole = isRoof ? (Rect2?)null : s.Rooms.FirstOrDefault(r => r.Type == RoomType.Staircase)?.Bounds;
        var part = scene.Part("Slabs", Concrete);
        if (hole is not { } h)
        {
            part.AddBox(env, z0, z1);
            return;
        }
        part.AddBox(Rect2.FromMinMax(env.MinX, env.MinY, env.MaxX, h.MinY), z0, z1);
        part.AddBox(Rect2.FromMinMax(env.MinX, h.MaxY, env.MaxX, env.MaxY), z0, z1);
        part.AddBox(Rect2.FromMinMax(env.MinX, h.MinY, h.MinX, h.MaxY), z0, z1);
        part.AddBox(Rect2.FromMinMax(h.MaxX, h.MinY, env.MaxX, h.MaxY), z0, z1);
    }

    private static void StructureMeshes(Scene3D scene, BuildingModel model)
    {
        var st = model.Structure;
        var roof = model.RoofLevel();
        foreach (var c in st.Columns)
            scene.Part("Structure", "#c0392b").AddBox(new Rect2(c.Position.X - c.B / 2, c.Position.Y - c.D / 2, c.B, c.D), -1500, roof);
        foreach (var f in st.Footings)
            scene.Part("Structure", "#8e44ad").AddBox(new Rect2(f.Position.X - f.L / 2, f.Position.Y - f.B / 2, f.L, f.B), -1500 - f.Depth, -1500);
        foreach (var b in st.Beams)
        {
            var z = b.Level == 0 ? 0 : model.GetStorey(b.Level - 1) is { } s ? s.Elevation + s.Height : roof;
            var len = b.Start.DistanceTo(b.End);
            if (len < 1) continue;
            var dir = (b.End - b.Start) * (1 / len);
            var n = new Vec2(-dir.Y, dir.X);
            scene.Part("Structure", "#d35400").AddOrientedBox(b.Start - n * (b.B / 2), dir, len, b.B, z - b.D, z);
        }
    }

    private static void SiteMeshes(Scene3D scene, BuildingModel model, bool cut)
    {
        var plot = model.Site.Plot;
        scene.Part("Site", "#7dbb6a").AddBox(plot.Inflate(8000), -Plinth - 60, -Plinth - 10);
        scene.Part("Site", "#a9c99a").AddBox(plot, -Plinth - 10, -Plinth);
        foreach (var i in model.Exterior.SiteItems)
        {
            var z0 = -Plinth;
            switch (i.Kind)
            {
                case "Tree":
                    var c = i.Bounds.Center;
                    scene.Part("Site", "#6e4b2a").AddBox(new Rect2(c.X - 120, c.Y - 120, 240, 240), z0, z0 + 2200);
                    scene.Part("Site", i.Color).AddBox(i.Bounds.Inflate(600), z0 + 2200, z0 + i.Height);
                    break;
                case "OverheadTank":
                    if (!cut) scene.Part("Site", i.Color).AddBox(i.Bounds, model.RoofLevel(), model.RoofLevel() + i.Height);
                    break;
                default:
                    scene.Part("Site", i.Color).AddBox(i.Bounds, z0, z0 + i.Height);
                    break;
            }
        }
    }
}
