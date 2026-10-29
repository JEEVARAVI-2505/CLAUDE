using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Drawing;

/// <summary>Site plan, door/window schedule and area statement sheets.</summary>
public static class SiteAndScheduleDrawer
{
    public static DrawingSheet SitePlan(BuildingModel model)
    {
        var sheet = new DrawingSheet { Name = "A-001", Title = "SITE PLAN", Scale = 200 };
        var e = sheet.Entities;
        var site = model.Site;
        var plot = site.Plot;
        e.Add(PolygonEntity.Rect(Layers.Site, plot, "#e9f7ef", 0.5));
        e.Add(PolygonEntity.Rect(Layers.Site, site.Envelope(), null, 0.18, true));
        if (model.Storeys.Count > 0)
            e.Add(PolygonEntity.Rect(Layers.Wall, model.Storeys[0].Envelope, "#d5d8dc", 0.5));
        foreach (var item in model.Exterior.SiteItems)
        {
            e.Add(PolygonEntity.Rect(Layers.Site, item.Bounds, item.Color, 0.18));
            e.Add(new TextEntity(Layers.Text, item.Bounds.Center, item.Name, 160));
        }
        var front = ModelGeometry.FrontSide(site);
        var road = front switch
        {
            RectSide.Right => Rect2.FromMinMax(plot.MaxX, plot.MinY - 1000, plot.MaxX + 6000, plot.MaxY + 1000),
            RectSide.Left => Rect2.FromMinMax(plot.MinX - 6000, plot.MinY - 1000, plot.MinX, plot.MaxY + 1000),
            RectSide.Top => Rect2.FromMinMax(plot.MinX - 1000, plot.MaxY, plot.MaxX + 1000, plot.MaxY + 6000),
            _ => Rect2.FromMinMax(plot.MinX - 1000, plot.MinY - 6000, plot.MaxX + 1000, plot.MinY),
        };
        e.Add(PolygonEntity.Rect(Layers.Site, road, "#d5d8dc", 0.18));
        e.Add(new TextEntity(Layers.Text, road.Center, "ROAD", 400, TextAlign.Center, true));
        e.Add(new DimensionEntity(Layers.Dims, new Vec2(plot.MaxX, plot.MinY), new Vec2(plot.MinX, plot.MinY), 900, $"{Units.MmToFt(plot.Width):0.#} ft ({plot.Width / 1000:0.00} m)", 250));
        e.Add(new DimensionEntity(Layers.Dims, new Vec2(plot.MinX, plot.MinY), new Vec2(plot.MinX, plot.MaxY), 900, $"{Units.MmToFt(plot.Height):0.#} ft ({plot.Height / 1000:0.00} m)", 250));
        PlanDrawer.NorthArrow(plot, site.NorthAngle, e);
        e.Add(new TextEntity(Layers.Title, new Vec2(plot.Center.X, plot.MinY - 2800),
            $"SITE PLAN  ·  1:{sheet.Scale}  ·  {site.Facing}-facing  ·  setbacks F {site.Setbacks.Front / 1000:0.00} / R {site.Setbacks.Rear / 1000:0.00} / S {site.Setbacks.Side / 1000:0.00} m", 300, TextAlign.Center, true));
        return sheet;
    }

    /// <summary>Opening marks: doors D1.., windows W1.., ventilators V1.. grouped by size.</summary>
    public static IReadOnlyList<(string Mark, OpeningKind Kind, double Width, double Height, double Sill, int Count, string Where)> OpeningSchedule(BuildingModel model)
    {
        var rows = model.Storeys.SelectMany(s => s.Openings.Select(o => (s, o)))
            .Where(x => x.o.Kind != OpeningKind.Archway)
            .GroupBy(x => (x.o.Kind, x.o.Width, x.o.Height, x.o.Sill, x.o.IsMainEntrance))
            .OrderBy(g => g.Key.Kind).ThenByDescending(g => g.Key.IsMainEntrance).ThenByDescending(g => g.Key.Width)
            .ToList();
        var counters = new Dictionary<OpeningKind, int>();
        return rows.Select(g =>
        {
            counters[g.Key.Kind] = counters.GetValueOrDefault(g.Key.Kind) + 1;
            var prefix = g.Key.Kind switch { OpeningKind.Door => "D", OpeningKind.Window => "W", _ => "V" };
            var where = string.Join(", ", g.Select(x => x.s.FindWall(x.o.WallId)?.RoomA is { } id ? x.s.FindRoom(id)?.Name : null)
                .Where(n => n is not null).Distinct().Take(4));
            if (g.Key.IsMainEntrance) where = "Main entrance";
            return ($"{prefix}{counters[g.Key.Kind]}", g.Key.Kind, g.Key.Width, g.Key.Height, g.Key.Sill, g.Count(), where);
        }).ToList();
    }

    public static DrawingSheet Schedules(BuildingModel model)
    {
        var sheet = new DrawingSheet { Name = "A-601", Title = "SCHEDULES & AREA STATEMENT", Scale = 100 };
        var rows = OpeningSchedule(model).Select(r => (IReadOnlyList<string>)
            [r.Mark, r.Kind.ToString(), $"{r.Width:0} x {r.Height:0}", $"{r.Sill:0}", r.Count.ToString(), r.Where]).ToList();
        var t1 = new TableEntity(Layers.Text, new Vec2(0, 0), ["Mark", "Type", "W x H (mm)", "Sill", "Nos", "Location"], rows, [1200, 2000, 2400, 1000, 900, 6000], 200);
        sheet.Entities.Add(t1);

        var plotSqm = Units.SqMmToSqM(model.Site.PlotArea);
        var areaRows = model.Storeys.Select(s => (IReadOnlyList<string>)
            [s.Name, $"{Units.SqMmToSqM(s.Rooms.Sum(r => r.Area)):0.00}", $"{Units.SqMmToSqFt(s.Rooms.Sum(r => r.Area)):0}"]).ToList();
        var total = model.BuiltUpArea();
        areaRows.Add(["Total built-up", $"{Units.SqMmToSqM(total):0.00}", $"{Units.SqMmToSqFt(total):0}"]);
        areaRows.Add(["Plot area", $"{plotSqm:0.00}", $"{Units.SqMmToSqFt(model.Site.PlotArea):0}"]);
        areaRows.Add(["FAR (built-up / plot)", $"{Units.SqMmToSqM(total) / plotSqm:0.00}", ""]);
        sheet.Entities.Add(new TableEntity(Layers.Text, new Vec2(0, -t1.Height - 1500), ["Area statement", "m²", "sq ft"], areaRows, [5000, 2000, 2000], 200));
        sheet.Entities.Add(new TextEntity(Layers.Title, new Vec2(0, 900), "DOOR / WINDOW SCHEDULE", 320, TextAlign.Left, true));
        return sheet;
    }
}
