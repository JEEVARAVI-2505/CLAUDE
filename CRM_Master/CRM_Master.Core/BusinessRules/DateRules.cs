using System.Globalization;

namespace CRM_Master.Core.BusinessRules;

/// <summary>Flexible date parsing for spreadsheets (day-first by default, as is usual in India).</summary>
public static class DateRules
{
    private static readonly string[] DayFirst =
    {
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yy", "d/M/yy", "dd-MM-yy",
        "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "dd-MM-yyyy HH:mm:ss", "d MMM yyyy", "dd MMM yyyy", "d MMMM yyyy", "MMM d, yyyy", "MMMM d, yyyy", "dd-MMM-yyyy", "dd-MMM-yy"
    };
    private static readonly string[] MonthFirst = { "MM/dd/yyyy", "M/d/yyyy", "MM-dd-yyyy", "MM/dd/yy", "M/d/yy", "MM/dd/yyyy HH:mm:ss" };
    private static readonly string[] Iso = { "yyyy-MM-dd", "yyyy/MM/dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyyMMdd" };

    /// <summary>Parses text or an Excel serial number into a date (time of day is kept only if present).</summary>
    public static bool TryParse(string? raw, out DateTime result, bool dayFirst = true)
    {
        result = default;
        var t = TextCleaner.Clean(raw);
        if (t is null) return false;
        var style = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
        var ci = CultureInfo.InvariantCulture;

        if (DateTime.TryParseExact(t, Iso, ci, style, out result)) return true;
        var primary = dayFirst ? DayFirst : MonthFirst; var secondary = dayFirst ? MonthFirst : DayFirst;
        if (DateTime.TryParseExact(t, primary, ci, style, out result)) return true;
        if (DateTime.TryParseExact(t, secondary, ci, style, out result)) return true;

        // Excel serial (e.g. 45123): only accept plausible 1950-2100 range.
        if (double.TryParse(t, NumberStyles.Float, ci, out var serial) && serial is > 18264 and < 73415)
        {
            result = DateTime.SpecifyKind(DateTime.FromOADate(serial), DateTimeKind.Utc);
            return true;
        }
        return false;
    }

    /// <summary>A created date is plausible if between 1970 and one day in the future.</summary>
    public static bool IsPlausible(DateTime date) => date.Year >= 1970 && date <= DateTime.UtcNow.AddDays(1);

    public static string ToIso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
