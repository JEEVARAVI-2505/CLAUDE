using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Structure;

/// <summary>
/// Column grid from ground-floor wall lines: columns only where grid lines cross walls (never mid-room),
/// beams along walls between consecutive columns, slab panels between beam lines.
/// </summary>
public static class StructuralLayout
{
    public static List<double> GridLines(IEnumerable<double> coords, double min, double max, double minGap, double maxSpan)
    {
        var sorted = coords.Append(min).Append(max).Where(c => c >= min - 1 && c <= max + 1).Distinct().OrderBy(c => c).ToList();
        var lines = new List<double>();
        foreach (var c in sorted)
        {
            if (lines.Count > 0 && c - lines[^1] < minGap)
            {
                if (Tolerance.Near(c, max, 1)) lines[^1] = c;
                continue;
            }
            lines.Add(c);
        }
        var result = new List<double>();
        for (var i = 0; i < lines.Count; i++)
        {
            result.Add(lines[i]);
            if (i + 1 >= lines.Count) continue;
            var gap = lines[i + 1] - lines[i];
            var n = (int)Math.Ceiling(gap / maxSpan - 1e-9);
            for (var k = 1; k < n; k++) result.Add(lines[i] + gap * k / n);
        }
        return result;
    }

    /// <summary>True when collinear segments cover [a, b] along a line (vertical when <paramref name="vertical"/>).</summary>
    public static bool Covered(IEnumerable<Segment2> segments, bool vertical, double line, double a, double b, double tol = 2)
    {
        var intervals = segments
            .Where(s => vertical ? s.IsVertical && Math.Abs(s.A.X - line) <= tol : s.IsHorizontal && Math.Abs(s.A.Y - line) <= tol)
            .Select(s => vertical ? (Math.Min(s.A.Y, s.B.Y), Math.Max(s.A.Y, s.B.Y)) : (Math.Min(s.A.X, s.B.X), Math.Max(s.A.X, s.B.X)))
            .OrderBy(i => i.Item1).ToList();
        var reach = a;
        foreach (var (lo, hi) in intervals)
        {
            if (lo > reach + tol) break;
            reach = Math.Max(reach, hi);
            if (reach >= b - tol) return true;
        }
        return reach >= b - tol;
    }

    public static void Build(BuildingModel model, StructureSettings settings)
    {
        var st = model.Structure;
        var ground = model.Storeys[0];
        var env = ground.Envelope;
        st.GridX = GridLines(ground.Rooms.SelectMany(r => new[] { r.Bounds.MinX, r.Bounds.MaxX }), env.MinX, env.MaxX, settings.MinGridGap, settings.MaxSpan);
        st.GridY = GridLines(ground.Rooms.SelectMany(r => new[] { r.Bounds.MinY, r.Bounds.MaxY }), env.MinY, env.MaxY, settings.MinGridGap, settings.MaxSpan);

        var walls = ground.Walls.Select(w => w.Segment).ToList();
        var perimeter = env.Edges().Select(e => e.Edge).ToList();
        var support = walls.Concat(perimeter).ToList();

        var n = 0;
        foreach (var y in st.GridY)
            foreach (var x in st.GridX)
            {
                var p = new Vec2(x, y);
                if (!support.Any(s => s.ContainsPoint(p, 2))) continue;
                st.Columns.Add(new Column { Mark = $"C{++n}", Position = p });
            }

        var beamCount = new Dictionary<int, int>();
        void AddBeams(bool vertical)
        {
            foreach (var line in vertical ? st.GridX : st.GridY)
            {
                var onLine = st.Columns.Where(c => Tolerance.Near(vertical ? c.Position.X : c.Position.Y, line, 2))
                    .OrderBy(c => vertical ? c.Position.Y : c.Position.X).ToList();
                for (var i = 0; i + 1 < onLine.Count; i++)
                {
                    var a = vertical ? onLine[i].Position.Y : onLine[i].Position.X;
                    var b = vertical ? onLine[i + 1].Position.Y : onLine[i + 1].Position.X;
                    if (!Covered(support, vertical, line, a, b)) continue;
                    for (var level = 0; level <= model.Storeys.Count; level++)
                    {
                        beamCount[level] = beamCount.GetValueOrDefault(level) + 1;
                        var prefix = level == 0 ? "PB" : level == model.Storeys.Count ? "RB" : $"B{level}";
                        st.Beams.Add(new Beam { Mark = $"{prefix}-{beamCount[level]}", Level = level, Start = onLine[i].Position, End = onLine[i + 1].Position });
                    }
                }
            }
        }
        AddBeams(true);
        AddBeams(false);

        // Slab panels between beam-carrying grid lines.
        var bx = st.GridX.Where(x => st.Beams.Any(b => b.Level == 1 && Tolerance.Near(b.Start.X, x, 2) && Tolerance.Near(b.End.X, x, 2))).ToList();
        var by = st.GridY.Where(y => st.Beams.Any(b => b.Level == 1 && Tolerance.Near(b.Start.Y, y, 2) && Tolerance.Near(b.End.Y, y, 2))).ToList();
        if (!bx.Contains(env.MinX)) bx.Insert(0, env.MinX);
        if (!bx.Contains(env.MaxX)) bx.Add(env.MaxX);
        if (!by.Contains(env.MinY)) by.Insert(0, env.MinY);
        if (!by.Contains(env.MaxY)) by.Add(env.MaxY);
        bx = bx.Distinct().OrderBy(v => v).ToList();
        by = by.Distinct().OrderBy(v => v).ToList();
        var stairs = model.Storeys.SelectMany(s => s.Rooms).Where(r => r.Type == RoomType.Staircase).Select(r => r.Bounds).ToList();
        for (var level = 1; level <= model.Storeys.Count; level++)
        {
            var k = 0;
            for (var i = 0; i + 1 < bx.Count; i++)
                for (var j = 0; j + 1 < by.Count; j++)
                {
                    var cell = Rect2.FromMinMax(bx[i], by[j], bx[i + 1], by[j + 1]);
                    // The stair well is open below the roof.
                    if (level < model.Storeys.Count && stairs.Any(sr => sr.Contains(cell.Center))) continue;
                    var ratio = cell.LongSide / cell.ShortSide;
                    st.Slabs.Add(new SlabPanel { Mark = $"S{level}-{++k}", Level = level, Bounds = cell, TwoWay = ratio <= 2 });
                }
        }

        foreach (var s in model.Storeys.Skip(1))
        {
            var beams = st.Beams.Where(b => b.Level == s.Level).Select(b => new Segment2(b.Start, b.End)).ToList();
            var unsupported = s.Walls.Count(w => !Covered(beams, w.Segment.IsVertical, w.Segment.IsVertical ? w.Start.X : w.Start.Y,
                w.Segment.IsVertical ? Math.Min(w.Start.Y, w.End.Y) : Math.Min(w.Start.X, w.End.X),
                w.Segment.IsVertical ? Math.Max(w.Start.Y, w.End.Y) : Math.Max(w.Start.X, w.End.X)));
            if (unsupported > 0)
                st.Warnings.Add($"{s.Name}: {unsupported} wall segment(s) rest on slab, not on a beam. Use lightweight partitions or add beams.");
        }
    }
}
