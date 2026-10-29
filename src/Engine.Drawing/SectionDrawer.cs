using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Drawing;

/// <summary>Section A-A cut along plan X through the middle of the envelope, looking towards plan +Y.</summary>
public static class SectionDrawer
{
    public static DrawingSheet Draw(BuildingModel model)
    {
        var sheet = new DrawingSheet { Name = "A-301", Title = "SECTION A-A", Scale = 100 };
        var e = sheet.Entities;
        if (model.Storeys.Count == 0) return sheet;
        var env = model.Storeys[0].Envelope;
        var cut = env.Center.Y;
        var roof = model.RoofLevel();

        e.Add(new LineEntity(Layers.Section, new Vec2(env.MinX - 2000, -ElevationDrawer.Plinth), new Vec2(env.MaxX + 2000, -ElevationDrawer.Plinth), 0.5));
        e.Add(PolygonEntity.Rect(Layers.Section, Rect2.FromMinMax(env.MinX - 115, -150, env.MaxX + 115, 0), "#7f8c8d", 0.35));

        foreach (var f in model.Structure.Footings.Where(f => Math.Abs(f.Position.Y - cut) < 3000))
            e.Add(PolygonEntity.Rect(Layers.Footing, Rect2.FromMinMax(f.Position.X - f.L / 2, -1500 - f.Depth, f.Position.X + f.L / 2, -1500), "#d7bde2", 0.25, true));

        foreach (var s in model.Storeys)
        {
            var top = s.Elevation + s.Height;
            e.Add(PolygonEntity.Rect(Layers.Section, Rect2.FromMinMax(env.MinX - 115, top - s.SlabThickness, env.MaxX + 115, top), "#7f8c8d", 0.35));
            foreach (var w in s.Walls.Where(w => w.Segment.IsVertical && Math.Min(w.Start.Y, w.End.Y) < cut && Math.Max(w.Start.Y, w.End.Y) > cut))
            {
                var x = w.Start.X;
                var r = Rect2.FromMinMax(x - w.Thickness / 2, s.Elevation, x + w.Thickness / 2, top - s.SlabThickness);
                var opening = s.Openings.FirstOrDefault(o => o.WallId == w.Id && CutsOpening(w, o, cut));
                if (opening is null)
                    e.Add(PolygonEntity.Rect(Layers.Section, r, "#566573", 0.35));
                else
                {
                    e.Add(PolygonEntity.Rect(Layers.Section, Rect2.FromMinMax(r.MinX, s.Elevation, r.MaxX, s.Elevation + opening.Sill), "#566573", 0.35));
                    e.Add(PolygonEntity.Rect(Layers.Section, Rect2.FromMinMax(r.MinX, s.Elevation + opening.Sill + opening.Height, r.MaxX, r.MaxY), "#566573", 0.35));
                }
            }
            foreach (var room in s.Rooms.Where(r => r.Bounds.MinY < cut && r.Bounds.MaxY > cut))
            {
                e.Add(new TextEntity(Layers.Text, new Vec2(room.Bounds.Center.X, s.Elevation + 1200), room.Name.ToUpperInvariant(), 150));
                if (room.Type == RoomType.Staircase)
                {
                    var b = room.Bounds;
                    e.Add(new LineEntity(Layers.Section, new Vec2(b.MinX + 200, s.Elevation), new Vec2(b.MaxX - 200, s.Elevation + s.Height / 2), 0.35));
                    e.Add(new LineEntity(Layers.Section, new Vec2(b.MaxX - 200, s.Elevation + s.Height / 2), new Vec2(b.MinX + 200, top), 0.25, true));
                }
            }
            e.Add(new TextEntity(Layers.Text, new Vec2(env.MaxX + 2600, s.Elevation + 80), $"+{s.Elevation / 1000:0.000}", 170, TextAlign.Left));
        }
        e.Add(PolygonEntity.Rect(Layers.Section, Rect2.FromMinMax(env.MinX - 115, roof, env.MinX + 115, roof + model.Exterior.ParapetHeight), "#566573", 0.35));
        e.Add(PolygonEntity.Rect(Layers.Section, Rect2.FromMinMax(env.MaxX - 115, roof, env.MaxX + 115, roof + model.Exterior.ParapetHeight), "#566573", 0.35));
        e.Add(new TextEntity(Layers.Text, new Vec2(env.MaxX + 2600, roof + 80), $"+{roof / 1000:0.000} ROOF", 170, TextAlign.Left));
        e.Add(new TextEntity(Layers.Title, new Vec2(env.Center.X, -ElevationDrawer.Plinth - 2600), $"SECTION A-A  ·  1:{sheet.Scale}", 300, TextAlign.Center, true));
        return sheet;
    }

    private static bool CutsOpening(Wall w, Opening o, double cut)
    {
        var a = w.Segment.PointAtDistance(o.Offset).Y;
        var b = w.Segment.PointAtDistance(o.Offset + o.Width).Y;
        return Math.Min(a, b) < cut && Math.Max(a, b) > cut;
    }
}
