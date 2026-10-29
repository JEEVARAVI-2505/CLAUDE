namespace BuildingStudio.Engine.Structure;

/// <summary>Load values (kN, m) with clause references. Residential defaults; edit through <see cref="StructureSettings"/>.</summary>
public sealed class StructureSettings
{
    /// <summary>Unit weight of RCC, kN/m³ (IS 875-1 Table 1).</summary>
    public double ConcreteUnitWeight { get; init; } = 25;

    /// <summary>Unit weight of brick masonry, kN/m³ (IS 875-1).</summary>
    public double BrickUnitWeight { get; init; } = 20;

    public double FloorFinish { get; init; } = 1.0;
    public double RoofFinish { get; init; } = 2.0;

    /// <summary>Imposed load on floors of dwellings, kN/m² (IS 875-2 Table 1).</summary>
    public double FloorLiveLoad { get; init; } = 2.0;

    /// <summary>Imposed load on accessible flat roof, kN/m² (IS 875-2 Table 2).</summary>
    public double RoofLiveLoad { get; init; } = 1.5;

    /// <summary>Partial safety factor for DL + LL (IS 456 Table 18).</summary>
    public double LoadFactor { get; init; } = 1.5;

    public double MaxSpan { get; init; } = 5000;
    public double MinGridGap { get; init; } = 900;
    public double ClearCover { get; init; } = 25;
}

/// <summary>IS 1893-1:2016 equivalent static seismic method (summary only).</summary>
public static class Is1893
{
    /// <summary>Zone factor Z (Table 3).</summary>
    public static double ZoneFactor(int zone) => zone switch { <= 2 => 0.10, 3 => 0.16, 4 => 0.24, _ => 0.36 };

    /// <summary>Approximate period of an RC moment frame without infill, s (cl. 7.6.2 a): 0.075 h^0.75.</summary>
    public static double Period(double heightM) => 0.075 * Math.Pow(heightM, 0.75);

    /// <summary>Sa/g for medium soil, 5% damping (cl. 6.4.2).</summary>
    public static double SaByG(double t) => t < 0.10 ? 1 + 15 * t : t <= 0.55 ? 2.5 : t <= 4 ? 1.36 / t : 0.34;

    /// <summary>Design horizontal acceleration coefficient Ah = (Z/2)(Sa/g)/(R/I) (cl. 6.4.2).</summary>
    public static double Ah(int zone, double t, double r = 5, double importance = 1.0) => ZoneFactor(zone) / 2 * SaByG(t) / (r / importance);

    /// <summary>Vertical distribution Qi = Vb Wi hi² / Σ Wj hj² (cl. 7.6.3).</summary>
    public static double[] Distribute(double vb, IReadOnlyList<double> weights, IReadOnlyList<double> heights)
    {
        var sum = weights.Select((w, i) => w * heights[i] * heights[i]).Sum();
        return weights.Select((w, i) => sum == 0 ? 0 : vb * w * heights[i] * heights[i] / sum).ToArray();
    }
}

/// <summary>IS 875-3:2015 design wind pressure (summary only).</summary>
public static class Is875Wind
{
    /// <summary>k2 for terrain category 3 (Table 2), heights up to 20 m.</summary>
    public static double K2Terrain3(double heightM) => heightM <= 10 ? 0.91 : heightM <= 15 ? 0.97 : 1.01;

    /// <summary>Design wind pressure pz = 0.6 Vz², N/m², with Vz = Vb k1 k2 k3 k4 (cl. 6.3, 7.2).</summary>
    public static double Pressure(double vb, double heightM, double k1 = 1.0, double k3 = 1.0, double k4 = 1.0)
    {
        var vz = vb * k1 * K2Terrain3(heightM) * k3 * k4;
        return 0.6 * vz * vz;
    }
}
