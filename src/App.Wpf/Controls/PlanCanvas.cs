using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Engine.Drawing;
using BuildingStudio.Engine.Vastu;

namespace BuildingStudio.App.Wpf.Controls;

/// <summary>
/// Interactive 2D plan editor. Drag a room onto another to swap; drag an interior wall to move it (neighbours
/// adjust); click to select. All edits are raised as requests and executed as undoable commands by the view model.
/// </summary>
public sealed class PlanCanvas : FrameworkElement
{
    private const double Pad = 40;
    private const double EdgeGrab = 7;
    private Storey? _storey;
    private BuildingModel? _model;
    private Guid? _selected;
    private bool _zones = true;
    private bool _furniture = true;
    private double _scale = 0.05;
    private Rect2 _env;

    private enum DragKind { None, Body, Edge }
    private DragKind _drag;
    private Room? _dragRoom;
    private RectSide _dragSide;
    private Point _down;
    private Point _current;

    public Action<Guid, Guid>? SwapRequested { get; set; }
    public Action<Guid, RectSide, double>? EdgeMoveRequested { get; set; }
    public Action<Guid?>? RoomSelected { get; set; }

    public PlanCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    public void SetData(BuildingModel? model, int level, Guid? selected, bool zones, bool furniture)
    {
        _model = model;
        _storey = model?.GetStorey(level);
        _selected = selected;
        _zones = zones;
        _furniture = furniture;
        InvalidateVisual();
    }

    private Point ToScreen(Vec2 p) => new(Pad + (p.X - _env.MinX) * _scale, Pad + (_env.MaxY - p.Y) * _scale);
    private Vec2 ToPlan(Point p) => new(_env.MinX + (p.X - Pad) / _scale, _env.MaxY - (p.Y - Pad) / _scale);

    private Rect ToScreen(Rect2 r)
    {
        var a = ToScreen(new Vec2(r.MinX, r.MaxY));
        return new Rect(a.X, a.Y, r.Width * _scale, r.Height * _scale);
    }

    private static Brush Brush(string hex, byte alpha = 255)
    {
        var c = (Color)ColorConverter.ConvertFromString(hex);
        c.A = alpha;
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Brushes.White, null, new Rect(RenderSize));
        if (_storey is null || _model is null)
        {
            dc.DrawText(Text("Generate or open a plan to start editing.", 14, Brushes.Gray), new Point(20, 20));
            return;
        }
        _env = _storey.Envelope;
        _scale = Math.Max(0.001, Math.Min((ActualWidth - 2 * Pad) / _env.Width, (ActualHeight - 2 * Pad) / _env.Height));

        foreach (var r in _storey.Rooms)
        {
            var sel = r.Id == _selected;
            dc.DrawRectangle(Brush(RoomColors.For(r.Type)), new Pen(sel ? Brushes.DodgerBlue : Brushes.Gray, sel ? 3 : 0.5), ToScreen(r.Bounds));
        }
        if (_zones) DrawZones(dc);
        if (_furniture)
            foreach (var f in _storey.Furniture)
                dc.DrawRectangle(Brush(f.Color, 90), new Pen(Brush("#8d6e63"), 0.8), ToScreen(f.Bounds));

        var wallBrush = Brush("#2c2c2c");
        foreach (var w in _storey.Walls)
            dc.DrawLine(new Pen(wallBrush, Math.Max(2, w.Thickness * _scale)) { StartLineCap = PenLineCap.Square, EndLineCap = PenLineCap.Square },
                ToScreen(w.Start), ToScreen(w.End));
        foreach (var o in _storey.Openings)
        {
            var w = _storey.FindWall(o.WallId);
            if (w is null) continue;
            var a = ToScreen(w.Segment.PointAtDistance(o.Offset));
            var b = ToScreen(w.Segment.PointAtDistance(o.Offset + o.Width));
            var t = Math.Max(2, w.Thickness * _scale) + 1;
            dc.DrawLine(new Pen(Brushes.White, t), a, b);
            var pen = o.Kind switch
            {
                OpeningKind.Window or OpeningKind.Ventilator => new Pen(Brush("#2471a3"), 2.5),
                OpeningKind.Door => new Pen(o.IsMainEntrance ? Brushes.Firebrick : Brush("#8e44ad"), o.IsMainEntrance ? 3 : 1.5),
                _ => new Pen(Brushes.LightGray, 1) { DashStyle = DashStyles.Dash },
            };
            dc.DrawLine(pen, a, b);
        }
        foreach (var c in _model.Structure.Columns)
            dc.DrawRectangle(Brushes.Firebrick, null, ToScreen(new Rect2(c.Position.X - c.B / 2, c.Position.Y - c.D / 2, c.B, c.D)));

        foreach (var r in _storey.Rooms)
        {
            var rect = ToScreen(r.Bounds);
            var size = Math.Clamp(rect.Width / 9, 8, 13);
            var title = Text((r.Locked ? "🔒 " : "") + r.Name, size, Brushes.Black, true);
            var dims = Text($"{r.Bounds.Width / 1000:0.00}×{r.Bounds.Height / 1000:0.00} m · {r.Area / 1e6:0.0} m²", size * 0.8, Brushes.DimGray);
            if (title.Width > rect.Width - 4 && rect.Height > rect.Width) continue;
            dc.DrawText(title, new Point(rect.X + (rect.Width - title.Width) / 2, rect.Y + rect.Height / 2 - title.Height));
            if (dims.Width < rect.Width - 4 && r.Type != RoomType.Passage)
                dc.DrawText(dims, new Point(rect.X + (rect.Width - dims.Width) / 2, rect.Y + rect.Height / 2 + 1));
        }
        DrawNorth(dc);
        DrawDragFeedback(dc);
    }

    private void DrawZones(DrawingContext dc)
    {
        var grid = new ZoneGrid(_env, _model!.Site.NorthAngle);
        var pen = new Pen(Brush("#c0392b", 120), 1) { DashStyle = DashStyles.Dash };
        for (var i = 1; i < 3; i++)
        {
            var x = _env.MinX + _env.Width * i / 3;
            var y = _env.MinY + _env.Height * i / 3;
            dc.DrawLine(pen, ToScreen(new Vec2(x, _env.MinY)), ToScreen(new Vec2(x, _env.MaxY)));
            dc.DrawLine(pen, ToScreen(new Vec2(_env.MinX, y)), ToScreen(new Vec2(_env.MaxX, y)));
        }
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
            {
                var p = new Vec2(_env.MinX + _env.Width * (i + 0.5) / 3, _env.MinY + _env.Height * (j + 0.5) / 3);
                var label = Text(grid.ZoneOf(p).ToString(), 11, Brush("#c0392b", 150), true);
                var s = ToScreen(new Vec2(_env.MinX + _env.Width * i / 3, _env.MinY + _env.Height * (j + 1) / 3));
                dc.DrawText(label, new Point(s.X + 3, s.Y + 2));
            }
    }

    private void DrawNorth(DrawingContext dc)
    {
        var c = new Point(ActualWidth - 30, 30);
        var up = new Vec2(0, 1).Rotate(_model!.Site.NorthAngle);
        var tip = new Point(c.X + up.X * 18, c.Y - up.Y * 18);
        dc.DrawEllipse(null, new Pen(Brushes.Black, 1), c, 20, 20);
        dc.DrawLine(new Pen(Brushes.Black, 3), c, tip);
        dc.DrawText(Text("N", 12, Brushes.Black, true), new Point(tip.X - 4, tip.Y - 18));
    }

    private void DrawDragFeedback(DrawingContext dc)
    {
        if (_drag == DragKind.Body && _dragRoom is not null)
        {
            var r = ToScreen(_dragRoom.Bounds);
            r.Offset(_current.X - _down.X, _current.Y - _down.Y);
            dc.DrawRectangle(Brush("#3498db", 60), new Pen(Brushes.DodgerBlue, 2) { DashStyle = DashStyles.Dash }, r);
        }
        else if (_drag == DragKind.Edge && _dragRoom is not null)
        {
            var vertical = _dragSide is RectSide.Left or RectSide.Right;
            var pen = new Pen(Brushes.OrangeRed, 3) { DashStyle = DashStyles.Dash };
            var p = ToPlan(_current);
            var b = _dragRoom.Bounds;
            if (vertical) dc.DrawLine(pen, ToScreen(new Vec2(p.X, b.MinY)), ToScreen(new Vec2(p.X, b.MaxY)));
            else dc.DrawLine(pen, ToScreen(new Vec2(b.MinX, p.Y)), ToScreen(new Vec2(b.MaxX, p.Y)));
            var len = vertical ? Math.Abs(p.X - b.SideCoordinate(_dragSide == RectSide.Left ? RectSide.Right : RectSide.Left))
                               : Math.Abs(p.Y - b.SideCoordinate(_dragSide == RectSide.Bottom ? RectSide.Top : RectSide.Bottom));
            dc.DrawText(Text($"{len / 1000:0.00} m", 12, Brushes.OrangeRed, true), new Point(_current.X + 10, _current.Y + 10));
        }
    }

    private Room? RoomAt(Point p)
    {
        var plan = ToPlan(p);
        return _storey?.Rooms.FirstOrDefault(r => r.Bounds.Contains(plan, 0));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (_storey is null) return;
        Focus();
        _down = _current = e.GetPosition(this);
        var room = RoomAt(_down);
        if (room is null) return;
        var r = ToScreen(room.Bounds);
        var edges = new (RectSide Side, double Dist)[]
        {
            (RectSide.Left, Math.Abs(_down.X - r.Left)), (RectSide.Right, Math.Abs(_down.X - r.Right)),
            (RectSide.Top, Math.Abs(_down.Y - r.Top)), (RectSide.Bottom, Math.Abs(_down.Y - r.Bottom)),
        };
        var nearest = edges.OrderBy(x => x.Dist).First();
        _dragRoom = room;
        if (nearest.Dist <= EdgeGrab)
        {
            _drag = DragKind.Edge;
            _dragSide = nearest.Side;
        }
        else _drag = DragKind.Body;
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var p = e.GetPosition(this);
        if (_drag != DragKind.None)
        {
            _current = p;
            InvalidateVisual();
            return;
        }
        var room = RoomAt(p);
        if (room is null) { Cursor = Cursors.Arrow; return; }
        var r = ToScreen(room.Bounds);
        var nearV = Math.Min(Math.Abs(p.X - r.Left), Math.Abs(p.X - r.Right)) <= EdgeGrab;
        var nearH = Math.Min(Math.Abs(p.Y - r.Top), Math.Abs(p.Y - r.Bottom)) <= EdgeGrab;
        Cursor = nearV ? Cursors.SizeWE : nearH ? Cursors.SizeNS : Cursors.SizeAll;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
        var up = e.GetPosition(this);
        var moved = (up - _down).Length > 4;
        var kind = _drag;
        var room = _dragRoom;
        _drag = DragKind.None;
        _dragRoom = null;
        InvalidateVisual();
        if (room is null) { RoomSelected?.Invoke(null); return; }
        if (!moved) { RoomSelected?.Invoke(room.Id); return; }
        if (kind == DragKind.Edge)
        {
            var p = ToPlan(up);
            EdgeMoveRequested?.Invoke(room.Id, _dragSide, _dragSide is RectSide.Left or RectSide.Right ? p.X : p.Y);
        }
        else if (RoomAt(up) is { } target && target.Id != room.Id)
            SwapRequested?.Invoke(room.Id, target.Id);
    }

    private FormattedText Text(string s, double size, Brush brush, bool bold = false) =>
        new(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal),
            size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
}
