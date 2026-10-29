using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Drawing;

/// <summary>Column and footing layout with grid bubbles and schedules.</summary>
public static class StructuralDrawer
{
    public const string Banner = "PRELIMINARY - MUST BE REVIEWED AND CERTIFIED BY A LICENSED STRUCTURAL ENGINEER";

    public static DrawingSheet Draw(BuildingModel model)
    {
        var sheet = new DrawingSheet { Name = "S-101", Title = "COLUMN & FOOTING LAYOUT", Scale = 100 };
        var e = sheet.Entities;
        var st = model.Structure;
        if (model.Storeys.Count == 0) return sheet;
        var env = model.Storeys[0].Envelope;

        for (var i = 0; i < st.GridX.Count; i++)
        {
            var x = st.GridX[i];
            e.Add(new LineEntity(Layers.Grid, new Vec2(x, env.MinY - 1500), new Vec2(x, env.MaxY + 1500), 0.13, true));
            e.Add(new CircleEntity(Layers.Grid, new Vec2(x, env.MaxY + 1900), 350));
            e.Add(new TextEntity(Layers.Grid, new Vec2(x, env.MaxY + 1820), (i + 1).ToString(), 250, TextAlign.Center, true));
        }
        for (var i = 0; i < st.GridY.Count; i++)
        {
            var y = st.GridY[i];
            e.Add(new LineEntity(Layers.Grid, new Vec2(env.MinX - 1500, y), new Vec2(env.MaxX + 1500, y), 0.13, true));
            e.Add(new CircleEntity(Layers.Grid, new Vec2(env.MinX - 1900, y), 350));
            e.Add(new TextEntity(Layers.Grid, new Vec2(env.MinX - 1900, y - 80), GridLetter(i), 250, TextAlign.Center, true));
        }
        foreach (var b in st.Beams.Where(b => b.Level == 1))
            e.Add(new LineEntity(Layers.Beam, b.Start, b.End, 0.25, true));
        foreach (var f in st.Footings)
            e.Add(PolygonEntity.Rect(Layers.Footing, new Rect2(f.Position.X - f.L / 2, f.Position.Y - f.B / 2, f.L, f.B), null, 0.18, true));
        foreach (var c in st.Columns)
        {
            e.Add(PolygonEntity.Rect(Layers.Column, new Rect2(c.Position.X - c.B / 2, c.Position.Y - c.D / 2, c.B, c.D), "#c0392b", 0.25));
            e.Add(new TextEntity(Layers.Column, c.Position + new Vec2(350, 250), c.Mark, 150, TextAlign.Left));
        }

        var y0 = env.MinY - 3000;
        e.Add(new TextEntity(Layers.Title, new Vec2(env.MinX, y0), Banner, 220, TextAlign.Left, true));
        var colRows = st.Columns.GroupBy(c => (c.B, c.D, c.Reinforcement, c.Ties))
            .Select(g => (IReadOnlyList<string>)[Marks(g.Select(c => c.Mark)), $"{g.Key.B:0}x{g.Key.D:0}", g.Key.Reinforcement, g.Key.Ties, $"{g.Max(c => c.Pu):0}"])
            .ToList();
        var table = new TableEntity(Layers.Text, new Vec2(env.MinX, y0 - 600), ["Columns", "Size (mm)", "Main bars", "Ties", "Max Pu (kN)"], colRows, [5200, 1800, 2200, 2400, 1800], 170);
        e.Add(table);
        var ftRows = st.Footings.GroupBy(f => (f.L, f.B, f.Depth, f.Reinforcement))
            .Select(g => (IReadOnlyList<string>)[Marks(g.Select(f => f.Mark)), $"{g.Key.L:0}x{g.Key.B:0}", $"{g.Key.Depth:0}", g.Key.Reinforcement])
            .ToList();
        e.Add(new TableEntity(Layers.Text, new Vec2(env.MinX, y0 - 900 - table.Height), ["Footings", "L x B (mm)", "Depth", "Bottom mesh"], ftRows, [5200, 1800, 1200, 3000], 170));
        sheet.Notes.Add(Banner);
        sheet.Notes.AddRange(st.Notes);
        return sheet;
    }

    /// <summary>Compact mark list for schedules: "C1,C2,C3 … (+12)".</summary>
    public static string Marks(IEnumerable<string> marks)
    {
        var list = marks.ToList();
        return list.Count <= 5 ? string.Join(",", list) : $"{string.Join(",", list.Take(4))} … (+{list.Count - 4})";
    }

    public static string GridLetter(int i) => i < 26 ? ((char)('A' + i)).ToString() : $"A{i - 25}";
}
