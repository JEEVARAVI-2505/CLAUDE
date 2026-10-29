using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;

namespace BuildingStudio.Engine.Structure;

/// <summary>
/// Preliminary RCC design for G+N residential frames: layout, gravity loads by tributary area,
/// IS 456 member sizing, footings from SBC, IS 1893 base shear and IS 875-3 wind pressure summaries, BBS.
/// Lateral load effects on members are NOT analysed; results must be verified by a structural engineer.
/// </summary>
public sealed class StructureEngine(StructureSettings? settings = null)
{
    private readonly StructureSettings _s = settings ?? new StructureSettings();

    public StructureModel Generate(BuildingModel model)
    {
        model.Structure = new StructureModel();
        if (model.Storeys.Count == 0) return model.Structure;
        var st = model.Structure;
        StructuralLayout.Build(model, _s);
        var fck = model.Brief.ConcreteFck;
        var fy = model.Brief.SteelFy;

        DesignSlabs(model, fck, fy);
        DesignBeams(model, fck, fy);
        DesignColumns(model, fck, fy);
        DesignFootings(model, fck, fy);
        LateralSummary(model);
        st.Bbs = BbsBuilder.Build(model);

        st.Notes.Add("Gravity design only: members sized for 1.5(DL+LL) by tributary area (IS 456 Table 18).");
        st.Notes.Add("Lateral (seismic/wind) forces are summarised but not distributed to members; a frame analysis is required.");
        st.Notes.Add($"Concrete M{fck:0}, steel Fe{fy:0}; clear cover {_s.ClearCover:0} mm slabs, 25 mm beams, 40 mm columns, 50 mm footings.");
        return st;
    }

    private double SlabDead(double thicknessMm, bool roof) => _s.ConcreteUnitWeight * thicknessMm / 1000 + (roof ? _s.RoofFinish : _s.FloorFinish);
    private double Live(bool roof) => roof ? _s.RoofLiveLoad : _s.FloorLiveLoad;

    private void DesignSlabs(BuildingModel model, double fck, double fy)
    {
        foreach (var level in model.Structure.Slabs.Select(p => p.Level).Distinct().ToList())
        {
            var panels = model.Structure.Slabs.Where(p => p.Level == level).ToList();
            var thickness = panels.Max(p => Is456.SlabThickness(p.Bounds.ShortSide));
            var roof = level == model.Storeys.Count;
            var q = _s.LoadFactor * (SlabDead(thickness, roof) + Live(roof));
            foreach (var p in panels)
            {
                p.Thickness = thickness;
                var lx = p.Bounds.ShortSide / 1000;
                var mu = (p.TwoWay ? Is456.AlphaXSimplySupported(p.Bounds.LongSide / p.Bounds.ShortSide) : 0.125) * q * lx * lx * 1e6;
                var d = thickness - _s.ClearCover - 5;
                var ast = Math.Max(Is456.AstRequired(mu, 1000, d, fck, fy), Is456.SlabAstMin(1000, thickness));
                var dia = ast <= 300 ? 8.0 : 10.0;
                var spacing = Math.Min(Math.Floor(1000 * Is456.BarArea(dia) / ast / 10) * 10, Math.Min(3 * d, 300));
                p.Reinforcement = $"{dia:0}φ @ {spacing:0} c/c {(p.TwoWay ? "both ways" : "main + 8φ @ 200 dist.")}";
            }
            var storey = model.GetStorey(level - 1);
            if (storey is not null) storey.SlabThickness = thickness;
        }
    }

    private double WallLoadPerMetre(double thickness, double height) => _s.BrickUnitWeight * thickness / 1000 * height / 1000;

    private void DesignBeams(BuildingModel model, double fck, double fy)
    {
        var st = model.Structure;
        foreach (var b in st.Beams)
        {
            var seg = new Segment2(b.Start, b.End);
            var l = seg.Length / 1000;
            b.B = 230;
            b.D = Math.Max(b.Level == 0 ? 300 : 380, Math.Ceiling(seg.Length / 12 / 50) * 50);
            double w;
            for (var attempt = 0; ; attempt++)
            {
                w = BeamNonSlabLoad(model, b, seg) + BeamSlabLoad(model, b, seg);
                b.Wu = _s.LoadFactor * w;
                b.Mu = b.Wu * l * l / 10;
                b.Vu = 0.6 * b.Wu * l;
                var d = b.D - 25 - 8 - 6;
                if (b.Mu * 1e6 <= Is456.MuLimit(b.B, d, fck, fy) || attempt > 8) break;
                b.D += 50;
            }
            var dEff = b.D - 25 - 8 - 6;
            var ast = Is456.AstRequired(b.Mu * 1e6, b.B, dEff, fck, fy);
            b.Ok = !double.IsNaN(ast);
            b.AstRequired = Math.Max(double.IsNaN(ast) ? 0 : ast, Is456.BeamAstMin(b.B, dEff, fy));
            var (count, dia) = Bars.Choose(b.AstRequired, [12, 16, 20], 2, 4);
            b.Bottom = $"{count}-{dia:0}φ";
            b.Top = $"{count}-{dia:0}φ";
            var pt = 100 * count * Is456.BarArea(dia) / (b.B * dEff);
            var tc = Is456.Tc(pt, fck);
            var tv = b.Vu * 1000 / (b.B * dEff);
            if (tv > Is456.TcMax(fck)) b.Ok = false;
            var sv = Math.Floor(Is456.StirrupSpacing(b.Vu * 1000, b.B, dEff, tc, fy, 2 * Is456.BarArea(8)) / 25) * 25;
            var ends = Math.Min(Math.Floor(dEff / 4 / 25) * 25, 100);
            b.Stirrups = $"2L-8φ @ {ends:0} (ends, IS 13920) / {sv:0} (mid)";
        }
        var failed = st.Beams.Count(b => !b.Ok);
        if (failed > 0) st.Warnings.Add($"{failed} beam(s) exceed section capacity at the maximum depth tried; increase width or add columns.");
    }

    /// <summary>Service UDL on a beam from self weight and the wall it carries, kN/m.</summary>
    private double BeamNonSlabLoad(BuildingModel model, Beam b, Segment2 seg)
    {
        var n = model.Storeys.Count;
        var w = _s.ConcreteUnitWeight * b.B / 1000 * b.D / 1000;
        var wallStorey = model.GetStorey(b.Level);
        if (wallStorey is not null)
        {
            var collinear = wallStorey.Walls.Where(x => StructuralLayout.Covered([x.Segment], seg.IsVertical,
                seg.IsVertical ? seg.A.X : seg.A.Y, Math.Min(Coord(seg.A, seg), Coord(seg.B, seg)), Math.Max(Coord(seg.A, seg), Coord(seg.B, seg)))
                || Overlap(x.Segment, seg) > 0).ToList();
            var thickness = collinear.Count == 0 ? 0 : collinear.Max(x => x.Thickness);
            if (thickness > 0) w += WallLoadPerMetre(thickness, wallStorey.Height - b.D);
        }
        else if (b.Level == n)
        {
            var env = model.Storeys[0].Envelope;
            if (env.Edges().Any(e => Overlap(e.Edge, seg) > 0)) w += WallLoadPerMetre(115, model.Exterior.ParapetHeight);
        }
        return w;
    }

    /// <summary>
    /// Equivalent UDL from adjacent slab panels, kN/m: triangular share q·lx/3 on short edges and trapezoidal
    /// share q·lx/6·(3 - (lx/ly)²) on long edges of two-way panels; q·lx/2 on long edges of one-way panels.
    /// </summary>
    private double BeamSlabLoad(BuildingModel model, Beam b, Segment2 seg)
    {
        var n = model.Storeys.Count;
        double w = 0;
        if (b.Level >= 1)
        {
            var roof = b.Level == n;
            foreach (var p in model.Structure.Slabs.Where(p => p.Level == b.Level))
            {
                var edge = p.Bounds.Edges().FirstOrDefault(e => Overlap(e.Edge, seg) > 1);
                if (edge.Edge.Length < 1) continue;
                var q = SlabDead(p.Thickness, roof) + Live(roof);
                var lx = p.Bounds.ShortSide / 1000;
                var ly = p.Bounds.LongSide / 1000;
                var alongLong = Math.Abs(edge.Edge.Length - p.Bounds.LongSide) < 1;
                if (!p.TwoWay) w += alongLong ? q * lx / 2 : 0;
                else w += alongLong ? q * lx / 6 * (3 - (lx / ly) * (lx / ly)) : q * lx / 3;
            }
        }
        return w;
    }

    private static double Coord(Vec2 p, Segment2 s) => s.IsVertical ? p.Y : p.X;

    private static double Overlap(Segment2 a, Segment2 b)
    {
        if (a.IsVertical && b.IsVertical && Math.Abs(a.A.X - b.A.X) < 2)
            return Math.Min(Math.Max(a.A.Y, a.B.Y), Math.Max(b.A.Y, b.B.Y)) - Math.Max(Math.Min(a.A.Y, a.B.Y), Math.Min(b.A.Y, b.B.Y));
        if (a.IsHorizontal && b.IsHorizontal && Math.Abs(a.A.Y - b.A.Y) < 2)
            return Math.Min(Math.Max(a.A.X, a.B.X), Math.Max(b.A.X, b.B.X)) - Math.Max(Math.Min(a.A.X, a.B.X), Math.Min(b.A.X, b.B.X));
        return 0;
    }

    private void DesignColumns(BuildingModel model, double fck, double fy)
    {
        var st = model.Structure;
        var minSide = model.Site.SeismicZone >= 3 ? 300 : 230;
        foreach (var c in st.Columns)
        {
            var trib = Tributary(st, c.Position, model.Storeys[0].Envelope);
            double service = 0;
            for (var level = 1; level <= model.Storeys.Count; level++)
            {
                var roof = level == model.Storeys.Count;
                var thickness = st.Slabs.Where(p => p.Level == level).Select(p => p.Thickness).DefaultIfEmpty(125).Max();
                service += (SlabDead(thickness, roof) + Live(roof)) * trib.Area / 1e6;
            }
            foreach (var b in st.Beams.Where(b => b.Start == c.Position || b.End == c.Position))
            {
                var seg = new Segment2(b.Start, b.End);
                service += BeamNonSlabLoad(model, b, seg) * seg.Length / 2000;
            }
            var height = model.RoofLevel() / 1000 + 1.5;
            c.B = minSide;
            c.D = Math.Max(minSide, 300);
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var self = _s.ConcreteUnitWeight * c.B / 1000 * c.D / 1000 * height;
                c.Pu = Math.Round(_s.LoadFactor * (service + self), 1);
                c.AscRequired = Is456.ColumnAsc(c.Pu * 1000, c.B * c.D, fck, fy);
                if (c.AscRequired <= 0.03 * c.B * c.D) break;
                if (c.D <= c.B) c.D += 75; else c.B += 75;
            }
            var (count, dia) = Bars.Choose(c.AscRequired, [12, 16, 20], 4, 12, even: true);
            c.Reinforcement = $"{count}-{dia:0}φ";
            c.Ties = $"{Math.Max(8, Is456.TieDiameter(dia)):0}φ @ {Is456.TieSpacing(Math.Min(c.B, c.D), dia):0} (100 at ends, IS 13920)";
        }
    }

    private static Rect2 Tributary(StructureModel st, Vec2 p, Rect2 env)
    {
        double Half(List<double> lines, double v, bool lower)
        {
            var neighbours = lower ? lines.Where(x => x < v - 1).ToList() : lines.Where(x => x > v + 1).ToList();
            if (neighbours.Count == 0) return 0;
            return Math.Abs(v - (lower ? neighbours.Max() : neighbours.Min())) / 2;
        }
        var colXs = st.Columns.Where(c => Tolerance.Near(c.Position.Y, p.Y, 2)).Select(c => c.Position.X).ToList();
        var colYs = st.Columns.Where(c => Tolerance.Near(c.Position.X, p.X, 2)).Select(c => c.Position.Y).ToList();
        var r = Rect2.FromMinMax(p.X - Half(colXs, p.X, true), p.Y - Half(colYs, p.Y, true), p.X + Half(colXs, p.X, false), p.Y + Half(colYs, p.Y, false));
        return env.Contains(r) ? r : Rect2.FromMinMax(Math.Max(r.MinX, env.MinX), Math.Max(r.MinY, env.MinY), Math.Min(r.MaxX, env.MaxX), Math.Min(r.MaxY, env.MaxY));
    }

    private void DesignFootings(BuildingModel model, double fck, double fy)
    {
        var sbc = model.Site.SoilSbc;
        var n = 0;
        foreach (var c in model.Structure.Columns)
        {
            var f = FootingDesign.Design(c.Pu, c.B, c.D, sbc, fck, fy);
            f.Mark = $"F{++n}";
            f.ColumnId = c.Id;
            f.Position = c.Position;
            model.Structure.Footings.Add(f);
        }
        // Overlapping footings would need combined footings.
        var fs = model.Structure.Footings;
        var overlaps = 0;
        for (var i = 0; i < fs.Count; i++)
            for (var j = i + 1; j < fs.Count; j++)
                if (new Rect2(fs[i].Position.X - fs[i].L / 2, fs[i].Position.Y - fs[i].B / 2, fs[i].L, fs[i].B)
                    .Overlaps(new Rect2(fs[j].Position.X - fs[j].L / 2, fs[j].Position.Y - fs[j].B / 2, fs[j].L, fs[j].B)))
                    overlaps++;
        if (overlaps > 0) model.Structure.Warnings.Add($"{overlaps} pair(s) of isolated footings overlap; use combined footings there.");
    }

    private void LateralSummary(BuildingModel model)
    {
        var st = model.Structure;
        var area = model.Storeys[0].Envelope.Area / 1e6;
        var weights = new List<double>();
        var heights = new List<double>();
        foreach (var s in model.Storeys)
        {
            var roof = s.Level == model.Storeys.Count - 1;
            var slab = SlabDead(s.SlabThickness, roof) + 0.25 * (roof ? 0 : _s.FloorLiveLoad);
            var walls = s.Walls.Sum(w => WallLoadPerMetre(w.Thickness, s.Height) * w.Length / 1000);
            weights.Add(slab * area + walls);
            heights.Add((s.Elevation + s.Height) / 1000);
        }
        var h = model.RoofLevel() / 1000;
        var t = Is1893.Period(h);
        var ah = Is1893.Ah(model.Site.SeismicZone, t);
        st.SeismicBaseShear = Math.Round(ah * weights.Sum(), 1);
        var q = Is1893.Distribute(st.SeismicBaseShear, weights, heights);
        st.Notes.Add($"IS 1893: zone {model.Site.SeismicZone} (Z={Is1893.ZoneFactor(model.Site.SeismicZone)}), T={t:0.000} s, Sa/g={Is1893.SaByG(t):0.00}, R=5, I=1.0, Ah={ah:0.0000}, W={weights.Sum():0} kN, Vb={st.SeismicBaseShear:0.0} kN; storey forces {string.Join(", ", q.Select(x => $"{x:0.0}"))} kN.");
        st.WindPressure = Math.Round(Is875Wind.Pressure(model.Site.BasicWindSpeed, h) / 1000, 3);
        st.Notes.Add($"IS 875-3: Vb={model.Site.BasicWindSpeed} m/s, terrain category 3, k1=k3=k4=1, pz={st.WindPressure:0.00} kN/m².");
    }
}

/// <summary>Bar selection helper.</summary>
public static class Bars
{
    /// <summary>Smallest bar diameter (and count within limits) whose area ≥ required.</summary>
    public static (int Count, double Dia) Choose(double required, double[] dias, int minCount, int maxCount, bool even = false)
    {
        foreach (var dia in dias)
        {
            var count = Math.Max(minCount, (int)Math.Ceiling(required / Is456.BarArea(dia) - 1e-9));
            if (even && count % 2 == 1) count++;
            if (count <= maxCount) return (count, dia);
        }
        var big = dias[^1];
        var c = Math.Max(minCount, (int)Math.Ceiling(required / Is456.BarArea(big)));
        return (even && c % 2 == 1 ? c + 1 : c, big);
    }
}
