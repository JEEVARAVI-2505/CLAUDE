using BuildingStudio.Engine.Drawing;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using DxfLayer = netDxf.Tables.Layer;

namespace BuildingStudio.IO.Dxf;

/// <summary>Writes drawing sheets to DXF (model space in millimetres, AIA layer names).</summary>
public static class DxfExporter
{
    public static DxfDocument ToDocument(DrawingSheet sheet)
    {
        var doc = new DxfDocument();
        var layers = new Dictionary<string, DxfLayer>();
        DxfLayer L(string name)
        {
            if (layers.TryGetValue(name, out var l)) return l;
            l = new DxfLayer(name) { Color = Aci(Layers.Colors.GetValueOrDefault(name, "#000000")) };
            doc.Layers.Add(l);
            return layers[name] = l;
        }

        foreach (var e in sheet.Primitives())
        {
            switch (e)
            {
                case LineEntity l:
                    doc.Entities.Add(new Line(V(l.A), V(l.B)) { Layer = L(l.Layer), Linetype = l.Dashed ? Linetype.Dashed : Linetype.ByLayer });
                    break;
                case PolygonEntity p:
                    var poly = new Polyline2D(p.Points.Select(q => new Polyline2DVertex(q.X, q.Y)), p.Closed) { Layer = L(p.Layer) };
                    doc.Entities.Add(poly);
                    break;
                case ArcEntity a:
                    doc.Entities.Add(new Arc(V(a.Center), a.Radius, a.StartAngle * 180 / Math.PI, a.EndAngle * 180 / Math.PI) { Layer = L(a.Layer) });
                    break;
                case CircleEntity c:
                    doc.Entities.Add(new Circle(V(c.Center), c.Radius) { Layer = L(c.Layer) });
                    break;
                case TextEntity t:
                    var text = new Text(t.Text, V(t.Position), t.Height) { Layer = L(t.Layer) };
                    text.Alignment = t.Align switch
                    {
                        TextAlign.Left => TextAlignment.BaselineLeft,
                        TextAlign.Right => TextAlignment.BaselineRight,
                        _ => TextAlignment.BaselineCenter,
                    };
                    doc.Entities.Add(text);
                    break;
            }
        }
        return doc;
    }

    public static void Save(DrawingSheet sheet, string path) => ToDocument(sheet).Save(path);

    private static Vector2 V(BuildingStudio.Core.Geometry.Vec2 p) => new(p.X, p.Y);

    private static AciColor Aci(string hex)
    {
        var h = hex.TrimStart('#');
        return new AciColor(Convert.ToByte(h[..2], 16), Convert.ToByte(h.Substring(2, 2), 16), Convert.ToByte(h.Substring(4, 2), 16));
    }
}
