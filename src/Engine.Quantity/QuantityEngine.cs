using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Rules;

namespace BuildingStudio.Engine.Quantity;

public sealed record BoqLine(string Group, string Code, string Description, string Unit, double Quantity, double Rate)
{
    public double Amount => Math.Round(Quantity * Rate, 2);
}

public sealed record CostSummary(
    IReadOnlyList<BoqLine> Lines, double Subtotal, double Contingency, double Margin, double Gst, double Total,
    double BuiltUpSqft, double CostPerSqft, string Currency, RateTable Rates)
{
    public IEnumerable<(string Group, double Amount)> ByGroup() =>
        Lines.GroupBy(l => l.Group).Select(g => (g.Key, g.Sum(l => l.Amount)));
}

/// <summary>Extra inputs from other engines (electrical points). Optional.</summary>
public sealed record QuantityInputs(int ElectricalPoints = 0);

/// <summary>
/// Quantity take-off from the model (centre-line method for walls) priced from a regional rate table.
/// Total = subtotal + contingency; contractor margin on that; GST on the lot.
/// </summary>
public sealed class QuantityEngine(RateTable rates)
{
    public RateTable Rates { get; } = rates;

    public CostSummary Estimate(BuildingModel model, QuantityInputs? inputs = null)
    {
        var q = new Dictionary<string, double>();
        void Add(string code, double v) { if (v > 0) q[code] = q.GetValueOrDefault(code) + v; }

        Structure(model, Add);
        Masonry(model, Add);
        Finishes(model, Add);
        Openings(model, Add);
        Services(model, Add, inputs);
        Interiors(model, Add);
        External(model, Add);
        Add("PAINT_INT", q.GetValueOrDefault("PLASTER_INT"));
        Add("PAINT_EXT", q.GetValueOrDefault("PLASTER_EXT"));

        var lines = q.Where(kv => Rates.Items.ContainsKey(kv.Key)).Select(kv =>
        {
            var r = Rates.Get(kv.Key);
            return new BoqLine(GroupOf(kv.Key), kv.Key, r.Description, r.Unit, Math.Round(kv.Value, 2), r.Rate);
        }).OrderBy(l => GroupOrder(l.Group)).ThenBy(l => l.Code, StringComparer.Ordinal).ToList();

        var subtotal = lines.Sum(l => l.Amount);
        var contingency = Math.Round(subtotal * Rates.ContingencyPct / 100, 2);
        var margin = Math.Round((subtotal + contingency) * Rates.ContractorMarginPct / 100, 2);
        var gst = Math.Round((subtotal + contingency + margin) * Rates.GstPct / 100, 2);
        var total = subtotal + contingency + margin + gst;
        var sqft = Units.SqMmToSqFt(model.BuiltUpArea());
        return new CostSummary(lines, subtotal, contingency, margin, gst, total, Math.Round(sqft, 1),
            sqft > 0 ? Math.Round(total / sqft, 0) : 0, Rates.Currency, Rates);
    }

    private static void Structure(BuildingModel m, Action<string, double> add)
    {
        var st = m.Structure;
        const double foundingDepth = 1.5;
        var height = m.RoofLevel() / 1000;
        foreach (var f in st.Footings)
        {
            double l = f.L / 1000, b = f.B / 1000, d = f.Depth / 1000;
            add("EXC", (l + 0.6) * (b + 0.6) * (foundingDepth + 0.1));
            add("PCC", (l + 0.2) * (b + 0.2) * 0.1);
            add("RCC_FOOTING", l * b * d);
            add("SHUTTER", 4 * l * d);
        }
        foreach (var c in st.Columns)
        {
            var h = height + foundingDepth;
            add("RCC_COLUMN", c.B / 1000 * c.D / 1000 * h);
            add("SHUTTER", 2 * (c.B + c.D) / 1000 * h);
        }
        foreach (var b in st.Beams)
        {
            var len = new Segment2(b.Start, b.End).Length / 1000;
            var slab = b.Level == 0 ? 0 : st.Slabs.Where(p => p.Level == b.Level).Select(p => p.Thickness).DefaultIfEmpty(0).Max();
            var depth = (b.D - slab) / 1000;
            add("RCC_BEAM", b.B / 1000 * depth * len);
            add("SHUTTER", (2 * depth + b.B / 1000) * len);
        }
        foreach (var p in st.Slabs)
        {
            add("RCC_SLAB", p.Bounds.Area / 1e6 * p.Thickness / 1000);
            add("SHUTTER", p.Bounds.Area / 1e6);
        }
        var steel = st.Bbs.Sum(r => r.Weight);
        add("STEEL", steel * 1.03);
    }

    /// <summary>Brick height = storey height minus the beam/slab depth over it (slab thickness when no structure).</summary>
    private static double WallHeight(BuildingModel m, Storey s)
    {
        var beam = m.Structure.Beams.Where(b => b.Level == s.Level + 1).Select(b => b.D).DefaultIfEmpty(s.SlabThickness).Average();
        return (s.Height - beam) / 1000;
    }

    private static double OpeningArea(Storey s, Wall w) =>
        s.Openings.Where(o => o.WallId == w.Id).Sum(o => o.Width / 1000 * Math.Min(o.Height, s.Height) / 1000);

    private static void Masonry(BuildingModel m, Action<string, double> add)
    {
        foreach (var s in m.Storeys)
        {
            var h = WallHeight(m, s);
            foreach (var w in s.Walls)
            {
                var gross = w.Length / 1000 * h;
                var net = Math.Max(0, gross - OpeningArea(s, w));
                if (w.Type == WallType.Exterior)
                {
                    add("BRICK230", net * w.Thickness / 1000);
                    add("PLASTER_EXT", net);
                    add("PLASTER_INT", net);
                }
                else
                {
                    add("BRICK115", net);
                    add("PLASTER_INT", 2 * net);
                }
            }
        }
        if (m.Storeys.Count > 0)
        {
            var env = m.Storeys[0].Envelope;
            var parapet = 2 * (env.Width + env.Height) / 1000 * m.Exterior.ParapetHeight / 1000;
            add("BRICK115", parapet);
            add("PLASTER_EXT", 2 * parapet);
        }
    }

    private static void Finishes(BuildingModel m, Action<string, double> add)
    {
        foreach (var s in m.Storeys)
            foreach (var r in s.Rooms)
            {
                var a = r.Area / 1e6;
                var wet = r.Type is RoomType.Bath or RoomType.Utility or RoomType.Balcony or RoomType.Parking or RoomType.Kitchen;
                add(wet ? "FLOOR_ANTISKID" : "FLOOR_VITRIFIED", a);
                add("PAINT_INT", a); // ceiling
                if (r.Type is RoomType.Bath or RoomType.Utility) add("WATERPROOF", a);
                if (r.Type == RoomType.Bath) add("TILE_WALL", 2 * (r.Bounds.Width + r.Bounds.Height) / 1000 * 2.1 - 0.75 * 2.1);
                if (r.Type == RoomType.Kitchen) add("TILE_WALL", s.Furniture.Where(f => f.RoomId == r.Id && f.CatalogId.StartsWith("kitchen-counter")).Sum(f => f.Bounds.LongSide / 1000) * 0.6);
                if (r.Type is RoomType.Living or RoomType.MasterBedroom) add("FALSE_CEILING", a);
            }
        if (m.Storeys.Count > 0) add("WATERPROOF", m.Storeys[^1].Envelope.Area / 1e6);
    }

    private static void Openings(BuildingModel m, Action<string, double> add)
    {
        foreach (var s in m.Storeys)
            foreach (var o in s.Openings)
            {
                var room = s.FindWall(o.WallId) is { } w ? s.FindRoom(w.RoomA ?? Guid.Empty) : null;
                var other = s.FindWall(o.WallId) is { RoomB: { } rb } ? s.FindRoom(rb) : null;
                switch (o.Kind)
                {
                    case OpeningKind.Door when o.IsMainEntrance: add("DOOR_MAIN", 1); break;
                    case OpeningKind.Door when room?.Type == RoomType.Bath || other?.Type == RoomType.Bath: add("DOOR_BATH", 1); break;
                    case OpeningKind.Door: add("DOOR", 1); break;
                    case OpeningKind.Window: add("WINDOW", o.Width / 1000 * o.Height / 1000); break;
                    case OpeningKind.Ventilator: add("VENT", 1); break;
                }
            }
    }

    private static void Services(BuildingModel m, Action<string, double> add, QuantityInputs? inputs)
    {
        var rooms = m.AllRooms().ToList();
        var points = inputs?.ElectricalPoints ?? rooms.Sum(r => Math.Max(2, (int)Math.Ceiling(r.Area / 1e6 / 4)));
        add("ELEC_POINT", points);
        add("PLUMB_POINT", rooms.Count(r => r.Type == RoomType.Bath) * 4 + rooms.Count(r => r.Type == RoomType.Kitchen) * 2 + rooms.Count(r => r.Type == RoomType.Utility) * 2);
    }

    private static void Interiors(BuildingModel m, Action<string, double> add)
    {
        foreach (var f in m.Storeys.SelectMany(s => s.Furniture))
        {
            if (f.CatalogId.StartsWith("wardrobe")) add("WARDROBE", f.Bounds.LongSide / 1000 * f.Height / 1000);
            if (f.CatalogId.StartsWith("kitchen-counter")) add("KITCHEN_MODULAR", f.Bounds.LongSide / 1000);
        }
    }

    private static void External(BuildingModel m, Action<string, double> add)
    {
        foreach (var i in m.Exterior.SiteItems)
        {
            switch (i.Kind)
            {
                case "Sump": add("SUMP", 1); break;
                case "Septic": add("SEPTIC", 1); break;
                case "RWH": add("RWH", 1); break;
                case "Gate": add("GATE", 1); break;
                case "CompoundWall": add("COMPOUND_WALL", i.Bounds.LongSide / 1000); break;
            }
        }
    }

    private static string GroupOf(string code) => code switch
    {
        "EXC" or "PCC" or "RCC_FOOTING" => "1 Substructure",
        "RCC_COLUMN" or "RCC_BEAM" or "RCC_SLAB" or "STEEL" or "SHUTTER" => "2 RCC frame",
        "BRICK230" or "BRICK115" => "3 Masonry",
        "PLASTER_INT" or "PLASTER_EXT" or "FLOOR_VITRIFIED" or "FLOOR_ANTISKID" or "TILE_WALL" or "WATERPROOF" or "PAINT_INT" or "PAINT_EXT" => "4 Finishes",
        "DOOR_MAIN" or "DOOR" or "DOOR_BATH" or "WINDOW" or "VENT" => "5 Doors & windows",
        "ELEC_POINT" or "PLUMB_POINT" => "6 Services",
        "FALSE_CEILING" or "WARDROBE" or "KITCHEN_MODULAR" => "7 Interiors",
        _ => "8 External works",
    };

    private static int GroupOrder(string g) => g[0] - '0';
}
