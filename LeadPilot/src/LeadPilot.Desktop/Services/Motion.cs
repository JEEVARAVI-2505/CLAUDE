using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace LeadPilot.Desktop.Services;

/// <summary>
/// Attached behaviours that animate the element's own transform (styles cannot do this safely because style values are frozen):
/// Enter (page entrance), Reveal (staggered fade-and-rise for list items/cards), Show (re-plays whenever the element becomes visible),
/// Grow (bars grow from their base), CountUp (numbers count up), Lift (hover raise + shadow).
/// </summary>
public static class Motion
{
    private static readonly IEasingFunction EaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };

    // ---------- Enter ----------
    public static readonly DependencyProperty EnterProperty = DependencyProperty.RegisterAttached("Enter", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnEnterChanged));
    public static bool GetEnter(DependencyObject d) => (bool)d.GetValue(EnterProperty);
    public static void SetEnter(DependencyObject d, bool v) => d.SetValue(EnterProperty, v);

    private static void OnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || !(bool)e.NewValue) return;
        fe.Opacity = 0;
        RoutedEventHandler? h = null;
        h = (_, _) => { fe.Loaded -= h; FadeSlide(fe, fe is Window ? 0 : 20, TimeSpan.Zero, fe is Window ? 450 : 420); };
        fe.Loaded += h;
    }

    // ---------- Reveal (staggered) ----------
    public static readonly DependencyProperty RevealProperty = DependencyProperty.RegisterAttached("Reveal", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnRevealChanged));
    public static bool GetReveal(DependencyObject d) => (bool)d.GetValue(RevealProperty);
    public static void SetReveal(DependencyObject d, bool v) => d.SetValue(RevealProperty, v);

    /// <summary>Extra delay in milliseconds for static (non-list) elements, to sequence sections.</summary>
    public static readonly DependencyProperty DelayProperty = DependencyProperty.RegisterAttached("Delay", typeof(double), typeof(Motion), new PropertyMetadata(0.0));
    public static double GetDelay(DependencyObject d) => (double)d.GetValue(DelayProperty);
    public static void SetDelay(DependencyObject d, double v) => d.SetValue(DelayProperty, v);

    private static void OnRevealChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || !(bool)e.NewValue) return;
        fe.Opacity = 0;
        Translate(fe).Y = 16;
        RoutedEventHandler? h = null;
        h = (_, _) =>
        {
            fe.Loaded -= h;
            var delay = Math.Min(IndexOf(fe), 14) * 45 + GetDelay(fe) + 80;
            FadeSlide(fe, 16, TimeSpan.FromMilliseconds(delay), 420);
        };
        fe.Loaded += h;
    }

    // ---------- Show (plays each time it becomes visible) ----------
    public static readonly DependencyProperty ShowProperty = DependencyProperty.RegisterAttached("Show", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnShowChanged));
    public static bool GetShow(DependencyObject d) => (bool)d.GetValue(ShowProperty);
    public static void SetShow(DependencyObject d, bool v) => d.SetValue(ShowProperty, v);

    private static void OnShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || !(bool)e.NewValue) return;
        fe.IsVisibleChanged += (_, a) => { if (a.NewValue is true) FadeSlide(fe, 10, TimeSpan.Zero, 300); };
    }

    // ---------- Grow ----------
    public static readonly DependencyProperty GrowProperty = DependencyProperty.RegisterAttached("Grow", typeof(string), typeof(Motion), new PropertyMetadata(null, OnGrowChanged));
    public static string? GetGrow(DependencyObject d) => (string?)d.GetValue(GrowProperty);
    public static void SetGrow(DependencyObject d, string? v) => d.SetValue(GrowProperty, v);

    private static void OnGrowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not string axis) return;
        var horizontal = axis == "X";
        var scale = new ScaleTransform(horizontal ? 0 : 1, horizontal ? 1 : 0);
        fe.RenderTransformOrigin = horizontal ? new Point(0, 0.5) : new Point(0.5, 1);
        fe.RenderTransform = scale;
        RoutedEventHandler? h = null;
        h = (_, _) =>
        {
            fe.Loaded -= h;
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(700)) { BeginTime = TimeSpan.FromMilliseconds(Math.Min(IndexOf(fe), 14) * 50 + 150), EasingFunction = EaseOut };
            scale.BeginAnimation(horizontal ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty, anim);
        };
        fe.Loaded += h;
    }

    // ---------- CountUp ----------
    public static readonly DependencyProperty CountUpProperty = DependencyProperty.RegisterAttached("CountUp", typeof(string), typeof(Motion), new PropertyMetadata(null, OnCountUpChanged));
    public static string? GetCountUp(DependencyObject d) => (string?)d.GetValue(CountUpProperty);
    public static void SetCountUp(DependencyObject d, string? v) => d.SetValue(CountUpProperty, v);

    private static readonly DependencyProperty ShownProperty = DependencyProperty.RegisterAttached("Shown", typeof(double), typeof(Motion), new PropertyMetadata(0.0, OnShownChanged));

    private static void OnCountUpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb) return;
        var text = e.NewValue as string ?? "";
        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (digits.Length == 0 || !double.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var target)) { tb.Text = text; return; }
        var from = (double)tb.GetValue(ShownProperty);
        if (Math.Abs(from - target) < 0.5) { tb.Text = text; return; }
        tb.BeginAnimation(ShownProperty, new DoubleAnimation(from, target, TimeSpan.FromMilliseconds(Math.Min(1100, 350 + Math.Abs(target - from) * 8))) { EasingFunction = EaseOut });
    }

    private static void OnShownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock tb) tb.Text = Math.Round((double)e.NewValue).ToString("N0", CultureInfo.CurrentCulture);
    }

    // ---------- Lift (hover) ----------
    public static readonly DependencyProperty LiftProperty = DependencyProperty.RegisterAttached("Lift", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnLiftChanged));
    public static bool GetLift(DependencyObject d) => (bool)d.GetValue(LiftProperty);
    public static void SetLift(DependencyObject d, bool v) => d.SetValue(LiftProperty, v);

    private static void OnLiftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || !(bool)e.NewValue) return;
        fe.MouseEnter += (_, _) =>
        {
            fe.Effect = new DropShadowEffect { Color = Colors.Black, Opacity = 0.16, BlurRadius = 20, ShadowDepth = 5, Direction = 270 };
            Translate(fe).BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-4, TimeSpan.FromMilliseconds(150)) { EasingFunction = EaseOut });
        };
        fe.MouseLeave += (_, _) =>
        {
            fe.Effect = null;
            Translate(fe).BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(180)) { EasingFunction = EaseOut });
        };
    }

    // ---------- helpers ----------
    private static TranslateTransform Translate(UIElement e)
    {
        if (e.RenderTransform is TranslateTransform t && !t.IsFrozen) return t;
        var n = new TranslateTransform(); e.RenderTransform = n; return n;
    }

    private static int IndexOf(FrameworkElement fe)
    {
        try
        {
            DependencyObject? container = fe.TemplatedParent ?? VisualTreeHelper.GetParent(fe);
            if (container is ContentPresenter cp && ItemsControl.ItemsControlFromItemContainer(cp) is { } ic)
                return Math.Max(0, ic.ItemContainerGenerator.IndexFromContainer(cp));
        }
        catch (InvalidOperationException) { /* not an items container */ }
        return 0;
    }

    private static void FadeSlide(FrameworkElement fe, double fromY, TimeSpan delay, int ms)
    {
        fe.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(ms)) { BeginTime = delay, EasingFunction = EaseOut });
        if (fe is Window || fromY == 0) return;
        var t = Translate(fe);
        t.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(fromY, 0, TimeSpan.FromMilliseconds(ms)) { BeginTime = delay, EasingFunction = EaseOut });
    }
}
