namespace BuildingStudio.Core.Geometry;

/// <summary>Geometric tolerances and snapping (millimetres).</summary>
public static class Tolerance
{
    /// <summary>Numeric epsilon.</summary>
    public const double Eps = 1e-9;

    /// <summary>Default coordinate tolerance: half a millimetre.</summary>
    public const double Mm = 0.5;

    public static bool Near(double a, double b, double tol = Mm) => Math.Abs(a - b) <= tol;

    /// <summary>Rounds to the nearest multiple of <paramref name="module"/>.</summary>
    public static double Snap(double value, double module) =>
        module <= 0 ? value : Math.Round(value / module, MidpointRounding.AwayFromZero) * module;

    public static double SnapDown(double value, double module) =>
        module <= 0 ? value : Math.Floor(value / module + 1e-9) * module;

    public static double SnapUp(double value, double module) =>
        module <= 0 ? value : Math.Ceiling(value / module - 1e-9) * module;
}

/// <summary>Unit conversion helpers, used only at UI/IO edges.</summary>
public static class Units
{
    public const double MmPerFoot = 304.8;
    public const double SqMmPerSqM = 1_000_000;
    public const double SqMmPerSqFt = 92_903.04;

    public static double FtToMm(double ft) => ft * MmPerFoot;
    public static double MmToFt(double mm) => mm / MmPerFoot;
    public static double MmToM(double mm) => mm / 1000.0;
    public static double SqMmToSqM(double a) => a / SqMmPerSqM;
    public static double SqMmToSqFt(double a) => a / SqMmPerSqFt;
    public static double SqMToSqMm(double a) => a * SqMmPerSqM;
    public static double DegToRad(double d) => d * Math.PI / 180.0;
    public static double RadToDeg(double r) => r * 180.0 / Math.PI;
}
