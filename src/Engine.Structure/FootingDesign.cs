using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Structure;

/// <summary>Isolated square footing design (IS 456 cl. 34).</summary>
public static class FootingDesign
{
    /// <summary>
    /// Plan size from service load: A = 1.1 P / SBC (10% for self weight and backfill), square side rounded up
    /// to 100 mm, minimum 1000. Depth from punching shear (cl. 31.6.3) and one-way shear at d from the column face
    /// (cl. 34.2.4.1), minimum 300 mm. Bending at the column face (cl. 34.2.3.1).
    /// </summary>
    /// <param name="pu">Factored column load, kN.</param>
    /// <param name="sbc">Safe bearing capacity, kN/m².</param>
    public static Footing Design(double pu, double colB, double colD, double sbc, double fck, double fy)
    {
        var service = pu / 1.5;
        var area = 1.1 * service / sbc;
        var side = Math.Max(1000, Math.Ceiling(Math.Sqrt(area) * 10 - 1e-9) * 100);
        var qu = pu * 1000 / (side * side); // N/mm²
        var betaC = Math.Min(colB, colD) / Math.Max(colB, colD);
        var tauP = Is456.PunchingStrength(fck, betaC);
        var tauOneWay = Is456.Tc(0.25, fck);

        double d = 200;
        for (; d < 1500; d += 25)
        {
            var vPunch = qu * (side * side - (colB + d) * (colD + d));
            var b0 = 2 * (colB + d + colD + d);
            var oneWayLever = (side - Math.Max(colB, colD)) / 2 - d;
            var vOne = oneWayLever > 0 ? qu * side * oneWayLever : 0;
            if (vPunch / (b0 * d) <= tauP && vOne / (side * d) <= tauOneWay) break;
        }
        var depth = Math.Max(300, Math.Ceiling((d + 60) / 50) * 50);
        var eff = depth - 60;
        var cantilever = (side - Math.Min(colB, colD)) / 2;
        var mu = qu * side * cantilever * cantilever / 2;
        var ast = Math.Max(Is456.AstRequired(mu, side, eff, fck, fy), Is456.SlabAstMin(side, depth));
        var dia = 12.0;
        var count = Math.Max(4, (int)Math.Ceiling(ast / Is456.BarArea(dia)));
        var spacing = Math.Min(200, Math.Floor((side - 100) / Math.Max(1, count - 1) / 10) * 10);
        return new Footing { L = side, B = side, Depth = depth, Reinforcement = $"{dia:0}φ @ {spacing:0} c/c both ways" };
    }
}
