using System.Globalization;
using System.Net;
using System.Text;
using BuildingStudio.Core.Geometry;

namespace BuildingStudio.Engine.Drawing;

/// <summary>Writes a sheet as SVG (model millimetres in the viewBox, Y flipped).</summary>
public static class SvgWriter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <param name="pixelWidth">Rendered width attribute in pixels.</param>
    public static string Write(DrawingSheet sheet, double pixelWidth = 1200, bool background = true)
    {
        var ext = sheet.Extents().Inflate(800);
        double Y(double y) => ext.MaxY - y;
        string F(double v) => v.ToString("0.#", Inv);
        var pixelHeight = pixelWidth * ext.Height / ext.Width;
        var sb = new StringBuilder();
        sb.Append(Inv, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {F(ext.Width)} {F(ext.Height)}\" width=\"{F(pixelWidth)}\" height=\"{F(pixelHeight)}\" font-family=\"Arial, Helvetica, sans-serif\">\n");
        sb.Append($"<title>{WebUtility.HtmlEncode(sheet.Title)}</title>\n");
        if (background) sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>\n");
        sb.Append(Inv, $"<g transform=\"translate({F(-ext.MinX)},0)\">\n");

        foreach (var e in sheet.Primitives())
        {
            var color = Layers.Colors.GetValueOrDefault(e.Layer, "#000000");
            switch (e)
            {
                case PolygonEntity p:
                    var pts = string.Join(" ", p.Points.Select(q => $"{F(q.X)},{F(Y(q.Y))}"));
                    var tag = p.Closed ? "polygon" : "polyline";
                    sb.Append($"<{tag} points=\"{pts}\" fill=\"{p.Fill ?? "none"}\" stroke=\"{color}\" stroke-width=\"{F(p.Weight * 100)}\"{Dash(p.Dashed)}/>\n");
                    break;
                case LineEntity l:
                    sb.Append($"<line x1=\"{F(l.A.X)}\" y1=\"{F(Y(l.A.Y))}\" x2=\"{F(l.B.X)}\" y2=\"{F(Y(l.B.Y))}\" stroke=\"{color}\" stroke-width=\"{F(l.Weight * 100)}\"{Dash(l.Dashed)}/>\n");
                    break;
                case ArcEntity a:
                    var p0 = a.Center + new Vec2(Math.Cos(a.StartAngle), Math.Sin(a.StartAngle)) * a.Radius;
                    var p1 = a.Center + new Vec2(Math.Cos(a.EndAngle), Math.Sin(a.EndAngle)) * a.Radius;
                    var large = a.EndAngle - a.StartAngle > Math.PI ? 1 : 0;
                    sb.Append($"<path d=\"M {F(p0.X)} {F(Y(p0.Y))} A {F(a.Radius)} {F(a.Radius)} 0 {large} 0 {F(p1.X)} {F(Y(p1.Y))}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{F(a.Weight * 100)}\"/>\n");
                    break;
                case CircleEntity c:
                    sb.Append($"<circle cx=\"{F(c.Center.X)}\" cy=\"{F(Y(c.Center.Y))}\" r=\"{F(c.Radius)}\" fill=\"{c.Fill ?? "none"}\" stroke=\"{color}\" stroke-width=\"25\"/>\n");
                    break;
                case TextEntity t:
                    var anchor = t.Align switch { TextAlign.Left => "start", TextAlign.Right => "end", _ => "middle" };
                    sb.Append($"<text x=\"{F(t.Position.X)}\" y=\"{F(Y(t.Position.Y))}\" font-size=\"{F(t.Height)}\" text-anchor=\"{anchor}\" fill=\"{color}\"{(t.Bold ? " font-weight=\"bold\"" : "")}>{WebUtility.HtmlEncode(t.Text)}</text>\n");
                    break;
            }
        }
        sb.Append("</g>\n</svg>\n");
        return sb.ToString();
    }

    private static string Dash(bool dashed) => dashed ? " stroke-dasharray=\"120,80\"" : "";
}
