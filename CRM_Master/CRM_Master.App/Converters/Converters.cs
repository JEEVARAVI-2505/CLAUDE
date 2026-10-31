using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CRM_Master.App.Converters;

/// <summary>true -> Visible. ConverterParameter "invert" flips it.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is true; if (parameter as string == "invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

/// <summary>Visible when the value is non-null (and, for strings, non-empty). "invert" flips it.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var has = value is not null && !(value is string s && s.Length == 0); if (parameter as string == "invert") has = !has;
        return has ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Visible when a collection / count is greater than zero. "invert" flips it.</summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var n = value switch { int i => i, ICollection c => c.Count, IEnumerable e => e.Cast<object>().Count(), _ => 0 };
        var has = n > 0; if (parameter as string == "invert") has = !has;
        return has ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>UTC date -> local time text. Parameter is the format (default "yyyy-MM-dd HH:mm").</summary>
public sealed class UtcToLocalConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fmt = parameter as string ?? "yyyy-MM-dd HH:mm";
        return value switch
        {
            DateTime d => DateTime.SpecifyKind(d, DateTimeKind.Utc).ToLocalTime().ToString(fmt, CultureInfo.CurrentCulture),
            _ => ""
        };
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Formats a double 0..1 as a percentage ("92%").</summary>
public sealed class FractionToPercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double d ? (d * 100).ToString("0", CultureInfo.CurrentCulture) + "%" : "";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
