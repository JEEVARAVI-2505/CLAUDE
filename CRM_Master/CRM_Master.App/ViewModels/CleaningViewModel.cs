using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.DTOs;
using CRM_Master.Engine;
using CRM_Master.Engine.Cleaning;

namespace CRM_Master.App.ViewModels;

/// <summary>Data cleaning: analyse (before/after preview) -> apply (with backup) -> undo.</summary>
public sealed partial class CleaningViewModel : ViewModelBase
{
    private const int ShowLimit = 5000;
    private readonly CleaningService _svc;
    private readonly BatchUndoService _undo;
    private CleaningReport? _report;
    private CleaningOptions? _optionsUsed;
    private CancellationTokenSource? _cts;

    public CleaningViewModel(UiServices ui, CleaningService svc, BatchUndoService undo) : base(ui) { _svc = svc; _undo = undo; }

    public ObservableCollection<CleaningChange> Changes { get; } = new();
    public ObservableCollection<CleaningIssue> Issues { get; } = new();
    public ObservableCollection<string> DuplicateGroups { get; } = new();

    [ObservableProperty] private bool _trimAndCollapse = true;
    [ObservableProperty] private bool _properCaseNames = true;
    [ObservableProperty] private bool _normalizeEmail = true;
    [ObservableProperty] private bool _normalizePhone = true;
    [ObservableProperty] private bool _normalizeWebsite = true;
    [ObservableProperty] private bool _removeExactDuplicates;
    [ObservableProperty] private bool _hasReport;
    [ObservableProperty] private int _scanned;
    [ObservableProperty] private int _recordsAffected;
    [ObservableProperty] private int _totalChanges;
    [ObservableProperty] private int _errorCount;
    [ObservableProperty] private int _removable;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressText;
    [ObservableProperty] private string? _limitNote;
    [ObservableProperty] private string? _resultText;
    [ObservableProperty] private string? _lastBatchId;
    [ObservableProperty] private bool _canUndo;

    public bool CanApply => HasReport && (TotalChanges > 0 || (RemoveExactDuplicates && Removable > 0)) && !IsBusy;

    partial void OnTotalChangesChanged(int value) => OnPropertyChanged(nameof(CanApply));
    partial void OnRemovableChanged(int value) => OnPropertyChanged(nameof(CanApply));
    partial void OnHasReportChanged(bool value) => OnPropertyChanged(nameof(CanApply));
    partial void OnRemoveExactDuplicatesChanged(bool value) => OnPropertyChanged(nameof(CanApply));

    private CleaningOptions BuildOptions() => new()
    {
        TrimAndCollapseSpaces = TrimAndCollapse, ProperCaseNames = ProperCaseNames, NormalizeEmail = NormalizeEmail, NormalizePhone = NormalizePhone,
        NormalizeWebsite = NormalizeWebsite, RemoveExactDuplicates = RemoveExactDuplicates, DefaultCountryCode = Ui.Settings.Current.DefaultCountryCode
    };

    [RelayCommand]
    private async Task Analyze()
    {
        _cts = new CancellationTokenSource(); var options = BuildOptions();
        var progress = new Progress<ProgressInfo>(p => { Progress = p.Fraction; ProgressText = $"{p.Stage}: {p.Done:N0} of {p.Total:N0}"; });
        CleaningReport? report = null;
        var ok = await RunAsync("Analysing data", async () => report = await _svc.AnalyzeAsync(options, progress, _cts.Token), Core.Entities.AuditOperation.DataCleaning);
        ProgressText = null; Progress = 0;
        if (!ok || report is null) return;

        _report = report; _optionsUsed = options; ResultText = null;
        Scanned = report.ContactsScanned; RecordsAffected = report.RecordsAffected; TotalChanges = report.Changes.Count; ErrorCount = report.ErrorCount; Removable = report.RemovableDuplicates;
        Changes.Clear(); foreach (var c in report.Changes.Take(ShowLimit)) Changes.Add(c);
        Issues.Clear(); foreach (var i in report.Issues.OrderByDescending(i => i.Kind != CleaningIssueKind.EmptyField).Take(ShowLimit)) Issues.Add(i);
        DuplicateGroups.Clear();
        foreach (var g in report.ExactDuplicateGroups.Take(500)) DuplicateGroups.Add($"Keep #{g[0]}, remove {string.Join(", ", g.Skip(1).Select(x => "#" + x))}");
        LimitNote = report.Changes.Count > ShowLimit || report.Issues.Count > ShowLimit ? $"Showing the first {ShowLimit:N0} rows of each list; applying changes uses all {report.Changes.Count:N0} proposed changes." : null;
        HasReport = true;
        Ui.Notifications.Add(NotificationLevel.Info, "Analysis complete", $"{report.Changes.Count:N0} proposed change(s) in {report.RecordsAffected:N0} record(s).");
    }

    [RelayCommand] private void CancelAnalysis() => _cts?.Cancel();

    [RelayCommand]
    private async Task Apply()
    {
        if (_report is null || _optionsUsed is null) return;
        var removeDupes = RemoveExactDuplicates && Removable > 0;
        var msg = $"Apply {TotalChanges:N0} change(s) to {RecordsAffected:N0} contact(s)?" +
                  (removeDupes ? $"\n\nAlso remove {Removable:N0} exact duplicate record(s) (the oldest of each set is kept)." : "") +
                  "\n\nA backup of the database is taken first. Previous values are recorded in the audit log, and you can undo this from here or from Activity Logs. Values edited since the analysis are skipped, never overwritten.";
        if (!Ui.Dialogs.Confirm("Apply data cleaning?", msg, "Apply changes", danger: removeDupes)) return;

        _cts = new CancellationTokenSource();
        var progress = new Progress<ProgressInfo>(p => { Progress = p.Fraction; ProgressText = $"{p.Stage}: {p.Done:N0} of {p.Total:N0}"; });
        CleaningApplyResult? result = null;
        var ok = await RunAsync("Applying cleaning", async () => result = await _svc.ApplyAsync(_report, _optionsUsed, removeDupes, progress, _cts.Token), Core.Entities.AuditOperation.DataCleaning);
        ProgressText = null; Progress = 0;
        if (!ok || result is null) return;

        LastBatchId = result.BatchId; CanUndo = true;
        ResultText = $"Cleaned {result.ContactsUpdated:N0} contact(s) ({result.FieldsChanged:N0} fields)" + (result.DuplicatesRemoved > 0 ? $", removed {result.DuplicatesRemoved:N0} exact duplicate(s)" : "") +
                     (result.SkippedBecauseChanged > 0 ? $", skipped {result.SkippedBecauseChanged:N0} value(s) edited since the analysis" : "") + ". Backup: " + result.BackupPath;
        Ui.Notifications.Add(NotificationLevel.Success, "Data cleaning applied", ResultText);
        HasReport = false; Changes.Clear(); TotalChanges = 0;
    }

    [RelayCommand]
    private async Task UndoLast()
    {
        if (LastBatchId is null) return;
        if (!Ui.Dialogs.Confirm("Undo data cleaning?", "This restores the values (and removed duplicates) recorded for the last cleaning run. Anything you edited afterwards is left as it is.", "Undo")) return;
        UndoResult? res = null;
        var ok = await RunAsync("Undoing cleaning", async () => res = await _undo.UndoAsync(LastBatchId), Core.Entities.AuditOperation.DataCleaning);
        if (!ok || res is null) return;
        CanUndo = false;
        ResultText = $"Undone: {res.Reverted:N0} contact(s) reverted, {res.Restored:N0} restored" + (res.Conflicts > 0 ? $", {res.Conflicts:N0} left unchanged because they were edited later." : ".");
        Ui.Notifications.Add(NotificationLevel.Success, "Cleaning undone", ResultText);
    }

    [RelayCommand] private void OpenBackups() => Ui.Navigation.Navigate(Module.Settings);
}
