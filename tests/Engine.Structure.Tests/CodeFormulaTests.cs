using BuildingStudio.Engine.Structure;

namespace BuildingStudio.Engine.Structure.Tests;

/// <summary>Hand-calculated golden values. Each test names the clause it checks.</summary>
public class CodeFormulaTests
{
    [Fact]
    public void Ast_singly_reinforced_IS456_AnnexG_1_1b()
    {
        // Mu = 50 kNm, b = 230, d = 410, M20, Fe500:
        // 4.6 Mu/(fck b d²) = 0.29744; Ast = 0.5(20/500)(1 - √0.70256)(230)(410) = 305.18 mm²
        Is456.AstRequired(50e6, 230, 410, 20, 500).Should().BeApproximately(305.18, 0.05);
    }

    [Fact]
    public void Mu_limit_IS456_AnnexG_1_1c()
    {
        // xu,max = 0.46 d = 188.6; Mu,lim = 0.36(20)(230)(188.6)(410 - 0.42 x 188.6) = 103.31 kNm
        (Is456.MuLimit(230, 410, 20, 500) / 1e6).Should().BeApproximately(103.31, 0.01);
    }

    [Fact]
    public void Section_beyond_capacity_returns_NaN() => double.IsNaN(Is456.AstRequired(400e6, 230, 410, 20, 500)).Should().BeTrue();

    [Fact]
    public void Min_beam_steel_IS456_26_5_1_1() => Is456.BeamAstMin(230, 410, 500).Should().BeApproximately(160.31, 0.01);

    [Fact]
    public void Column_axial_IS456_39_3()
    {
        // Pu = 1000 kN, 300x300: (1e6 - 0.4·20·90000) / (0.67·500 - 0.4·20) = 280000 / 327 = 856.27 mm²
        Is456.ColumnAsc(1000e3, 90000, 20, 500).Should().BeApproximately(856.27, 0.01);
        // Light load is governed by 0.8% minimum (cl. 26.5.3.1 a) = 720 mm²
        Is456.ColumnAsc(300e3, 90000, 20, 500).Should().Be(720);
    }

    [Theory]
    [InlineData(0.15, 0.28)]
    [InlineData(0.50, 0.48)]
    [InlineData(0.375, 0.42)]
    [InlineData(5.0, 0.82)]
    public void Tau_c_Table19_M20(double pt, double expected) => Is456.Tc(pt, 20).Should().BeApproximately(expected, 1e-9);

    [Fact]
    public void Tau_c_max_Table20() => Is456.TcMax(20).Should().Be(2.8);

    [Fact]
    public void Stirrup_spacing_limited_by_IS456_26_5_1_5()
    {
        // Low shear: governed by min(0.75d, 300) = 300 for d = 410.
        Is456.StirrupSpacing(20e3, 230, 410, 0.36, 500, 100.5).Should().Be(300);
        // Vus = 150e3 - 0.36·230·410 = 116052 N; sv = 0.87·500·100.53·410/116052 = 154.5 mm
        Is456.StirrupSpacing(150e3, 230, 410, 0.36, 500, Is456.BarArea(8) * 2).Should().BeApproximately(154.5, 0.2);
    }

    [Theory]
    [InlineData(3000, 120)]
    [InlineData(3500, 110 + 10)]
    [InlineData(4500, 170)]
    public void Slab_thickness_IS456_24_1_and_23_2_1(double span, double expected) => Is456.SlabThickness(span).Should().Be(expected);

    [Fact]
    public void Alpha_x_Table27_interpolates() => Is456.AlphaXSimplySupported(1.25).Should().BeApproximately(0.0885, 1e-9);

    [Fact]
    public void Tie_spacing_IS456_26_5_3_2c() => Is456.TieSpacing(230, 12).Should().Be(192);

    [Fact]
    public void Bar_weight_d2_over_162() => Is456.BarWeightPerMetre(12).Should().BeApproximately(0.8889, 1e-4);

    [Fact]
    public void Punching_strength_IS456_31_6_3_1() => Is456.PunchingStrength(20, 0.77).Should().BeApproximately(1.118, 1e-3);

    [Fact]
    public void Footing_size_from_SBC()
    {
        // Pu = 750 kN → P = 500 kN; A = 1.1 x 500 / 150 = 3.667 m² → 1.915 m → 2000 mm square.
        var f = FootingDesign.Design(750, 300, 300, 150, 20, 500);
        f.L.Should().Be(2000);
        f.Depth.Should().BeGreaterThanOrEqualTo(300);
    }

    [Fact]
    public void Seismic_IS1893_6_4_2_and_7_6_2()
    {
        // h = 6.9 m: T = 0.075 x 6.9^0.75 = 0.319 s → Sa/g = 2.5; zone III: Ah = (0.16/2)(2.5)/(5/1) = 0.04
        Is1893.Period(6.9).Should().BeApproximately(0.3193, 1e-4);
        Is1893.Ah(3, 0.3193).Should().BeApproximately(0.04, 1e-12);
        Is1893.Distribute(100, [500, 400], [3, 6]).Sum().Should().BeApproximately(100, 1e-9);
        Is1893.Distribute(100, [500, 400], [3, 6])[1].Should().BeApproximately(100 * 400 * 36 / (500 * 9 + 400 * 36.0), 1e-9);
    }

    [Fact]
    public void Wind_IS875_3() => Is875Wind.Pressure(50, 6.9).Should().BeApproximately(1242.15, 0.01);

    [Fact]
    public void Bar_choice_meets_area()
    {
        var (count, dia) = Bars.Choose(856, [12, 16, 20], 4, 12, even: true);
        (count * Is456.BarArea(dia)).Should().BeGreaterThanOrEqualTo(856);
        (count % 2).Should().Be(0);
    }
}
