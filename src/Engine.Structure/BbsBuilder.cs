using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Structure;

/// <summary>Bar bending schedule (simplified IS 2502 shapes). Weights use d²/162 kg/m.</summary>
public static class BbsBuilder
{
    public static List<BbsRow> Build(BuildingModel model)
    {
        var rows = new List<BbsRow>();
        var height = model.RoofLevel();
        var floors = model.Storeys.Count;

        foreach (var c in model.Structure.Columns)
        {
            var (count, dia) = Parse(c.Reinforcement);
            var footing = model.Structure.Footings.FirstOrDefault(f => f.ColumnId == c.Id);
            var length = height + 1500 + (footing?.Depth ?? 300) + 300 + Math.Max(0, floors - 1) * 50 * dia;
            rows.Add(Row(c.Mark, "C-main", "Straight (00)", dia, count, length));
            var spacing = 150;
            var ties = (int)Math.Ceiling((height + 1500) / spacing) + 1;
            var tieLen = 2 * (c.B - 80 + c.D - 80) + 24 * 8;
            rows.Add(Row(c.Mark, "C-tie", "Closed link (51)", 8, ties, tieLen));
        }
        foreach (var b in model.Structure.Beams)
        {
            var len = new Segment2(b.Start, b.End).Length;
            var (count, dia) = Parse(b.Bottom);
            rows.Add(Row(b.Mark, "B-bot", "Straight with L-bends (11)", dia, count, len + 2 * 40 * dia));
            rows.Add(Row(b.Mark, "B-top", "Straight with L-bends (11)", dia, count, len + 2 * 40 * dia));
            var stirrups = (int)Math.Ceiling(len / 150) + 1;
            rows.Add(Row(b.Mark, "B-stir", "Closed link (51)", 8, stirrups, 2 * (b.B - 50 + b.D - 50) + 24 * 8));
        }
        foreach (var p in model.Structure.Slabs)
        {
            var (dia, spacing) = ParseSpacing(p.Reinforcement);
            var nx = (int)Math.Ceiling(p.Bounds.Height / spacing) + 1;
            var ny = (int)Math.Ceiling(p.Bounds.Width / (p.TwoWay ? spacing : 200)) + 1;
            rows.Add(Row(p.Mark, "S-x", "Straight (00)", dia, nx, p.Bounds.Width + 2 * 150));
            rows.Add(Row(p.Mark, "S-y", "Straight (00)", p.TwoWay ? dia : 8, ny, p.Bounds.Height + 2 * 150));
        }
        foreach (var f in model.Structure.Footings)
        {
            var (dia, spacing) = ParseSpacing(f.Reinforcement);
            var n = (int)Math.Ceiling((f.B - 100) / spacing) + 1;
            var len = f.L - 100 + 2 * (f.Depth - 100);
            rows.Add(Row(f.Mark, "F-x", "U-bar (21)", dia, n, len));
            rows.Add(Row(f.Mark, "F-y", "U-bar (21)", dia, n, len));
        }
        return rows;
    }

    private static BbsRow Row(string member, string mark, string shape, double dia, int count, double cutLength) => new()
    {
        Member = member, BarMark = mark, Shape = shape, Dia = dia, Count = count, CutLength = Math.Round(cutLength),
        Weight = Math.Round(Is456.BarWeightPerMetre(dia) * cutLength / 1000 * count, 2),
    };

    /// <summary>Parses "4-16φ".</summary>
    public static (int Count, double Dia) Parse(string s)
    {
        var parts = s.Split('-', 'φ');
        return parts.Length >= 2 && int.TryParse(parts[0], out var c) && double.TryParse(parts[1], out var d) ? (c, d) : (4, 12);
    }

    /// <summary>Parses "10φ @ 150 c/c ...".</summary>
    public static (double Dia, double Spacing) ParseSpacing(string s)
    {
        var dia = double.TryParse(s.Split('φ')[0], out var d) ? d : 10;
        var at = s.IndexOf('@');
        var rest = at >= 0 ? s[(at + 1)..].Trim().Split(' ')[0] : "150";
        return (dia, double.TryParse(rest, out var sp) ? sp : 150);
    }
}
