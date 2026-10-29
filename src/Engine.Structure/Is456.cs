namespace BuildingStudio.Engine.Structure;

/// <summary>IS 456:2000 limit-state formulas. Units: N, mm, N/mm² unless noted. Each method cites its clause.</summary>
public static class Is456
{
    /// <summary>xu,max/d for the steel grade (cl. 38.1, note).</summary>
    public static double XuMaxRatio(double fy) => fy <= 250 ? 0.53 : fy <= 415 ? 0.48 : 0.46;

    /// <summary>Limiting moment of resistance, N·mm (Annex G-1.1 c): 0.36 fck b xu,max (d - 0.42 xu,max).</summary>
    public static double MuLimit(double b, double d, double fck, double fy)
    {
        var xu = XuMaxRatio(fy) * d;
        return 0.36 * fck * b * xu * (d - 0.42 * xu);
    }

    /// <summary>
    /// Tension steel for a singly reinforced section, mm² (Annex G-1.1 b rearranged):
    /// Ast = 0.5 fck/fy [1 - sqrt(1 - 4.6 Mu / (fck b d²))] b d. Returns NaN if Mu exceeds the section.
    /// </summary>
    public static double AstRequired(double mu, double b, double d, double fck, double fy)
    {
        var k = 1 - 4.6 * mu / (fck * b * d * d);
        return k < 0 ? double.NaN : 0.5 * fck / fy * (1 - Math.Sqrt(k)) * b * d;
    }

    /// <summary>Minimum beam tension steel, mm² (cl. 26.5.1.1): 0.85 b d / fy.</summary>
    public static double BeamAstMin(double b, double d, double fy) => 0.85 * b * d / fy;

    /// <summary>Minimum slab/footing steel, mm² (cl. 26.5.2.1): 0.12% of gross area for HYSD bars.</summary>
    public static double SlabAstMin(double b, double overallDepth) => 0.0012 * b * overallDepth;

    private static readonly double[] Pt = [0.15, 0.25, 0.50, 0.75, 1.00, 1.25, 1.50, 1.75, 2.00, 2.25, 2.50, 2.75, 3.00];
    private static readonly double[] TcM20 = [0.28, 0.36, 0.48, 0.56, 0.62, 0.67, 0.72, 0.75, 0.79, 0.81, 0.82, 0.82, 0.82];
    private static readonly double[] TcM25 = [0.29, 0.36, 0.49, 0.57, 0.64, 0.70, 0.74, 0.78, 0.82, 0.85, 0.88, 0.90, 0.92];

    /// <summary>Design shear strength of concrete τc, N/mm² (Table 19), linear interpolation on pt (%).</summary>
    public static double Tc(double ptPercent, double fck)
    {
        var table = fck >= 25 ? TcM25 : TcM20;
        if (ptPercent <= Pt[0]) return table[0];
        for (var i = 1; i < Pt.Length; i++)
            if (ptPercent <= Pt[i])
                return table[i - 1] + (table[i] - table[i - 1]) * (ptPercent - Pt[i - 1]) / (Pt[i] - Pt[i - 1]);
        return table[^1];
    }

    /// <summary>Maximum shear stress τc,max, N/mm² (Table 20).</summary>
    public static double TcMax(double fck) => fck >= 40 ? 4.0 : fck >= 35 ? 3.7 : fck >= 30 ? 3.5 : fck >= 25 ? 3.1 : fck >= 20 ? 2.8 : 2.5;

    /// <summary>
    /// Vertical stirrup spacing, mm: strength (cl. 40.4 a) sv = 0.87 fy Asv d / Vus, minimum shear steel
    /// (cl. 26.5.1.6) sv ≤ 0.87 fy Asv / (0.4 b), and maximum spacing (cl. 26.5.1.5) min(0.75 d, 300).
    /// </summary>
    public static double StirrupSpacing(double vu, double b, double d, double tc, double fy, double asv)
    {
        var vus = vu - tc * b * d;
        var byStrength = vus > 0 ? 0.87 * fy * asv * d / vus : double.MaxValue;
        var byMinimum = 0.87 * fy * asv / (0.4 * b);
        return Math.Min(Math.Min(byStrength, byMinimum), Math.Min(0.75 * d, 300));
    }

    /// <summary>
    /// Axially loaded short column (cl. 39.3): Pu = 0.4 fck Ac + 0.67 fy Asc, with Ac = Ag - Asc.
    /// Returns required Asc, mm² (not less than 0.8% of Ag, cl. 26.5.3.1 a).
    /// </summary>
    public static double ColumnAsc(double pu, double ag, double fck, double fy)
    {
        var asc = (pu - 0.4 * fck * ag) / (0.67 * fy - 0.4 * fck);
        return Math.Max(asc, 0.008 * ag);
    }

    /// <summary>Axial capacity, N (cl. 39.3).</summary>
    public static double ColumnCapacity(double ag, double asc, double fck, double fy) => 0.4 * fck * (ag - asc) + 0.67 * fy * asc;

    /// <summary>Lateral tie spacing, mm (cl. 26.5.3.2 c): min(least lateral dimension, 16 φ, 300).</summary>
    public static double TieSpacing(double leastDimension, double mainBarDia) => Math.Min(Math.Min(leastDimension, 16 * mainBarDia), 300);

    /// <summary>Tie diameter (cl. 26.5.3.2 c): ≥ max(φ/4, 6 mm).</summary>
    public static double TieDiameter(double mainBarDia) => Math.Max(mainBarDia / 4, 6) <= 6 ? 6 : 8;

    /// <summary>
    /// Slab overall depth, mm. Short span ≤ 3.5 m: span/overall depth ≤ 40 × 0.8 = 32 for continuous two-way slabs
    /// (cl. 24.1 note 2). Otherwise basic span/effective depth 26 (cl. 23.2.1) × modification factor 1.2 assumed, + 25 mm.
    /// Minimum 120 mm. Rounded up to 5 mm.
    /// </summary>
    public static double SlabThickness(double shortSpan)
    {
        var d = shortSpan <= 3500 ? shortSpan / 32 : shortSpan / (26 * 1.2) + 25;
        return Math.Max(120, Math.Ceiling(d / 5 - 1e-9) * 5);
    }

    private static readonly double[] Ratio = [1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.75, 2.0];
    private static readonly double[] AlphaX = [0.062, 0.074, 0.084, 0.093, 0.099, 0.104, 0.113, 0.118];

    /// <summary>Simply supported two-way slab moment coefficient αx (Table 27).</summary>
    public static double AlphaXSimplySupported(double lyOverLx)
    {
        if (lyOverLx <= 1) return AlphaX[0];
        for (var i = 1; i < Ratio.Length; i++)
            if (lyOverLx <= Ratio[i])
                return AlphaX[i - 1] + (AlphaX[i] - AlphaX[i - 1]) * (lyOverLx - Ratio[i - 1]) / (Ratio[i] - Ratio[i - 1]);
        return AlphaX[^1];
    }

    /// <summary>Punching shear strength ks·0.25√fck, N/mm² (cl. 31.6.3.1), ks = 0.5 + βc ≤ 1.</summary>
    public static double PunchingStrength(double fck, double betaC) => Math.Min(1, 0.5 + betaC) * 0.25 * Math.Sqrt(fck);

    /// <summary>Bar weight, kg/m: d²/162.</summary>
    public static double BarWeightPerMetre(double dia) => dia * dia / 162.0;

    /// <summary>Area of one bar, mm².</summary>
    public static double BarArea(double dia) => Math.PI * dia * dia / 4;
}
