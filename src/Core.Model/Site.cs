using System.Text.Json.Serialization;
using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Core.Model;

/// <summary>Setbacks in millimetres, relative to the road (front) side.</summary>
public sealed class Setbacks
{
    public double Front { get; set; } = 1500;
    public double Rear { get; set; } = 1000;
    public double Side { get; set; } = 750;
}

/// <summary>
/// Rectangular plot. Plan X runs along <see cref="PlotWidth"/>, plan Y along <see cref="PlotDepth"/>.
/// With <see cref="NorthAngle"/> = 0, plan +Y is true north and plan +X is east.
/// </summary>
public sealed class Site
{
    public double PlotWidth { get; set; } = 9144;
    public double PlotDepth { get; set; } = 12192;

    /// <summary>Road side of the plot.</summary>
    public CardinalSide Facing { get; set; } = CardinalSide.E;

    /// <summary>Angle of true north, counter-clockwise from plan +Y, in radians.</summary>
    public double NorthAngle { get; set; }

    public Setbacks Setbacks { get; set; } = new();

    /// <summary>Safe bearing capacity of soil, kN/m².</summary>
    public double SoilSbc { get; set; } = 150;

    /// <summary>IS 1893 seismic zone (2, 3, 4 or 5).</summary>
    public int SeismicZone { get; set; } = 3;

    /// <summary>IS 875-3 basic wind speed, m/s.</summary>
    public double BasicWindSpeed { get; set; } = 50;

    [JsonIgnore] public Rect2 Plot => new(0, 0, PlotWidth, PlotDepth);
    [JsonIgnore] public double PlotArea => PlotWidth * PlotDepth;

    /// <summary>
    /// Plot side in plan terms for a compass side, assuming north angle is 0 (rotated plots use the facing as-is).
    /// </summary>
    public double SetbackFor(CardinalSide side) =>
        side == Facing ? Setbacks.Front : side == Directions.Opposite(Facing) ? Setbacks.Rear : Setbacks.Side;

    /// <summary>Buildable envelope = plot minus setbacks (N = max Y, E = max X).</summary>
    public Rect2 Envelope() => Rect2.FromMinMax(
        SetbackFor(CardinalSide.W), SetbackFor(CardinalSide.S),
        PlotWidth - SetbackFor(CardinalSide.E), PlotDepth - SetbackFor(CardinalSide.N));
}
