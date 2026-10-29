using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Vastu;

namespace BuildingStudio.Engine.Exterior;

/// <summary>
/// Exterior package: applies a style preset and places site items (compound wall, gate, driveway, sump, septic,
/// overhead tank, rainwater pit, trees) in the setback zones, preferring Vastu zones from the rule pack.
/// </summary>
public sealed class ExteriorEngine(ExteriorStylePack styles, VastuRulePack vastu)
{
    public const double WallThickness = 230;

    public ExteriorSpec Generate(BuildingModel model, ExteriorStyle? style = null)
    {
        var chosen = style ?? model.Brief.Style;
        var def = styles.Styles.TryGetValue(chosen, out var d) ? d : new ExteriorStyleDef();
        var spec = new ExteriorSpec
        {
            Style = chosen, WallColor = def.WallColor, AccentColor = def.AccentColor, TrimColor = def.TrimColor,
            GlassColor = def.GlassColor, Cladding = def.Cladding, ParapetHeight = def.ParapetHeight, ChajjaDepth = def.ChajjaDepth,
            Roof = def.Roof, CorniceBand = def.CorniceBand, MaterialNotes = [.. def.Materials],
        };
        if (IsCoastal(model)) spec.MaterialNotes.Add("Coastal humidity (Puducherry/TN coast): use elastomeric exterior paint, SS 304 fixings and anti-fungal primer.");
        if (model.Storeys.Count > 0) PlaceSiteItems(model, spec);
        model.Exterior = spec;
        return spec;
    }

    private static bool IsCoastal(BuildingModel model) => model.Site.BasicWindSpeed >= 50;

    private void PlaceSiteItems(BuildingModel model, ExteriorSpec spec)
    {
        var site = model.Site;
        var plot = site.Plot;
        var building = model.Storeys[0].Envelope;
        var front = ModelGeometry.FrontSide(site);
        var grid = new ZoneGrid(plot, site.NorthAngle);
        var items = spec.SiteItems;

        // Compound wall strips with a gate gap on the road side, aligned with parking (or the entrance).
        var parking = model.Storeys[0].Rooms.FirstOrDefault(r => r.Type == RoomType.Parking);
        var target = parking?.Bounds.Center ?? building.Center;
        var gateCentre = front is RectSide.Left or RectSide.Right ? target.Y : target.X;
        var frontAlong = front is RectSide.Left or RectSide.Right ? plot.Height : plot.Width;
        var gate0 = Math.Clamp(gateCentre - spec.GateWidth / 2, 300, frontAlong - spec.GateWidth - 300);
        foreach (var (side, edge) in plot.Edges())
        {
            var strips = side == front
                ? new[] { (0.0, gate0), (gate0 + spec.GateWidth, frontAlong) }
                : [(0.0, edge.Length)];
            foreach (var (a, b) in strips)
            {
                if (b - a < 100) continue;
                items.Add(new SiteItem { Kind = "CompoundWall", Name = "Compound wall", Bounds = Strip(plot, side, a, b), Height = spec.CompoundWallHeight, Color = spec.WallColor });
            }
        }
        var gate = Strip(plot, front, gate0, gate0 + spec.GateWidth);
        items.Add(new SiteItem { Kind = "Gate", Name = "Gate", Bounds = gate, Height = spec.CompoundWallHeight, Color = spec.TrimColor });

        var drive = front switch
        {
            RectSide.Right => Rect2.FromMinMax(building.MaxX, gate.MinY, plot.MaxX - WallThickness, gate.MaxY),
            RectSide.Left => Rect2.FromMinMax(plot.MinX + WallThickness, gate.MinY, building.MinX, gate.MaxY),
            RectSide.Top => Rect2.FromMinMax(gate.MinX, building.MaxY, gate.MaxX, plot.MaxY - WallThickness),
            _ => Rect2.FromMinMax(gate.MinX, plot.MinY + WallThickness, gate.MaxX, building.MinY),
        };
        if (drive.Width > 300 && drive.Height > 300)
            items.Add(new SiteItem { Kind = "Driveway", Name = "Driveway", Bounds = drive, Height = 50, Color = "#b3b6b7" });

        // The sump may sit under the driveway (common on small plots); other items keep clear of it.
        var taken = items.Where(i => i.Kind is "Gate").Select(i => i.Bounds).ToList();
        Place(model, spec, grid, building, taken, "Sump", "Sump (UG tank)", 1000, 2000, 100, "#5dade2");
        taken.AddRange(items.Where(i => i.Kind is "Driveway").Select(i => i.Bounds));
        Place(model, spec, grid, building, taken, "Septic", "Septic tank", 1000, 2000, 100, "#7f8c8d");
        Place(model, spec, grid, building, taken, "RWH", "Rainwater pit", 900, 900, 100, "#48c9b0");
        for (var i = 0; i < 2; i++)
            Place(model, spec, grid, building, taken, "Tree", "Tree", 900, 900, 4500, "#27ae60");

        // Overhead tank on the roof, south-west corner of the building (heavy mass in the SW).
        var sw = CornerOfBuilding(building, site.NorthAngle, Direction.SW);
        items.Add(new SiteItem
        {
            Kind = "OverheadTank", Name = "Overhead tank", Height = 1200, Color = "#2e4053",
            Bounds = new Rect2(sw.X - 750, sw.Y - 750, 1500, 1500), Note = "On roof (SW)",
        });
        foreach (var s in items.Where(i => i.Note.Length == 0 && vastu.SiteItems.ContainsKey(i.Kind)))
            s.Note = $"In {grid.ZoneOf(s.Bounds)} ({VastuEngine.Describe(VastuEngine.Classify(vastu.SiteItems[s.Kind], grid.ZoneOf(s.Bounds)))})";
    }

    private static Vec2 CornerOfBuilding(Rect2 b, double northAngle, Direction corner)
    {
        var grid = new ZoneGrid(b, northAngle);
        foreach (var c in b.Corners())
        {
            var p = b.Center + (c - b.Center) * 0.85;
            if (grid.ZoneOf(p) == corner) return p;
        }
        return b.Center;
    }

    private static Rect2 Strip(Rect2 plot, RectSide side, double a, double b) => side switch
    {
        RectSide.Bottom => Rect2.FromMinMax(plot.MinX + a, plot.MinY, plot.MinX + b, plot.MinY + WallThickness),
        RectSide.Top => Rect2.FromMinMax(plot.MinX + a, plot.MaxY - WallThickness, plot.MinX + b, plot.MaxY),
        RectSide.Left => Rect2.FromMinMax(plot.MinX, plot.MinY + a, plot.MinX + WallThickness, plot.MinY + b),
        _ => Rect2.FromMinMax(plot.MaxX - WallThickness, plot.MinY + a, plot.MaxX, plot.MinY + b),
    };

    /// <summary>Places an item in the setback area, trying positions in order of Vastu preference.</summary>
    private void Place(BuildingModel model, ExteriorSpec spec, ZoneGrid grid, Rect2 building, List<Rect2> taken,
        string kind, string name, double w, double d, double height, string color)
    {
        var plot = model.Site.Plot;
        var inner = plot.Inflate(-WallThickness - 50);
        var rule = vastu.SiteItems.GetValueOrDefault(kind);
        var best = (Rect: default(Rect2), Cost: double.MaxValue);
        foreach (var (ww, dd) in new[] { (w, d), (d, w) })
            for (var x = inner.MinX; x <= inner.MaxX - ww; x += 150)
                for (var y = inner.MinY; y <= inner.MaxY - dd; y += 150)
                {
                    var r = new Rect2(x, y, ww, dd);
                    if (r.Overlaps(building.Inflate(150)) || taken.Any(t => t.Overlaps(r.Inflate(150)))) continue;
                    var zone = grid.ZoneOf(r);
                    var fit = rule is null ? VastuFit.Neutral : VastuEngine.Classify(rule, zone);
                    var cost = fit switch { VastuFit.Preferred => 0, VastuFit.Acceptable => 10, VastuFit.Neutral => 20, _ => 40 }
                               + r.Center.DistanceTo(plot.Center) / 1e5;
                    if (cost < best.Cost) best = (r, cost);
                }
        if (best.Cost == double.MaxValue)
        {
            spec.MaterialNotes.Add($"{name}: no room in the setbacks; place below the driveway or in a basement.");
            return;
        }
        taken.Add(best.Rect);
        spec.SiteItems.Add(new SiteItem { Kind = kind, Name = name, Bounds = best.Rect, Height = height, Color = color });
    }
}
