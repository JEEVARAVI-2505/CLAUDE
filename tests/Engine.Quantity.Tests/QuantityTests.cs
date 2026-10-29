using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Quantity;

namespace BuildingStudio.Engine.Quantity.Tests;

public class QuantityTests
{
    private static readonly RateTable Rates = RuleRepository.Load().Rates;

    /// <summary>One 4 x 5 m room, 3 m storey, 125 mm slab, four 230 mm exterior walls, no openings, no structure.</summary>
    private static BuildingModel Box()
    {
        var m = new BuildingModel();
        var env = new Rect2(0, 0, 4000, 5000);
        var s = new Storey { Envelope = env, Height = 3000, SlabThickness = 125 };
        var room = new Room { Type = RoomType.Living, Name = "Living", Bounds = env };
        s.Rooms.Add(room);
        foreach (var (_, e) in env.Edges())
            s.Walls.Add(new Wall { Start = e.A, End = e.B, Thickness = 230, Type = WallType.Exterior, RoomA = room.Id });
        m.Storeys.Add(s);
        return m;
    }

    private static double Qty(CostSummary c, string code) => c.Lines.Where(l => l.Code == code).Sum(l => l.Quantity);

    [Fact]
    public void Golden_masonry_plaster_and_flooring()
    {
        var c = new QuantityEngine(Rates).Estimate(Box());
        // Centre-line method: 18 m x (3.000 - 0.125) m x 0.23 m = 11.9025 m³
        Qty(c, "BRICK230").Should().BeApproximately(11.90, 0.01);
        // Parapet 115 mm: 18 m x 1.0 m = 18 m²
        Qty(c, "BRICK115").Should().BeApproximately(18, 1e-9);
        // External plaster: 18 x 2.875 = 51.75 + parapet both faces 36 = 87.75 m²
        Qty(c, "PLASTER_EXT").Should().BeApproximately(87.75, 1e-9);
        // Internal plaster 51.75 m²; interior paint = plaster + ceiling 20 = 71.75 m²
        Qty(c, "PAINT_INT").Should().BeApproximately(71.75, 1e-9);
        Qty(c, "FLOOR_VITRIFIED").Should().Be(20);
    }

    [Fact]
    public void Cost_roll_up_arithmetic()
    {
        var c = new QuantityEngine(Rates).Estimate(Box());
        c.Subtotal.Should().BeApproximately(c.Lines.Sum(l => l.Amount), 0.01);
        c.Contingency.Should().BeApproximately(c.Subtotal * 0.03, 0.01);
        c.Margin.Should().BeApproximately((c.Subtotal + c.Contingency) * 0.10, 0.01);
        c.Gst.Should().BeApproximately((c.Subtotal + c.Contingency + c.Margin) * 0.18, 0.01);
        c.Total.Should().BeApproximately(c.Subtotal + c.Contingency + c.Margin + c.Gst, 0.01);
        c.BuiltUpSqft.Should().BeApproximately(215.3, 0.1);
        c.CostPerSqft.Should().Be(Math.Round(c.Total / (20e6 / Units.SqMmPerSqFt), 0));
    }

    [Fact]
    public void Openings_are_deducted_and_counted()
    {
        var m = Box();
        var s = m.Storeys[0];
        s.Openings.Add(new Opening { WallId = s.Walls[0].Id, Offset = 500, Width = 1500, Height = 1200, Sill = 900, Kind = OpeningKind.Window });
        s.Openings.Add(new Opening { WallId = s.Walls[1].Id, Offset = 500, Width = 1050, Height = 2100, Kind = OpeningKind.Door, IsMainEntrance = true });
        var c = new QuantityEngine(Rates).Estimate(m);
        // 11.9025 - (1.5 x 1.2 + 1.05 x 2.1) x 0.23 = 11.9025 - 0.92115 = 10.98135
        Qty(c, "BRICK230").Should().BeApproximately(10.98, 0.01);
        Qty(c, "WINDOW").Should().BeApproximately(1.8, 1e-9);
        Qty(c, "DOOR_MAIN").Should().Be(1);
    }

    [Fact]
    public void Rates_drive_amounts()
    {
        var c = new QuantityEngine(Rates).Estimate(Box());
        var line = c.Lines.Single(l => l.Code == "FLOOR_VITRIFIED");
        line.Amount.Should().Be(20 * Rates.Get("FLOOR_VITRIFIED").Rate);
    }
}
