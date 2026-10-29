using BuildingStudio.Core.Model;

namespace BuildingStudio.Core.Rules;

public sealed class SetbackBand
{
    public double MaxPlotAreaSqm { get; set; }
    public double FrontM { get; set; }
    public double RearM { get; set; }
    public double SideM { get; set; }
}

public sealed class MinRoomSize
{
    public double AreaSqm { get; set; }
    public double WidthM { get; set; }
}

public sealed class StairRule
{
    public double MinWidthM { get; set; } = 0.9;
    public double MaxRiserMm { get; set; } = 190;
    public double MinTreadMm { get; set; } = 250;
}

/// <summary>Building bylaw rule pack (rules/bylaws.*.json). Values are templates to be verified locally.</summary>
public sealed class BylawRulePack
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Source { get; set; } = "";
    public string Disclaimer { get; set; } = "";
    public List<SetbackBand> Setbacks { get; set; } = [];
    public double MaxFar { get; set; } = 2.0;
    public double MaxCoverage { get; set; } = 0.75;
    public double MaxHeightM { get; set; } = 12;
    public double MinFloorHeightM { get; set; } = 2.75;
    public double MinCorridorWidthM { get; set; } = 1.0;
    public double MinOpeningToFloorRatio { get; set; } = 0.1;
    public double CarSpaceAreaSqm { get; set; } = 12.5;
    public int CarsPerDwelling { get; set; } = 1;
    public Dictionary<RoomType, MinRoomSize> MinRoomSizes { get; set; } = [];
    public StairRule Stair { get; set; } = new();

    /// <summary>Required setbacks for a plot area (m²); the first band whose limit covers the area.</summary>
    public SetbackBand SetbacksFor(double plotAreaSqm) =>
        Setbacks.OrderBy(b => b.MaxPlotAreaSqm).FirstOrDefault(b => plotAreaSqm <= b.MaxPlotAreaSqm)
        ?? Setbacks.OrderBy(b => b.MaxPlotAreaSqm).LastOrDefault()
        ?? new SetbackBand();
}
