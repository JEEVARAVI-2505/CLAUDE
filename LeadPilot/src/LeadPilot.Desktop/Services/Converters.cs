using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LeadPilot.Desktop.Services;

/// <summary>true -> Visible, false -> Collapsed. Parameter "invert" flips it.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var b = value is bool v && v;
        if (parameter is string s && s == "invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Logical NOT.</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : value!;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : value;
}

/// <summary>null or empty string -> Collapsed, otherwise Visible.</summary>
public sealed class HasValueToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var has = value switch { null => false, string s => s.Length > 0, int i => i != 0, _ => true };
        if (parameter is string p && p == "invert") has = !has;
        return has ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Maps a relevance name (Hot/Warm/Cold/Unqualified) or accent name (Blue/Green/…) to a theme brush.</summary>
public sealed class NameToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = (value?.ToString() ?? "") switch
        {
            "Hot" or "Red" or "Overdue" => value!.ToString() == "Hot" ? "HotBrush" : "DangerBrush",
            "Warm" or "Amber" or "Orange" => "WarmBrush",
            "Cold" or "Blue" => "ColdBrush",
            "Green" or "Won" or "Done" or "Applied" => "SuccessBrush",
            "Teal" => "PrimaryBrush",
            _ => "NoneBrush"
        };
        return System.Windows.Application.Current.TryFindResource(key) ?? Brushes.Gray;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>double 0..1 -> star GridLength (for proportional bars).</summary>
public sealed class FractionToStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var f = value is double d ? Math.Clamp(d, 0, 1) : 0;
        var rest = parameter is string p && p == "rest";
        return new GridLength(rest ? Math.Max(0.0001, 1 - f) : Math.Max(0.0001, f), GridUnitType.Star);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Score 0..100 -> star GridLength (progress bar inside a cell).</summary>
public sealed class ScoreToStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value is int i ? Math.Clamp(i, 0, 100) : 0;
        var rest = parameter is string p && p == "rest";
        return new GridLength(Math.Max(0.0001, rest ? 100 - s : s), GridUnitType.Star);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Binds a PasswordBox's Password (which is not a dependency property).</summary>
public static class PasswordHelper
{
    public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordHelper),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChanged));
    private static readonly DependencyProperty UpdatingProperty = DependencyProperty.RegisterAttached("Updating", typeof(bool), typeof(PasswordHelper));

    public static string GetPassword(DependencyObject d) => (string)d.GetValue(PasswordProperty);
    public static void SetPassword(DependencyObject d, string value) => d.SetValue(PasswordProperty, value);

    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box) return;
        box.PasswordChanged -= Changed;
        if (!(bool)box.GetValue(UpdatingProperty) && box.Password != (string)e.NewValue) box.Password = (string?)e.NewValue ?? "";
        box.PasswordChanged += Changed;
    }

    private static void Changed(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender;
        box.SetValue(UpdatingProperty, true);
        SetPassword(box, box.Password);
        box.SetValue(UpdatingProperty, false);
    }
}
