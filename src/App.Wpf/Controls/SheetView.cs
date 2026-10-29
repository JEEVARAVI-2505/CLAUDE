using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BuildingStudio.Core.Geometry;
using BuildingStudio.Engine.Drawing;

namespace BuildingStudio.App.Wpf.Controls;

/// <summary>Renders a drawing sheet (the same entities that go to SVG, DXF and PDF).</summary>
public sealed class SheetView : FrameworkElement
{
    public static readonly DependencyProperty SheetProperty = DependencyProperty.Register(
        nameof(Sheet), typeof(DrawingSheet), typeof(SheetView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public DrawingSheet? Sheet
    {
        get => (DrawingSheet?)GetValue(SheetProperty);
        set => SetValue(SheetProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Brushes.White, null, new Rect(RenderSize));
        if (Sheet is null || ActualWidth < 10) return;
        var ext = Sheet.Extents().Inflate(500);
        var s = Math.Min(ActualWidth / ext.Width, ActualHeight / ext.Height);
        Point P(Vec2 v) => new((v.X - ext.MinX) * s, (ext.MaxY - v.Y) * s);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        foreach (var e in Sheet.Primitives())
        {
            var color = (Color)ColorConverter.ConvertFromString(Layers.Colors.GetValueOrDefault(e.Layer, "#000000"));
            var stroke = new SolidColorBrush(color);
            switch (e)
            {
                case PolygonEntity p when p.Points.Count > 1:
                    var geo = new StreamGeometry();
                    using (var ctx = geo.Open())
                    {
                        ctx.BeginFigure(P(p.Points[0]), p.Fill is not null, p.Closed);
                        ctx.PolyLineTo(p.Points.Skip(1).Select(P).ToList(), true, false);
                    }
                    var fill = p.Fill is null ? null : new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.Fill));
                    dc.DrawGeometry(fill, new Pen(stroke, Math.Max(0.5, p.Weight * 100 * s)) { DashStyle = p.Dashed ? DashStyles.Dash : DashStyles.Solid }, geo);
                    break;
                case LineEntity l:
                    dc.DrawLine(new Pen(stroke, Math.Max(0.5, l.Weight * 100 * s)) { DashStyle = l.Dashed ? DashStyles.Dash : DashStyles.Solid }, P(l.A), P(l.B));
                    break;
                case CircleEntity c:
                    dc.DrawEllipse(c.Fill is null ? null : new SolidColorBrush((Color)ColorConverter.ConvertFromString(c.Fill)), new Pen(stroke, 1), P(c.Center), c.Radius * s, c.Radius * s);
                    break;
                case ArcEntity a:
                    var p0 = P(a.Center + new Vec2(Math.Cos(a.StartAngle), Math.Sin(a.StartAngle)) * a.Radius);
                    var p1 = P(a.Center + new Vec2(Math.Cos(a.EndAngle), Math.Sin(a.EndAngle)) * a.Radius);
                    var arc = new StreamGeometry();
                    using (var ctx = arc.Open())
                    {
                        ctx.BeginFigure(p0, false, false);
                        ctx.ArcTo(p1, new Size(a.Radius * s, a.Radius * s), 0, a.EndAngle - a.StartAngle > Math.PI, SweepDirection.Counterclockwise, true, false);
                    }
                    dc.DrawGeometry(null, new Pen(stroke, 0.8), arc);
                    break;
                case TextEntity t:
                    var size = Math.Max(4, t.Height * s * 1.1);
                    var ft = new FormattedText(t.Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("Arial"), FontStyles.Normal, t.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal), size, stroke, dpi);
                    var pt = P(t.Position);
                    var x = t.Align switch { TextAlign.Left => pt.X, TextAlign.Right => pt.X - ft.Width, _ => pt.X - ft.Width / 2 };
                    dc.DrawText(ft, new Point(x, pt.Y - ft.Baseline));
                    break;
            }
        }
    }
}
