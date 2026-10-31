using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Engine.DuplicateDetection;
using CRM_Master.Engine.Reporting;

namespace CRM_Master.App.ViewModels;

/// <summary>One record of the selected group, shown as a column in the side-by-side view.</summary>
public sealed partial class GroupContactVM : ObservableObject
{
    public GroupContactVM(Contact c) => Contact = c;
    public Contact Contact { get; }
    [ObservableProperty] private bool _isMaster;
    [ObservableProperty] private bool _include = true;
    public string Header => $"#{Contact.Id}  {Contact.DisplayName}";
    public string SubHeader => $"Created {Contact.CreatedAt.ToLocalTime():dd MMM yyyy}";
}

/// <summary>One field across all records of the group: values, match/conflict highlighting and the user's merge choice.</summary>
public sealed partial class MergeFieldRowVM : ObservableObject
{
    public MergeFieldRowVM(MergeFieldChoice choice, IReadOnlyList<string?> values)
    {
        Label = choice.Label; Field = choice.Field; Values = values; Candidates = choice.Candidates.Select(c => c.Value).ToList();
        _selected = choice.Selected;
        var distinct = values.Where(v => v is not null).Select(v => v!.Trim().ToLowerInvariant()).Distinct().Count();
        Status = distinct == 0 ? "empty" : distinct > 1 ? "conflict" : values.Any(v => v is null) ? "fill" : "match";
    }
    public string Label { get; }
    public ContactField Field { get; }
    public IReadOnlyList<string?> Values { get; }
    public IReadOnlyList<string> Candidates { get; }
    /// <summary>empty | match | conflict | fill (one record has it, others are blank).</summary>
    public string Status { get; }
    public bool HasChoices => Candidates.Count > 1;
    [ObservableProperty] private string? _selected;
}

public sealed record ClassOption(string Label, MatchClass? Class);

/// <summary>Duplicate detection: configurable scan, group review, side-by-side comparison, reviewed merge.</summary>
public sealed partial class DuplicatesViewModel : ViewModelBase
{
    private readonly DuplicateService _svc;
    private readonly ReportService _reports;
    private CancellationTokenSource? _cts;

    public DuplicatesViewModel(UiServices ui, DuplicateService svc, ReportService reports) : base(ui)
    { _svc = svc; _reports = reports; _selectedClass = ClassOptions[0]; }

    public IReadOnlyList<ClassOption> ClassOptions { get; } = new[]
    {
        new ClassOption("All matches", null), new ClassOption("Exact duplicates", MatchClass.Exact), new ClassOption("High confidence and above", MatchClass.High),
        new ClassOption("Possible and above", MatchClass.Possible), new ClassOption("Low confidence and above", MatchClass.Low)
    };
    public ObservableCollection<DuplicateGroupView> Groups { get; } = new();
    public ObservableCollection<GroupContactVM> GroupContacts { get; } = new();
    public ObservableCollection<MergeFieldRowVM> FieldRows { get; } = new();

    [ObservableProperty] private bool _matchEmail = true;
    [ObservableProperty] private bool _matchPhone = true;
    [ObservableProperty] private bool _matchName = true;
    [ObservableProperty] private bool _matchCompany = true;
    [ObservableProperty] private bool _useFuzzy = true;
    [ObservableProperty] private double _highThreshold = 0.90;
    [ObservableProperty] private double _possibleThreshold = 0.80;
    [ObservableProperty] private double _lowThreshold = 0.70;
    [ObservableProperty] private ClassOption _selectedClass;
    [ObservableProperty] private DuplicateGroupView? _selectedGroup;
    [ObservableProperty] private string _countsText = "No scan results yet.";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressText;
    [ObservableProperty] private string? _scanNote;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalGroups;

    public string PageText => $"Page {Page} of {Math.Max(1, TotalPages)}  -  {TotalGroups:N0} group(s)";
    public bool HasSelection => SelectedGroup is not null;
    public bool HasGroups => TotalGroups > 0;
    partial void OnPageChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnTotalGroupsChanged(int value) { OnPropertyChanged(nameof(PageText)); OnPropertyChanged(nameof(HasGroups)); }
    partial void OnSelectedClassChanged(ClassOption value) { Page = 1; _ = LoadGroupsAsync(); }
    partial void OnSelectedGroupChanged(DuplicateGroupView? value) { OnPropertyChanged(nameof(HasSelection)); BuildComparison(value); }

    public override async Task ActivateAsync()
    {
        var s = Ui.Settings.Current;
        HighThreshold = s.DuplicateHighThreshold; PossibleThreshold = s.DuplicatePossibleThreshold; LowThreshold = s.DuplicateLowThreshold;
        await LoadGroupsAsync();
    }

    private DuplicateOptions BuildOptions() => new()
    {
        MatchEmail = MatchEmail, MatchPhone = MatchPhone, MatchName = MatchName, MatchCompany = MatchCompany, UseFuzzy = UseFuzzy,
        HighThreshold = Math.Round(HighThreshold, 2), PossibleThreshold = Math.Round(PossibleThreshold, 2), LowThreshold = Math.Round(LowThreshold, 2),
        DefaultCountryCode = Ui.Settings.Current.DefaultCountryCode
    };

    [RelayCommand]
    private async Task Scan()
    {
        var o = BuildOptions();
        if (!(o.LowThreshold < o.PossibleThreshold && o.PossibleThreshold < o.HighThreshold)) { Ui.Dialogs.Error("Check the thresholds", "Thresholds must increase: Low < Possible < High."); return; }
        _cts = new CancellationTokenSource();
        var progress = new Progress<ProgressInfo>(p => { Progress = p.Fraction; ProgressText = $"{p.Stage}: {p.Done:N0} of {p.Total:N0}"; });
        ScanResult? scan = null;
        var ok = await RunAsync("Scanning for duplicates", async () => scan = await _svc.ScanAsync(o, progress, _cts.Token), AuditOperation.DuplicateDetection);
        ProgressText = null; Progress = 0;
        if (!ok || scan is null) return;
        ScanNote = $"Scanned {scan.ContactsScanned:N0} contacts using {scan.CandidatePairs:N0} comparisons (a full pairwise check would need {(long)scan.ContactsScanned * (scan.ContactsScanned - 1) / 2:N0})." +
                   (scan.SkippedLargeBlocks > 0 ? $" {scan.SkippedLargeBlocks} very common value(s) (for example a shared email or a placeholder name) were skipped." : "");
        Ui.Notifications.Add(NotificationLevel.Info, "Duplicate scan finished", $"{scan.Clusters.Count:N0} group(s) to review.");
        Page = 1; await LoadGroupsAsync();
    }

    [RelayCommand] private void CancelScan() => _cts?.Cancel();

    private async Task LoadGroupsAsync()
    {
        PagedResult<DuplicateGroupView>? page = null; Dictionary<MatchClass, int>? counts = null;
        await RunAsync("Loading duplicate groups", async () =>
        {
            page = await _svc.GetGroupsAsync(DuplicateStatus.Open, SelectedClass.Class, Page, 50);
            counts = await _svc.CountsAsync();
        });
        if (page is null || counts is null) return;
        int N(MatchClass c) => counts.GetValueOrDefault(c);
        CountsText = $"Open groups: {N(MatchClass.Exact)} exact  -  {N(MatchClass.High)} high  -  {N(MatchClass.Possible)} possible  -  {N(MatchClass.Low)} low";
        Groups.Clear(); foreach (var g in page.Items) Groups.Add(g);
        TotalGroups = page.TotalCount; TotalPages = page.TotalPages; Page = page.Page;
        SelectedGroup = Groups.FirstOrDefault();
    }

    [RelayCommand] private async Task NextPage() { if (Page < TotalPages) { Page++; await LoadGroupsAsync(); } }
    [RelayCommand] private async Task PreviousPage() { if (Page > 1) { Page--; await LoadGroupsAsync(); } }

    // ---- side-by-side ----
    private void BuildComparison(DuplicateGroupView? g)
    {
        GroupContacts.Clear(); FieldRows.Clear();
        if (g is null || g.Contacts.Count == 0) return;
        var master = MergePlanner.SuggestMaster(g.Contacts);
        foreach (var c in g.Contacts)
        {
            var vm = new GroupContactVM(c) { IsMaster = c.Id == master.Id };
            vm.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(GroupContactVM.IsMaster) or nameof(GroupContactVM.Include)) OnMasterOrIncludeChanged(vm, e.PropertyName); };
            GroupContacts.Add(vm);
        }
        RebuildFieldRows();
    }

    private bool _rebuilding;
    private void OnMasterOrIncludeChanged(GroupContactVM changed, string? prop)
    {
        if (_rebuilding) return;
        _rebuilding = true;
        if (prop == nameof(GroupContactVM.IsMaster) && changed.IsMaster) { foreach (var o in GroupContacts.Where(x => x != changed)) o.IsMaster = false; changed.Include = true; }
        if (prop == nameof(GroupContactVM.IsMaster) && !changed.IsMaster && !GroupContacts.Any(x => x.IsMaster)) changed.IsMaster = true;   // a master is always required
        _rebuilding = false;
        RebuildFieldRows();
    }

    private void RebuildFieldRows()
    {
        FieldRows.Clear();
        var master = GroupContacts.FirstOrDefault(c => c.IsMaster); if (master is null) return;
        var others = GroupContacts.Where(c => c.Include && !c.IsMaster).Select(c => c.Contact).ToList();
        var choices = MergePlanner.Plan(master.Contact, others);
        var shown = GroupContacts.Select(c => c.Contact).ToList();
        foreach (var ch in choices)
        {
            var values = shown.Select(c => c.Get(ch.Field)).ToList();
            if (values.All(v => v is null)) continue;
            FieldRows.Add(new MergeFieldRowVM(ch, values));
        }
    }

    // ---- actions ----
    [RelayCommand]
    private async Task Merge()
    {
        if (SelectedGroup is null) return;
        var master = GroupContacts.FirstOrDefault(c => c.IsMaster); var losers = GroupContacts.Where(c => c.Include && !c.IsMaster).ToList();
        if (master is null || losers.Count == 0) { Ui.Dialogs.Info("Nothing to merge", "Choose a master record and keep at least one other record ticked."); return; }
        var conflicts = FieldRows.Count(r => r.Status == "conflict");
        var msg = $"Merge {losers.Count} record(s) into the master \"{master.Contact.DisplayName}\" (#{master.Contact.Id})?\n\nThe field values you selected are kept ({conflicts} field(s) had conflicting values). The other record(s) are removed after a backup; their full details stay in the audit log and the merge can be undone from Activity Logs.";
        if (!Ui.Dialogs.Confirm("Merge duplicates?", msg, "Merge", danger: false)) return;
        var chosen = FieldRows.ToDictionary(r => r.Field, r => r.Selected);
        var group = SelectedGroup;
        MergeResult? result = null;
        var ok = await RunAsync("Merging records", async () => result = await _svc.MergeAsync(group.GroupId, master.Contact.Id, chosen, losers.Select(l => l.Contact.Id).ToList()), AuditOperation.Merge);
        if (!ok || result is null) return;
        Ui.Notifications.Add(NotificationLevel.Success, "Records merged", $"{result.Merged} record(s) merged into #{result.MasterId}.");
        await LoadGroupsAsync();
    }

    [RelayCommand]
    private async Task Skip()
    {
        if (SelectedGroup is null) return;
        var id = SelectedGroup.GroupId;
        if (await RunAsync("Skipping group", () => _svc.SkipAsync(id))) await LoadGroupsAsync();
    }

    [RelayCommand]
    private async Task MarkSeparate()
    {
        if (SelectedGroup is null) return;
        if (!Ui.Dialogs.Confirm("Mark as separate customers?", "These records will be remembered as different people/companies and will not be suggested as duplicates again. Nothing is deleted or changed.", "Mark as separate")) return;
        var id = SelectedGroup.GroupId;
        if (await RunAsync("Saving decision", () => _svc.MarkSeparateAsync(id), AuditOperation.DuplicateDetection)) await LoadGroupsAsync();
    }

    [RelayCommand]
    private async Task MergeAllExact()
    {
        if (!Ui.Dialogs.Confirm("Merge all exact duplicates?", "Every open group classified as EXACT (all shared fields identical) is merged into its most complete record. Uncertain matches (High, Possible, Low) are never merged automatically.\n\nA backup is taken first and each merge can be undone from Activity Logs.", "Merge exact duplicates")) return;
        var cts = _cts = new CancellationTokenSource();
        var progress = new Progress<ProgressInfo>(p => { Progress = p.Fraction; ProgressText = $"{p.Stage}: {p.Done:N0} of {p.Total:N0}"; });
        (int Groups, int Records) res = (0, 0);
        var ok = await RunAsync("Merging exact duplicates", async () => res = await _svc.MergeAllExactAsync(progress, cts.Token), AuditOperation.Merge);
        ProgressText = null; Progress = 0;
        if (ok) Ui.Notifications.Add(NotificationLevel.Success, "Exact duplicates merged", $"{res.Records:N0} record(s) merged in {res.Groups:N0} group(s).");
        await LoadGroupsAsync();
    }

    [RelayCommand]
    private async Task ExportReport(string? formatName)
    {
        var format = Enum.TryParse<ExportFormat>(formatName, out var f) ? f : ExportFormat.Excel;
        var filter = format switch { ExportFormat.Excel => "Excel workbook (*.xlsx)|*.xlsx", ExportFormat.Csv => "CSV file (*.csv)|*.csv", _ => "PDF document (*.pdf)|*.pdf" };
        var path = Ui.Dialogs.PickSaveFile(_reports.SuggestedFileName(ReportType.Duplicates, format), filter, Ui.Settings.Current.ExportFolder);
        if (path is null) return;
        await RunAsync("Exporting duplicate report", async () =>
        {
            var data = await _reports.BuildAsync(ReportType.Duplicates, new());
            await _reports.ExportAsync(data, format, path);
            Ui.Notifications.Add(NotificationLevel.Success, "Report saved", Path.GetFileName(path));
            if (Ui.Dialogs.Confirm("Report saved", $"Saved to:\n{path}\n\nOpen it now?", "Open")) Ui.Dialogs.OpenInShell(path);
        }, AuditOperation.Export);
    }
}
