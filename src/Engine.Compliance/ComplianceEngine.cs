using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;

namespace BuildingStudio.Engine.Compliance;

public enum ComplianceStatus { Pass, Warning, Fail }

/// <summary>One bylaw check with required vs actual values. RuleId cites the rule pack key.</summary>
public sealed record ComplianceItem(string RuleId, string Category, string Description, string Required, string Actual, ComplianceStatus Status, Guid? RoomId = null);

public sealed record ComplianceReport(IReadOnlyList<ComplianceItem> Items, string RulePack, string Disclaimer)
{
    public int Failures => Items.Count(i => i.Status == ComplianceStatus.Fail);
    public int Warnings => Items.Count(i => i.Status == ComplianceStatus.Warning);
    public bool AllPass => Failures == 0;
}

/// <summary>Stair geometry derived from the floor height and the stair room (dog-legged, two flights).</summary>
public sealed record StairGeometry(int Risers, double Riser, double Tread, double FlightWidth, double RequiredLength, double AvailableLength)
{
    public static StairGeometry For(Rect2 stair, double floorHeight, StairRule rule)
    {
        var risers = (int)Math.Ceiling(floorHeight / rule.MaxRiserMm - 1e-9);
        if (risers % 2 == 1) risers++;
        var riser = floorHeight / risers;
        var flightWidth = (stair.ShortSide - 100) / 2;
        var treadsPerFlight = risers / 2 - 1;
        var available = stair.LongSide - flightWidth;
        var tread = Math.Max(rule.MinTreadMm, Math.Min(300, available / Math.Max(1, treadsPerFlight)));
        return new StairGeometry(risers, riser, tread, flightWidth, treadsPerFlight * rule.MinTreadMm + flightWidth, stair.LongSide);
    }
}

/// <summary>Deterministic bylaw checks driven by a <see cref="BylawRulePack"/>.</summary>
public sealed class ComplianceEngine(BylawRulePack pack)
{
    public BylawRulePack Pack { get; } = pack;

    public ComplianceReport Evaluate(BuildingModel model)
    {
        var items = new List<ComplianceItem>();
        var site = model.Site;
        var plotSqm = Units.SqMmToSqM(site.PlotArea);

        var band = Pack.SetbacksFor(plotSqm);
        items.Add(Min("B-SETBACK-FRONT", "Setbacks", "Front setback", band.FrontM, site.Setbacks.Front / 1000, "m"));
        items.Add(Min("B-SETBACK-REAR", "Setbacks", "Rear setback", band.RearM, site.Setbacks.Rear / 1000, "m"));
        items.Add(Min("B-SETBACK-SIDE", "Setbacks", "Side setbacks", band.SideM, site.Setbacks.Side / 1000, "m"));

        var builtUp = Units.SqMmToSqM(model.BuiltUpArea());
        items.Add(Max("B-FAR", "Intensity", "Floor area ratio (FAR/FSI)", Pack.MaxFar, builtUp / plotSqm, ""));
        var ground = model.GetStorey(0);
        var footprint = ground is null ? 0 : Units.SqMmToSqM(ground.Rooms.Sum(r => r.Area));
        items.Add(Max("B-COVERAGE", "Intensity", "Ground coverage", Pack.MaxCoverage * 100, footprint / plotSqm * 100, "%"));

        var height = Units.MmToM(model.RoofLevel());
        items.Add(Max("B-HEIGHT", "Height", "Building height to roof", Pack.MaxHeightM, height, "m"));
        items.Add(Min("B-FLOOR-HEIGHT", "Height", "Floor-to-floor height", Pack.MinFloorHeightM, model.Brief.FloorHeight / 1000, "m"));

        foreach (var storey in model.Storeys) CheckStorey(model, storey, items);
        CheckParking(model, items);
        return new ComplianceReport(items, Pack.Name, Pack.Disclaimer);
    }

    private void CheckStorey(BuildingModel model, Storey storey, List<ComplianceItem> items)
    {
        foreach (var room in storey.Rooms)
        {
            var label = $"{room.Name} ({storey.Name})";
            if (Pack.MinRoomSizes.TryGetValue(room.Type, out var min))
            {
                items.Add(Min("B-MIN-AREA", "Room size", $"{label} area", min.AreaSqm, Units.SqMmToSqM(room.Area), "m²", room.Id));
                items.Add(Min("B-MIN-WIDTH", "Room size", $"{label} width", min.WidthM, room.Bounds.ShortSide / 1000, "m", room.Id));
            }
            if (room.Type == RoomType.Passage)
                items.Add(Min("B-CORRIDOR", "Circulation", $"{label} width", Pack.MinCorridorWidthM, room.Bounds.ShortSide / 1000, "m", room.Id));

            var openings = storey.Openings.Where(o => o.Kind is OpeningKind.Window or OpeningKind.Ventilator
                && storey.FindWall(o.WallId)?.RoomA == room.Id).ToList();
            if (RoomTypes.IsHabitable(room.Type))
            {
                var ratio = openings.Sum(o => o.Width * o.Height) / room.Area;
                items.Add(Min("B-VENT", "Light & ventilation", $"{label} window area / floor area", Pack.MinOpeningToFloorRatio * 100, ratio * 100, "%", room.Id));
            }
            else if (room.Type == RoomType.Bath)
            {
                var ok = openings.Count > 0;
                items.Add(new ComplianceItem("B-VENT-BATH", "Light & ventilation", $"{label} ventilator to exterior", "1 opening",
                    ok ? $"{openings.Count} opening(s)" : "none (needs duct / exhaust)", ok ? ComplianceStatus.Pass : ComplianceStatus.Warning, room.Id));
            }
            if (room.Type == RoomType.Staircase)
            {
                var g = StairGeometry.For(room.Bounds, storey.Height, Pack.Stair);
                items.Add(Min("B-STAIR-WIDTH", "Stairs", $"{label} flight width", Pack.Stair.MinWidthM, g.FlightWidth / 1000, "m", room.Id));
                items.Add(Max("B-STAIR-RISER", "Stairs", $"{label} riser ({g.Risers} risers)", Pack.Stair.MaxRiserMm, g.Riser, "mm", room.Id));
                items.Add(Min("B-STAIR-LENGTH", "Stairs", $"{label} length for {Pack.Stair.MinTreadMm:0} mm treads", g.RequiredLength / 1000, g.AvailableLength / 1000, "m", room.Id));
            }
        }
    }

    private void CheckParking(BuildingModel model, List<ComplianceItem> items)
    {
        var cars = Math.Max(model.Brief.CarParks, 0);
        var parking = model.AllRooms().Where(r => r.Type == RoomType.Parking).Sum(r => Units.SqMmToSqM(r.Area));
        if (cars == 0 && parking == 0)
        {
            items.Add(new ComplianceItem("B-PARKING", "Parking", "Car parking", $"{Pack.CarsPerDwelling} car(s) typical",
                "none provided", ComplianceStatus.Warning));
            return;
        }
        var required = Math.Max(cars, 1) * Pack.CarSpaceAreaSqm;
        items.Add(Min("B-PARKING", "Parking", $"Parking area for {Math.Max(cars, 1)} car(s)", required, parking, "m²"));
    }

    private static ComplianceItem Min(string id, string cat, string desc, double required, double actual, string unit, Guid? room = null) =>
        new(id, cat, desc, $">= {required:0.##} {unit}".TrimEnd(), $"{actual:0.##} {unit}".TrimEnd(),
            actual + 1e-6 >= required ? ComplianceStatus.Pass : ComplianceStatus.Fail, room);

    private static ComplianceItem Max(string id, string cat, string desc, double limit, double actual, string unit, Guid? room = null) =>
        new(id, cat, desc, $"<= {limit:0.##} {unit}".TrimEnd(), $"{actual:0.##} {unit}".TrimEnd(),
            actual <= limit + 1e-6 ? ComplianceStatus.Pass : ComplianceStatus.Fail, room);
}
