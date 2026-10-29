namespace BuildingStudio.Engine.Layout;

/// <summary>Fixed parameters of one layout family.</summary>
/// <param name="CorridorAlongY">True when the corridor runs along plan Y (bands are west/east strips).</param>
/// <param name="StairBand">Band index (0 = low side, 1 = high side) holding the staircase.</param>
/// <param name="StairAtStart">Staircase at the low end of the band (true) or high end.</param>
/// <param name="SingleBand">Single-loaded corridor (narrow plots).</param>
/// <param name="CorridorLow">Single-loaded only: corridor on the low side of the envelope instead of the high side.</param>
public sealed record LayoutConfig(bool CorridorAlongY, int StairBand, bool StairAtStart, bool SingleBand, double CorridorWidth, bool CorridorLow = false)
{
    public string Describe() =>
        $"{(CorridorAlongY ? "N-S" : "E-W")} {(SingleBand ? "single-loaded" : "central")} corridor";
}

/// <summary>A slot along a band: one room, or two small rooms stacked across the band depth (first = exterior side).</summary>
public sealed class Slot(List<RoomSpec> rooms)
{
    public List<RoomSpec> Rooms { get; } = rooms;
    public double Area => Rooms.Sum(r => r.TargetArea);
    public Slot Clone() => new([.. Rooms]);
}

/// <summary>Room order per band for every floor.</summary>
public sealed class Genome(LayoutConfig config, List<Slot>[][] floors)
{
    public LayoutConfig Config { get; } = config;

    /// <summary>[floor][band] → ordered slots.</summary>
    public List<Slot>[][] Floors { get; } = floors;

    public Genome Clone() =>
        new(Config, Floors.Select(f => f.Select(b => b.Select(s => s.Clone()).ToList()).ToArray()).ToArray());

    public IEnumerable<Slot> AllSlots(int floor) => Floors[floor].SelectMany(b => b);
}
