using BuildingStudio.AI.Services;
using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Model.Commands;
using BuildingStudio.Core.Rules;
using BuildingStudio.Engine.Brief;
using BuildingStudio.Engine.Compliance;
using BuildingStudio.Engine.Exterior;
using BuildingStudio.Engine.Interior;
using BuildingStudio.Engine.Layout;
using BuildingStudio.Engine.Quantity;
using BuildingStudio.Engine.Structure;
using BuildingStudio.Engine.Vastu;

namespace BuildingStudio.Studio.Pipeline;

public sealed record AreaRow(int Level, string Room, RoomType Type, double TargetSqm, double ActualSqm, double DeltaPct, Direction Zone, bool Locked);

/// <summary>Everything the UI shows next to the plan, computed from the single model.</summary>
public sealed record ModelReport(
    VastuReport Vastu, ComplianceReport Compliance, CostSummary Cost, IReadOnlyList<ElectricalRow> Electrical,
    IReadOnlyList<AreaRow> Areas, IReadOnlyList<FixSuggestion> Fixes);

public sealed record OptionCost(string Name, double Total, double PerSqft, double Vastu, double BuiltUpSqft);

/// <summary>
/// Orchestrates the engines. All derived data (walls, openings, furniture, exterior, structure) is rebuilt
/// from the room layout so the plan, 3D, structure, drawings and BOQ always agree.
/// </summary>
public sealed class StudioService
{
    private StudioService(RuleRepository rules, IAiProvider ai)
    {
        Rules = rules;
        Ai = ai;
        Vastu = new VastuEngine(rules.Vastu);
        Compliance = new ComplianceEngine(rules.Bylaws);
        Validator = new BriefValidator(rules.Bylaws);
        Layout = new LayoutGenerator(Vastu, rules.Bylaws);
        Structure = new StructureEngine();
        Interior = new InteriorEngine(rules.Furniture);
        Exterior = new ExteriorEngine(rules.Styles, rules.Vastu);
        Quantity = new QuantityEngine(rules.Rates);
        BriefAi = new BriefAssistant(ai);
        Edits = new EditCommandParser(ai);
        Reviewer = new DesignReviewer(ai);
    }

    public RuleRepository Rules { get; }
    public IAiProvider Ai { get; }
    public VastuEngine Vastu { get; }
    public ComplianceEngine Compliance { get; }
    public BriefValidator Validator { get; }
    public LayoutGenerator Layout { get; }
    public StructureEngine Structure { get; }
    public InteriorEngine Interior { get; }
    public ExteriorEngine Exterior { get; }
    public QuantityEngine Quantity { get; }
    public BriefAssistant BriefAi { get; }
    public EditCommandParser Edits { get; }
    public DesignReviewer Reviewer { get; }

    public static StudioService Create(string? rulesDirectory = null, IAiProvider? ai = null) =>
        new(RuleRepository.Load(rulesDirectory), ai ?? new NoAiProvider());

    public Core.Model.Brief Preset(int index)
    {
        var b = BriefPresets.All[index].Create();
        Validator.ApplyBylawSetbacks(b);
        return b;
    }

    public IReadOnlyList<LayoutOption> Generate(Core.Model.Brief brief, int count = 5, int seed = 1) => Layout.Generate(brief, count, seed);

    public BuildingModel CreateModel(Core.Model.Brief brief, LayoutOption option)
    {
        var m = new BuildingModel
        {
            Name = brief.ProjectName,
            Brief = ProjectSerializer.Clone(brief),
            Storeys = option.Storeys.Select(ProjectSerializer.Clone).ToList(),
        };
        m.Metadata["option"] = option.Name;
        m.Metadata["seed"] = option.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Rebuild(m);
        return m;
    }

    /// <summary>Recomputes every derived element from the rooms (call after any edit, undo or redo).</summary>
    public void Rebuild(BuildingModel m, bool furnish = true)
    {
        foreach (var s in m.Storeys)
        {
            s.Elevation = s.Level * m.Brief.FloorHeight;
            s.Height = m.Brief.FloorHeight;
            PlanDetailer.Detail(s, m.Brief);
            var grid = new ZoneGrid(s.Envelope, m.Site.NorthAngle);
            foreach (var r in s.Rooms) r.Zone = grid.ZoneOf(r.Bounds);
        }
        Interior.ApplyFinishes(m);
        if (furnish) Interior.Furnish(m);
        else foreach (var s in m.Storeys) s.Furniture.Clear();
        Exterior.Generate(m, m.Brief.Style);
        Structure.Generate(m);
    }

    public ModelReport Evaluate(BuildingModel m, int fixesForLevel = 0)
    {
        var electrical = Interior.Electrical(m);
        var areas = m.Storeys.SelectMany(s => s.Rooms.Where(r => r.Type != RoomType.Passage).Select(r => new AreaRow(
            s.Level, r.Name, r.Type, Math.Round(r.TargetArea / 1e6, 2), Math.Round(r.Area / 1e6, 2),
            r.TargetArea > 0 ? Math.Round((r.Area - r.TargetArea) / r.TargetArea * 100, 0) : 0, r.Zone, r.Locked))).ToList();
        return new ModelReport(
            Vastu.Evaluate(m), Compliance.Evaluate(m),
            Quantity.Estimate(m, new QuantityInputs(electrical.Sum(e => e.Total))),
            electrical, areas, Vastu.SuggestFixes(m, fixesForLevel));
    }

    // ---- Interactive editing (all through undoable commands) ----

    public bool Swap(CommandStack stack, int level, Guid a, Guid b)
    {
        var s = stack.Model.GetStorey(level);
        var bounds = s is null ? null : PlanEditor.Swap(s, a, b);
        if (bounds is null) return false;
        stack.Execute(new SetRoomBoundsCommand(level, bounds, $"Swap {s!.FindRoom(a)!.Name} / {s.FindRoom(b)!.Name}"));
        Rebuild(stack.Model);
        return true;
    }

    public bool MoveEdge(CommandStack stack, int level, Guid room, RectSide side, double coordinate)
    {
        var s = stack.Model.GetStorey(level);
        var bounds = s is null ? null : PlanEditor.MoveEdge(s, room, side, coordinate);
        if (bounds is null) return false;
        stack.Execute(new SetRoomBoundsCommand(level, bounds, $"Move wall of {s!.FindRoom(room)!.Name}"));
        Rebuild(stack.Model);
        return true;
    }

    public void SetLock(CommandStack stack, Guid room, bool locked)
    {
        stack.Execute(new SetRoomLockCommand(room, locked));
        Rebuild(stack.Model);
    }

    public void ApplyOption(CommandStack stack, LayoutOption option)
    {
        stack.Execute(new ReplaceStoreysCommand(option.Storeys, $"Apply {option.Name}"));
        Rebuild(stack.Model);
    }

    public void SetStyle(CommandStack stack, ExteriorStyle style)
    {
        stack.Execute(new SetExteriorStyleCommand(style));
        Rebuild(stack.Model);
    }

    public void Undo(CommandStack stack) { stack.Undo(); Rebuild(stack.Model); }
    public void Redo(CommandStack stack) { stack.Redo(); Rebuild(stack.Model); }

    /// <summary>Applies a natural-language edit. Returns a message describing what happened.</summary>
    public async Task<string> ApplyTextEditAsync(CommandStack stack, string text, int level)
    {
        var intent = await Edits.ParseAsync(text);
        if (intent is null) return "Sorry, I could not understand that edit. Try: swap kitchen and store · move pooja to NE · make living 16x14 · lock kitchen.";
        var m = stack.Model;
        var storey = m.GetStorey(level) ?? m.Storeys[0];
        var a = FindRoom(m, storey, intent.RoomA);
        if (a.Room is null) return $"No room called '{intent.RoomA}'.";
        storey = a.Storey!;
        switch (intent.Kind)
        {
            case EditKind.Lock or EditKind.Unlock:
                SetLock(stack, a.Room.Id, intent.Kind == EditKind.Lock);
                return $"{a.Room.Name} {(intent.Kind == EditKind.Lock ? "locked" : "unlocked")}.";
            case EditKind.Swap:
                var b = FindRoom(m, storey, intent.RoomB ?? "");
                if (b.Room is null || b.Storey != storey) return $"No room called '{intent.RoomB}' on {storey.Name}.";
                return Swap(stack, storey.Level, a.Room.Id, b.Room.Id) ? $"Swapped {a.Room.Name} and {b.Room.Name}." : "Swap not possible (a room is locked).";
            case EditKind.MoveToZone:
                var grid = new ZoneGrid(storey.Envelope, m.Site.NorthAngle);
                var target = storey.Rooms.Where(r => r.Id != a.Room.Id && !r.Locked && r.Type is not (RoomType.Passage or RoomType.Staircase)
                        && grid.ZoneOf(r.Bounds) == intent.Zone)
                    .OrderBy(r => Math.Abs(r.Area - a.Room.Area)).FirstOrDefault();
                if (target is null) return $"No movable room in {intent.Zone} to swap with.";
                return Swap(stack, storey.Level, a.Room.Id, target.Id) ? $"Moved {a.Room.Name} to {intent.Zone} (swapped with {target.Name})." : "Move not possible.";
            case EditKind.Resize:
                return Resize(stack, storey, a.Room, intent.WidthMm ?? a.Room.Bounds.Width, intent.DepthMm ?? a.Room.Bounds.Height);
        }
        return "Nothing changed.";
    }

    private string Resize(CommandStack stack, Storey storey, Room room, double width, double depth)
    {
        var temp = ProjectSerializer.Clone(storey);
        var all = new Dictionary<Guid, Rect2>();
        void Try(RectSide side, double coordinate)
        {
            var r = PlanEditor.MoveEdge(temp, room.Id, side, coordinate);
            if (r is null) return;
            foreach (var (id, b) in r) { all[id] = b; temp.FindRoom(id)!.Bounds = b; }
        }
        var cur = temp.FindRoom(room.Id)!.Bounds;
        Try(RectSide.Right, cur.MinX + width);
        if (!all.ContainsKey(room.Id) || !Tolerance.Near(temp.FindRoom(room.Id)!.Bounds.Width, Tolerance.Snap(width, 150), 1)) Try(RectSide.Left, temp.FindRoom(room.Id)!.Bounds.MaxX - width);
        cur = temp.FindRoom(room.Id)!.Bounds;
        Try(RectSide.Top, cur.MinY + depth);
        if (!Tolerance.Near(temp.FindRoom(room.Id)!.Bounds.Height, Tolerance.Snap(depth, 150), 1)) Try(RectSide.Bottom, temp.FindRoom(room.Id)!.Bounds.MaxY - depth);
        if (all.Count == 0) return $"{room.Name} cannot be resized that way (exterior walls or locked/too-small neighbours).";
        stack.Execute(new SetRoomBoundsCommand(storey.Level, all, $"Resize {room.Name}"));
        Rebuild(stack.Model);
        var b2 = stack.Model.GetStorey(storey.Level)!.FindRoom(room.Id)!.Bounds;
        return $"{room.Name} is now {b2.Width / 1000:0.00} x {b2.Height / 1000:0.00} m ({Units.MmToFt(b2.Width):0.#} x {Units.MmToFt(b2.Height):0.#} ft).";
    }

    private static (Room? Room, Storey? Storey) FindRoom(BuildingModel m, Storey preferred, string text)
    {
        var t = text.Trim().ToLowerInvariant().Replace("room", "").Trim();
        if (t.Length == 0) return (null, null);
        IEnumerable<Storey> order = [preferred, .. m.Storeys.Where(s => s != preferred)];
        foreach (var s in order)
        {
            var exact = s.Rooms.FirstOrDefault(r => r.Name.Equals(t, StringComparison.OrdinalIgnoreCase));
            if (exact is not null) return (exact, s);
            var partial = s.Rooms.FirstOrDefault(r => r.Name.ToLowerInvariant().Contains(t) || RoomTypes.DisplayName(r.Type).ToLowerInvariant().Contains(t));
            if (partial is not null) return (partial, s);
        }
        return (null, null);
    }

    public IReadOnlyList<LayoutOption> Resolve(BuildingModel m, int count = 3) => Layout.Resolve(m, count);

    /// <summary>Cost and Vastu for each option (option comparison table).</summary>
    public IReadOnlyList<OptionCost> CompareOptions(Core.Model.Brief brief, IReadOnlyList<LayoutOption> options) =>
        options.Select(o =>
        {
            var m = CreateModel(brief, o);
            var r = Evaluate(m);
            return new OptionCost(o.Name, Math.Round(r.Cost.Total), r.Cost.CostPerSqft, r.Vastu.Score, r.Cost.BuiltUpSqft);
        }).ToList();
}
