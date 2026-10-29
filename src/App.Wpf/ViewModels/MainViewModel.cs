using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using BuildingStudio.Core.Geometry;
using BuildingStudio.Core.Model;
using BuildingStudio.Core.Model.Commands;
using BuildingStudio.Engine.Brief;
using BuildingStudio.Engine.Compliance;
using BuildingStudio.Engine.Drawing;
using BuildingStudio.Engine.Layout;
using BuildingStudio.Engine.Vastu;
using BuildingStudio.Studio.Pipeline;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BuildingStudio.App.Wpf.ViewModels;

public sealed record OptionViewModel(LayoutOption Option)
{
    public string Title => $"{Option.Name}  ·  fitness {Option.Metrics.Fitness:0.0}";
    public string Summary => Option.Summary;
    public string TradeOffs => Option.TradeOffs.Count == 0 ? "No trade-offs." : string.Join(Environment.NewLine, Option.TradeOffs.Take(6));
}

/// <summary>Shell view model: brief → options → edit → evaluate → export, all against one model with undo/redo.</summary>
public partial class MainViewModel(StudioService studio) : ObservableObject
{
    public StudioService Studio { get; } = studio;
    public CommandStack? Stack { get; private set; }
    public BuildingModel? Model => Stack?.Model;

    public BriefViewModel Brief { get; } = new();
    public ObservableCollection<string> BriefNotes { get; } = [];
    public ObservableCollection<BriefIssue> Issues { get; } = [];
    public ObservableCollection<OptionViewModel> Options { get; } = [];
    public ObservableCollection<OptionCost> OptionCosts { get; } = [];
    public ObservableCollection<int> Levels { get; } = [];
    public ObservableCollection<DrawingSheet> Sheets { get; } = [];
    public IReadOnlyList<string> Presets { get; } = BriefPresets.All.Select(p => p.Name).ToList();
    public static Array StyleValues => Enum.GetValues<ExteriorStyle>();

    [ObservableProperty] private string briefText = BriefPresets.VisionText;
    [ObservableProperty] private string status = "Type a brief or pick a preset, then Parse → confirm the form → Generate.";
    [ObservableProperty] private OptionViewModel? selectedOption;
    [ObservableProperty] private int currentLevel;
    [ObservableProperty] private ModelReport? report;
    [ObservableProperty] private Room? selectedRoom;
    [ObservableProperty] private string selectedRoomExplanation = "Click a room to see why it is where it is.";
    [ObservableProperty] private string editText = "swap kitchen and dining";
    [ObservableProperty] private ExteriorStyle style = ExteriorStyle.Contemporary;
    [ObservableProperty] private bool showFurniture = true;
    [ObservableProperty] private bool showStructure;
    [ObservableProperty] private bool floorCut;
    [ObservableProperty] private bool showZones = true;
    [ObservableProperty] private string reviewNotes = "";
    [ObservableProperty] private DrawingSheet? selectedSheet;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private int selectedPreset = 4;

    /// <summary>Raised after every model change so the plan canvas and 3D view redraw.</summary>
    public event EventHandler? ModelChanged;

    public string StructureBanner => StructuralDrawer.Banner;
    public string CostText => Report is null ? "" :
        $"{Report.Cost.Currency} {ReportComposer.Money(Report.Cost.Total)}  ·  {ReportComposer.Money(Report.Cost.CostPerSqft)}/sq ft  ·  {Report.Cost.BuiltUpSqft:0} sq ft";
    public string ComplianceText => Report is null ? "" :
        $"{Report.Compliance.Failures} fail · {Report.Compliance.Warnings} warnings · {Report.Compliance.Items.Count} checks";
    public string UndoText => Stack?.UndoName is { } n ? $"Undo {n}" : "Undo";
    public IEnumerable<ComplianceItem> FailedChecks => Report?.Compliance.Items.Where(i => i.Status != ComplianceStatus.Pass) ?? [];

    [RelayCommand]
    private void LoadPreset()
    {
        var b = Studio.Preset(SelectedPreset);
        Brief.Load(b);
        BriefText = SelectedPreset == 4 ? BriefPresets.VisionText.Replace("30x40", "30x60") : "";
        Validate();
        Status = $"Preset loaded: {Presets[SelectedPreset]}";
    }

    [RelayCommand]
    private async Task Parse()
    {
        var (result, source) = await Studio.BriefAi.ParseAsync(BriefText);
        Studio.Validator.ApplyBylawSetbacks(result.Brief);
        Brief.Load(result.Brief);
        BriefNotes.Clear();
        BriefNotes.Add($"Parsed with: {source}. Check the form below before generating.");
        foreach (var n in result.Understood) BriefNotes.Add("✓ " + n);
        foreach (var w in result.Warnings) BriefNotes.Add("! " + w);
        Validate();
    }

    [RelayCommand]
    private void ApplySetbacks()
    {
        var b = Brief.ToModel();
        Studio.Validator.ApplyBylawSetbacks(b);
        Brief.FrontSetbackM = b.Site.Setbacks.Front / 1000;
        Brief.RearSetbackM = b.Site.Setbacks.Rear / 1000;
        Brief.SideSetbackM = b.Site.Setbacks.Side / 1000;
        Validate();
    }

    [RelayCommand]
    private void AddRoom() => Brief.Rooms.Add(new RoomRequestViewModel { Type = RoomType.Bedroom, Floor = 0, WidthFt = 10, DepthFt = 11 });

    [RelayCommand]
    private void RemoveRoom(RoomRequestViewModel? row)
    {
        if (row is not null) Brief.Rooms.Remove(row);
    }

    [RelayCommand]
    private void Validate()
    {
        Issues.Clear();
        foreach (var i in Studio.Validator.Validate(Brief.ToModel())) Issues.Add(i);
    }

    [RelayCommand]
    private async Task Generate()
    {
        Validate();
        var brief = Brief.ToModel();
        IsBusy = true;
        Status = "Generating options…";
        try
        {
            var options = await Task.Run(() => Studio.Generate(brief, 5));
            Options.Clear();
            foreach (var o in options) Options.Add(new OptionViewModel(o));
            SelectedOption = Options.FirstOrDefault();
            OptionCosts.Clear();
            Status = $"{options.Count} options ready. Pick one and press Apply.";
            if (SelectedOption is not null) ApplyOption();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void ApplyOption()
    {
        if (SelectedOption is null) return;
        var brief = Brief.ToModel();
        if (Stack is null || Model is null)
        {
            Stack = new CommandStack(Studio.CreateModel(brief, SelectedOption.Option));
        }
        else
        {
            Model.Brief = brief;
            Studio.ApplyOption(Stack, SelectedOption.Option);
        }
        Style = Model!.Brief.Style;
        Levels.Clear();
        foreach (var s in Model.Storeys) Levels.Add(s.Level);
        CurrentLevel = 0;
        Refresh($"{SelectedOption.Option.Name} applied.");
    }

    [RelayCommand]
    private async Task CompareOptions()
    {
        IsBusy = true;
        try
        {
            var brief = Brief.ToModel();
            var opts = Options.Select(o => o.Option).ToList();
            var costs = await Task.Run(() => Studio.CompareOptions(brief, opts));
            OptionCosts.Clear();
            foreach (var c in costs) OptionCosts.Add(c);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task Resolve()
    {
        if (Model is null) return;
        var m = Model;
        IsBusy = true;
        try
        {
            var options = await Task.Run(() => Studio.Resolve(m, 3));
            Options.Clear();
            foreach (var o in options) Options.Add(new OptionViewModel(o));
            SelectedOption = Options.FirstOrDefault();
            Status = "Re-solved around locked rooms. Apply an option to use it (undo restores your edits).";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void Undo() { if (Stack is { CanUndo: true }) { Studio.Undo(Stack); Refresh("Undone."); } }
    [RelayCommand] private void Redo() { if (Stack is { CanRedo: true }) { Studio.Redo(Stack); Refresh("Redone."); } }

    [RelayCommand]
    private void ToggleLock()
    {
        if (Stack is null || SelectedRoom is null) return;
        Studio.SetLock(Stack, SelectedRoom.Id, !SelectedRoom.Locked);
        Refresh($"{SelectedRoom.Name} {(SelectedRoom.Locked ? "locked" : "unlocked")}.");
    }

    [RelayCommand]
    private void ApplyFix(FixSuggestion? fix)
    {
        if (Stack is null || fix is null) return;
        if (Studio.Swap(Stack, fix.Level, fix.RoomA, fix.RoomB)) Refresh(fix.Description);
    }

    [RelayCommand]
    private async Task ApplyEdit()
    {
        if (Stack is null || string.IsNullOrWhiteSpace(EditText)) return;
        var message = await Studio.ApplyTextEditAsync(Stack, EditText, CurrentLevel);
        Refresh(message);
    }

    [RelayCommand]
    private async Task Review()
    {
        if (Model is null || Report is null) return;
        ReviewNotes = await Studio.Reviewer.SummariseAsync(ReportComposer.ReviewFacts(Model, Report));
    }

    [RelayCommand]
    private void Save()
    {
        if (Model is null) return;
        var dlg = new SaveFileDialog { Filter = "Building Studio project (*.json)|*.json", FileName = $"{Model.Name}.json" };
        if (dlg.ShowDialog() != true) return;
        ProjectSerializer.Save(Model, dlg.FileName);
        Status = $"Saved {dlg.FileName}";
    }

    [RelayCommand]
    private void Open()
    {
        var dlg = new OpenFileDialog { Filter = "Building Studio project (*.json)|*.json" };
        if (dlg.ShowDialog() != true) return;
        var model = ProjectSerializer.Load(dlg.FileName);
        Studio.Rebuild(model);
        Stack = new CommandStack(model);
        Brief.Load(model.Brief);
        Style = model.Brief.Style;
        Levels.Clear();
        foreach (var s in model.Storeys) Levels.Add(s.Level);
        CurrentLevel = 0;
        Refresh($"Opened {dlg.FileName}");
    }

    [RelayCommand]
    private async Task ExportAll()
    {
        if (Model is null) return;
        var dlg = new OpenFolderDialog { Title = "Choose an output folder" };
        if (dlg.ShowDialog() != true) return;
        var dir = Path.Combine(dlg.FolderName, Safe(Model.Name));
        var m = Model;
        IsBusy = true;
        try
        {
            var result = await Task.Run(() => ExportService.ExportAll(Studio, m, dir));
            Status = $"Exported {result.Files.Count} files to {dir} (drawings PDF/DXF/SVG, BOQ.xlsx, report.pdf, model.glb, viewer.html).";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true });
        }
        finally { IsBusy = false; }
    }

    private static string Safe(string s) => string.Concat(s.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

    // ---- Plan canvas callbacks ----

    public void RequestSwap(Guid a, Guid b)
    {
        if (Stack is null) return;
        Refresh(Studio.Swap(Stack, CurrentLevel, a, b) ? "Rooms swapped." : "Swap not possible (locked room).");
    }

    public void RequestEdgeMove(Guid room, RectSide side, double coordinate)
    {
        if (Stack is null) return;
        Refresh(Studio.MoveEdge(Stack, CurrentLevel, room, side, coordinate)
            ? "Wall moved; neighbours adjusted." : "Wall cannot move there (exterior wall, locked room or room too small).");
    }

    public void SelectRoom(Guid? id)
    {
        SelectedRoom = id is null ? null : Model?.GetStorey(CurrentLevel)?.FindRoom(id.Value);
        UpdateExplanation();
    }

    partial void OnCurrentLevelChanged(int value) => Refresh(null);
    partial void OnShowFurnitureChanged(bool value) => ModelChanged?.Invoke(this, EventArgs.Empty);
    partial void OnShowStructureChanged(bool value) => ModelChanged?.Invoke(this, EventArgs.Empty);
    partial void OnFloorCutChanged(bool value) => ModelChanged?.Invoke(this, EventArgs.Empty);
    partial void OnShowZonesChanged(bool value) => ModelChanged?.Invoke(this, EventArgs.Empty);

    partial void OnStyleChanged(ExteriorStyle value)
    {
        if (Stack is null || Model is null || Model.Brief.Style == value) return;
        Studio.SetStyle(Stack, value);
        Refresh($"Exterior style: {value}");
    }

    private void UpdateExplanation()
    {
        if (SelectedRoom is null || Report is null) return;
        var v = Report.Vastu.Rooms.FirstOrDefault(r => r.RoomId == SelectedRoom.Id);
        var area = Report.Areas.FirstOrDefault(a => a.Room == SelectedRoom.Name && a.Level == CurrentLevel);
        SelectedRoomExplanation = $"{SelectedRoom.Name}: {SelectedRoom.Bounds.Width / 1000:0.00} x {SelectedRoom.Bounds.Height / 1000:0.00} m " +
            $"({Units.MmToFt(SelectedRoom.Bounds.Width):0.#} x {Units.MmToFt(SelectedRoom.Bounds.Height):0.#} ft), {SelectedRoom.Area / 1e6:0.0} m²" +
            (area is { TargetSqm: > 0 } ? $", target {area.TargetSqm:0.0} m² ({area.DeltaPct:+0;-0;0}%)" : "") +
            $"{Environment.NewLine}{v?.Explanation}{Environment.NewLine}Finish: {SelectedRoom.FloorFinish}" + (SelectedRoom.Locked ? $"{Environment.NewLine}LOCKED" : "");
    }

    private void Refresh(string? message)
    {
        if (Model is null) return;
        Report = Studio.Evaluate(Model, CurrentLevel);
        Sheets.Clear();
        foreach (var s in DrawingSetBuilder.Build(Model)) Sheets.Add(s);
        SelectedSheet ??= Sheets.FirstOrDefault();
        if (SelectedRoom is not null) SelectedRoom = Model.GetStorey(CurrentLevel)?.FindRoom(SelectedRoom.Id);
        UpdateExplanation();
        OnPropertyChanged(nameof(CostText));
        OnPropertyChanged(nameof(ComplianceText));
        OnPropertyChanged(nameof(UndoText));
        OnPropertyChanged(nameof(FailedChecks));
        if (message is not null) Status = message;
        ModelChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ShowError(string message) => MessageBox.Show(message, "AI Building Studio");
}
