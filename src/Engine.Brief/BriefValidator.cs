using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;

namespace BuildingStudio.Engine.Brief;

public enum IssueSeverity { Info, Warning, Error }

public sealed record BriefIssue(IssueSeverity Severity, string Message, string Suggestion = "");

/// <summary>Checks that a brief can be built before any generation runs.</summary>
public sealed class BriefValidator(BylawRulePack bylaws)
{
    /// <summary>Corridor width assumed for circulation estimates, mm.</summary>
    public const double CorridorWidth = 1050;

    /// <summary>Staircase footprint assumed per floor when the building has more than one storey, mm².</summary>
    public const double StairArea = 2400.0 * 4200.0;

    public IReadOnlyList<BriefIssue> Validate(Core.Model.Brief brief)
    {
        var issues = new List<BriefIssue>();
        var site = brief.Site;
        if (site.PlotWidth < 3000 || site.PlotDepth < 3000)
        {
            issues.Add(new(IssueSeverity.Error, "Plot is smaller than 3 m on a side.", "Check plot dimensions and units (feet vs metres)."));
            return issues;
        }
        var env = site.Envelope();
        if (env.Width < 3000 || env.Height < 3000)
        {
            issues.Add(new(IssueSeverity.Error, $"Buildable envelope after setbacks is only {env.Width / 1000:0.0} x {env.Height / 1000:0.0} m.",
                "Reduce setbacks (if bylaws allow) or use a larger plot."));
            return issues;
        }
        if (brief.Floors is < 1 or > 6)
            issues.Add(new(IssueSeverity.Error, "Floors must be between G (1) and G+5 (6)."));
        if (brief.FloorHeight < bylaws.MinFloorHeightM * 1000)
            issues.Add(new(IssueSeverity.Error, $"Floor height {brief.FloorHeight / 1000:0.00} m is below the {bylaws.MinFloorHeightM} m minimum."));

        var heightM = brief.Floors * brief.FloorHeight / 1000.0;
        if (heightM > bylaws.MaxHeightM)
            issues.Add(new(IssueSeverity.Error, $"Building height {heightM:0.0} m exceeds the {bylaws.MaxHeightM} m limit.", "Reduce the number of floors."));

        foreach (var r in brief.Rooms.Where(r => r.Width <= 0 || r.Depth <= 0 || r.Count < 1))
            issues.Add(new(IssueSeverity.Error, $"{RoomTypes.DisplayName(r.Type)} has an invalid size or count."));
        foreach (var r in brief.Rooms.Where(r => r.Floor < 0 || r.Floor >= brief.Floors))
            issues.Add(new(IssueSeverity.Error, $"{RoomTypes.DisplayName(r.Type)} is on floor {r.Floor}, but the building has {brief.Floors} floor(s)."));

        var available = env.Area;
        for (var f = 0; f < brief.Floors; f++)
        {
            var requested = RequestedArea(brief, f);
            var name = f == 0 ? "Ground floor" : $"Floor {f}";
            if (requested == 0)
            {
                issues.Add(new(IssueSeverity.Warning, $"{name} has no rooms.", "Add rooms or reduce the number of floors."));
                continue;
            }
            var needed = requested + CorridorWidth * Math.Max(env.Width, env.Height) + (brief.Floors > 1 ? StairArea : 0);
            if (needed > available * 1.05)
            {
                var over = Units.SqMmToSqM(needed - available);
                issues.Add(new(IssueSeverity.Error,
                    $"{name} needs about {Units.SqMmToSqM(needed):0} m² (rooms + circulation) but only {Units.SqMmToSqM(available):0} m² is buildable.",
                    $"Reduce room areas by about {over:0} m², move a room to another floor, or add a floor."));
            }
            else if (needed < available * 0.6)
            {
                issues.Add(new(IssueSeverity.Info,
                    $"{name} uses about {needed / available * 100:0}% of the envelope; rooms will be enlarged to fill it.",
                    "Add rooms or reduce the footprint if you want a smaller house."));
            }
        }

        var far = brief.Floors * available / site.PlotArea;
        if (far > bylaws.MaxFar)
            issues.Add(new(IssueSeverity.Warning, $"Estimated FAR {far:0.00} exceeds the {bylaws.MaxFar:0.00} limit.", "Reduce floors or footprint."));
        var cars = brief.Rooms.Where(r => r.Type == RoomType.Parking).Sum(r => r.Count);
        if (cars == 0 && brief.CarParks > 0)
            issues.Add(new(IssueSeverity.Warning, "Car parking requested but no Parking room in the list."));
        return issues;
    }

    /// <summary>Sum of requested room areas on a floor including attached baths, mm².</summary>
    public static double RequestedArea(Core.Model.Brief brief, int floor)
    {
        var bath = RoomDefaults.SizeMm(RoomType.Bath);
        return brief.Rooms.Where(r => r.Floor == floor)
            .Sum(r => r.Count * (r.TargetArea + (r.AttachedBath ? bath.Width * bath.Depth : 0)));
    }

    /// <summary>Copies the bylaw setbacks for this plot size into the brief (TN template values).</summary>
    public void ApplyBylawSetbacks(Core.Model.Brief brief)
    {
        var band = bylaws.SetbacksFor(Units.SqMmToSqM(brief.Site.PlotArea));
        brief.Site.Setbacks = new Setbacks { Front = band.FrontM * 1000, Rear = band.RearM * 1000, Side = band.SideM * 1000 };
    }
}
