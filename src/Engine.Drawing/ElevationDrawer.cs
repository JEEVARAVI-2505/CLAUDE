using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Drawing;

/// <summary>
/// Elevation of one face, projected from the model: wall face, openings with chajjas, floor bands,
/// plinth, parapet or sloped roof. X = distance along the face (viewer's left to right), Y = height.
/// </summary>
public static class ElevationDrawer
{
    public const double Plinth = 450;

    public static DrawingSheet Draw(BuildingModel model, CardinalSide side)
    {
        var sheet = new DrawingSheet { Name = $"A-20{(int)side + 1}", Title = $"{Name(side)} ELEVATION", Scale = 100 };
        var e = sheet.Entities;
        if (model.Storeys.Count == 0) return sheet;
        var ext = model.Exterior;
        var theta = model.Site.NorthAngle;
        var outward = ModelGeometry.PlanDirectionOf(side, theta);
        var right = (outward * -1).Rotate(-Math.PI / 2);
        var env = model.Storeys[0].Envelope;
        var us = env.Corners().Select(c => c.Dot(right)).ToList();
        var u0 = us.Min();
        var width = us.Max() - u0;
        double U(Vec2 p) => p.Dot(right) - u0;
        var roof = model.RoofLevel();

        e.Add(new LineEntity(Layers.Elevation, new Vec2(-2000, -Plinth), new Vec2(width + 2000, -Plinth), 0.5));
        e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(-115, -Plinth, width + 115, 0), ext.AccentColor, 0.25));

        foreach (var s in model.Storeys)
        {
            e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(-115, s.Elevation, width + 115, s.Elevation + s.Height), ext.WallColor, 0.35));
            e.Add(new LineEntity(Layers.Elevation, new Vec2(-115, s.Elevation + s.Height - s.SlabThickness), new Vec2(width + 115, s.Elevation + s.Height - s.SlabThickness), 0.13));
            if (ext.CorniceBand)
                e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(-200, s.Elevation + s.Height - 150, width + 200, s.Elevation + s.Height), ext.TrimColor, 0.18));

            foreach (var w in s.Walls.Where(w => w.Type == WallType.Exterior && ModelGeometry.CompassOf(ModelGeometry.OutwardNormal(s, w), theta) == side))
            {
                foreach (var o in s.Openings.Where(o => o.WallId == w.Id))
                {
                    var a = U(w.Segment.PointAtDistance(o.Offset));
                    var b = U(w.Segment.PointAtDistance(o.Offset + o.Width));
                    var (x0, x1) = (Math.Min(a, b), Math.Max(a, b));
                    var z0 = s.Elevation + o.Sill;
                    var z1 = z0 + o.Height;
                    var fill = o.Kind switch
                    {
                        OpeningKind.Door => "#7b4a2a",
                        OpeningKind.Archway => "#3d3d3d",
                        _ => ext.GlassColor,
                    };
                    e.Add(PolygonEntity.Rect(Layers.Glazing, Rect2.FromMinMax(x0, z0, x1, z1), fill, 0.25));
                    if (o.Kind is OpeningKind.Window)
                    {
                        e.Add(new LineEntity(Layers.Glazing, new Vec2((x0 + x1) / 2, z0), new Vec2((x0 + x1) / 2, z1), 0.13));
                        e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(x0 - 150, z1 + 100, x1 + 150, z1 + 175), ext.TrimColor, 0.18));
                    }
                    if (o.IsMainEntrance)
                        e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(x0 - 600, z1 + 150, x1 + 600, z1 + 300), ext.AccentColor, 0.25));
                }
            }
            e.Add(new TextEntity(Layers.Text, new Vec2(width + 2600, s.Elevation + 80), $"+{s.Elevation / 1000:0.000} {(s.Level == 0 ? "FFL" : s.Name.ToUpperInvariant())}", 170, TextAlign.Left));
            e.Add(new LineEntity(Layers.Dims, new Vec2(width + 300, s.Elevation), new Vec2(width + 2500, s.Elevation), 0.13, true));
        }

        RoofProfile(model, side, width, roof, right, u0, e);
        e.Add(new TextEntity(Layers.Text, new Vec2(width + 2600, roof + 80), $"+{roof / 1000:0.000} ROOF", 170, TextAlign.Left));
        e.Add(new TextEntity(Layers.Title, new Vec2(width / 2, -Plinth - 1500), $"{Name(side)} ELEVATION  ·  1:{sheet.Scale}  ·  {model.Exterior.Style} style", 300, TextAlign.Center, true));
        return sheet;
    }

    private static void RoofProfile(BuildingModel model, CardinalSide side, double width, double roof, Vec2 right, double u0, List<DrawEntity> e)
    {
        var ext = model.Exterior;
        var env = model.Storeys[0].Envelope;
        if (model.Storeys.Count > 1 && ext.Roof != RoofForm.SlopedTile)
        {
            var stair = model.Storeys[^1].Rooms.FirstOrDefault(r => r.Type == RoomType.Staircase);
            if (stair is not null)
            {
                var a = stair.Bounds.Corners().Select(c => c.Dot(right) - u0).ToList();
                e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(a.Min(), roof, a.Max(), roof + 2700), ext.WallColor, 0.25));
                e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(a.Min() - 300, roof + 2700, a.Max() + 300, roof + 2850), ext.TrimColor, 0.18));
            }
        }
        switch (ext.Roof)
        {
            case RoofForm.SlopedTile:
                var ridgeAlongX = env.Width >= env.Height;
                var faceAlongX = Math.Abs(right.X) > Math.Abs(right.Y);
                var span = ridgeAlongX ? env.Height : env.Width;
                var rise = span / 2 * Math.Tan(Math.PI / 180 * 25);
                const double eave = 750;
                if (ridgeAlongX == faceAlongX)
                    e.Add(new PolygonEntity(Layers.Elevation, [new(-eave, roof), new(width + eave, roof), new(width + eave - span / 2, roof + rise), new(-eave + span / 2, roof + rise)], ext.AccentColor, 0.35));
                else
                    e.Add(new PolygonEntity(Layers.Elevation, [new(-eave, roof), new(width + eave, roof), new(width / 2, roof + rise)], ext.AccentColor, 0.35));
                break;
            default:
                e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(-115, roof, width + 115, roof + ext.ParapetHeight), ext.WallColor, 0.35));
                e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(-150, roof + ext.ParapetHeight - 100, width + 150, roof + ext.ParapetHeight), ext.TrimColor, 0.18));
                if (ext.Roof == RoofForm.Pergola)
                    for (var x = width * 0.55; x < width - 100; x += 450)
                        e.Add(PolygonEntity.Rect(Layers.Elevation, new Rect2(x, roof + 2400, 100, 200), ext.AccentColor, 0.18));
                if (ext.Roof == RoofForm.Pergola)
                    e.Add(PolygonEntity.Rect(Layers.Elevation, Rect2.FromMinMax(width * 0.55, roof + 2350, width - 100, roof + 2400), ext.AccentColor, 0.18));
                break;
        }
    }

    public static string Name(CardinalSide s) => s switch
    {
        CardinalSide.N => "NORTH",
        CardinalSide.E => "EAST",
        CardinalSide.S => "SOUTH",
        _ => "WEST",
    };
}
