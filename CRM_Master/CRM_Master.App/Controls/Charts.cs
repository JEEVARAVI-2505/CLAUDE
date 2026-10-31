using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CRM_Master.Core.DTOs;

namespace CRM_Master.App.Controls;

/// <summary>Dependency-free line/area chart for a time series (contact growth).</summary>
public sealed class LineChart : FrameworkElement
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(nameof(Points), typeof(IEnumerable<TimePoint>), typeof(LineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(nameof(LineBrush), typeof(Brush), typeof(LineChart), new FrameworkPropertyMetadata(Brushes.SteelBlue, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(nameof(GridBrush), typeof(Brush), typeof(LineChart), new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty TextBrushProperty = DependencyProperty.Register(nameof(TextBrush), typeof(Brush), typeof(LineChart), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<TimePoint>? Points { get => (IEnumerable<TimePoint>?)GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
    public Brush LineBrush { get => (Brush)GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }
    public Brush GridBrush { get => (Brush)GetValue(GridBrushProperty); set => SetValue(GridBrushProperty, value); }
    public Brush TextBrush { get => (Brush)GetValue(TextBrushProperty); set => SetValue(TextBrushProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var pts = Points?.ToList(); var w = ActualWidth; var h = ActualHeight;
        if (pts is null || pts.Count == 0 || w < 80 || h < 60) return;

        const double left = 44, right = 12, top = 10, bottom = 26;
        var plot = new Rect(left, top, w - left - right, h - top - bottom);
        var max = Math.Max(1, pts.Max(p => p.Value)); var niceMax = NiceCeiling(max);
        var pen = new Pen(GridBrush, 1); var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Bottom - plot.Height * i / 4.0;
            dc.DrawLine(pen, new Point(plot.Left, y), new Point(plot.Right, y));
            var label = Text((niceMax * i / 4.0).ToString("N0", CultureInfo.CurrentCulture), 10, dpi);
            dc.DrawText(label, new Point(plot.Left - 6 - label.Width, y - label.Height / 2));
        }
        double X(int i) => pts.Count == 1 ? plot.Left + plot.Width / 2 : plot.Left + plot.Width * i / (pts.Count - 1.0);
        double Y(double v) => plot.Bottom - plot.Height * v / niceMax;

        var line = new StreamGeometry();
        using (var g = line.Open())
        {
            g.BeginFigure(new Point(X(0), Y(pts[0].Value)), false, false);
            for (var i = 1; i < pts.Count; i++) g.LineTo(new Point(X(i), Y(pts[i].Value)), true, true);
        }
        line.Freeze();
        var area = new StreamGeometry();
        using (var g = area.Open())
        {
            g.BeginFigure(new Point(X(0), plot.Bottom), true, true);
            for (var i = 0; i < pts.Count; i++) g.LineTo(new Point(X(i), Y(pts[i].Value)), true, true);
            g.LineTo(new Point(X(pts.Count - 1), plot.Bottom), true, true);
        }
        area.Freeze();
        var fill = LineBrush.CloneCurrentValue(); fill.Opacity = 0.12;
        dc.DrawGeometry(fill, null, area);
        dc.DrawGeometry(null, new Pen(LineBrush, 2.2) { LineJoin = PenLineJoin.Round }, line);
        for (var i = 0; i < pts.Count; i++)
        {
            dc.DrawEllipse(LineBrush, null, new Point(X(i), Y(pts[i].Value)), 3, 3);
            if (i % 2 == 0 || pts.Count <= 6)
            {
                var t = Text(pts[i].Label, 10, dpi);
                dc.DrawText(t, new Point(X(i) - t.Width / 2, plot.Bottom + 6));
            }
        }
    }

    private FormattedText Text(string s, double size, double dpi) =>
        new(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, TextBrush, dpi);

    private static double NiceCeiling(double v)
    {
        var pow = Math.Pow(10, Math.Floor(Math.Log10(v)));
        foreach (var m in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 }) if (m * pow >= v) return m * pow;
        return 10 * pow;
    }
}

/// <summary>One bar of <see cref="BarChart"/>: <c>Kind</c> is "good", "warn", "bad" or "info".</summary>
public sealed record BarItem(string Label, double Value, string Kind);

/// <summary>Horizontal bar chart with value labels (data-quality breakdown).</summary>
public sealed class BarChart : FrameworkElement
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(BarChart), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty TextBrushProperty = DependencyProperty.Register(nameof(TextBrush), typeof(Brush), typeof(BarChart), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(BarChart), new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty GoodBrushProperty = DependencyProperty.Register(nameof(GoodBrush), typeof(Brush), typeof(BarChart), new FrameworkPropertyMetadata(Brushes.SeaGreen, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty WarnBrushProperty = DependencyProperty.Register(nameof(WarnBrush), typeof(Brush), typeof(BarChart), new FrameworkPropertyMetadata(Brushes.Goldenrod, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty BadBrushProperty = DependencyProperty.Register(nameof(BadBrush), typeof(Brush), typeof(BarChart), new FrameworkPropertyMetadata(Brushes.IndianRed, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty InfoBrushProperty = DependencyProperty.Register(nameof(InfoBrush), typeof(Brush), typeof(BarChart), new FrameworkPropertyMetadata(Brushes.SteelBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable? Items { get => (IEnumerable?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public Brush TextBrush { get => (Brush)GetValue(TextBrushProperty); set => SetValue(TextBrushProperty, value); }
    public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public Brush GoodBrush { get => (Brush)GetValue(GoodBrushProperty); set => SetValue(GoodBrushProperty, value); }
    public Brush WarnBrush { get => (Brush)GetValue(WarnBrushProperty); set => SetValue(WarnBrushProperty, value); }
    public Brush BadBrush { get => (Brush)GetValue(BadBrushProperty); set => SetValue(BadBrushProperty, value); }
    public Brush InfoBrush { get => (Brush)GetValue(InfoBrushProperty); set => SetValue(InfoBrushProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var n = Items?.Cast<object>().Count() ?? 0;
        return new Size(double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width, Math.Max(40, n * 34));
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var items = Items?.OfType<BarItem>().ToList();
        if (items is null || items.Count == 0) return;
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var max = Math.Max(1, items.Max(i => i.Value));
        const double labelW = 130, valueW = 56, barH = 12, rowH = 34;
        var barW = Math.Max(10, ActualWidth - labelW - valueW);

        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i]; var y = i * rowH;
            var label = Fmt(it.Label, 12, TextBrush, dpi); label.MaxTextWidth = labelW - 8; label.MaxLineCount = 1; label.Trimming = TextTrimming.CharacterEllipsis;
            dc.DrawText(label, new Point(0, y + (rowH - label.Height) / 2));
            var track = new Rect(labelW, y + (rowH - barH) / 2, barW, barH);
            dc.DrawRoundedRectangle(TrackBrush, null, track, 6, 6);
            var fillW = it.Value <= 0 ? 0 : Math.Max(barH, barW * it.Value / max);
            if (it.Value > 0)
                dc.DrawRoundedRectangle(it.Kind switch { "good" => GoodBrush, "warn" => WarnBrush, "bad" => BadBrush, _ => InfoBrush }, null, new Rect(track.Left, track.Top, fillW, barH), 6, 6);
            var val = Fmt(it.Value.ToString("N0", CultureInfo.CurrentCulture), 12, TextBrush, dpi);
            dc.DrawText(val, new Point(labelW + barW + 10, y + (rowH - val.Height) / 2));
        }
    }

    private static FormattedText Fmt(string s, double size, Brush b, double dpi) =>
        new(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, b, dpi);
}
