using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Engine;
using CRM_Master.Engine.Reporting;

namespace CRM_Master.App.ViewModels;

public sealed record OperationOption(string Label, AuditOperation? Operation);

/// <summary>Searchable, filterable audit history with details and one-click undo of reversible batches.</summary>
public sealed partial class ActivityLogViewModel : ViewModelBase
{
    private readonly IAuditService _audit;
    private readonly BatchUndoService _undo;
    private readonly ReportService _reports;
    private CancellationTokenSource? _debounce;
    private bool _suppress;

    public ActivityLogViewModel(UiServices ui, IAuditService audit, BatchUndoService undo, ReportService reports) : base(ui)
    {
        _audit = audit; _undo = undo; _reports = reports;
        Operations = new[] { new OperationOption("All operations", null) }.Concat(Enum.GetValues<AuditOperation>().Select(o => new OperationOption(o.ToString(), o))).ToList();
        _selectedOperation = Operations[0]; _selectedStatus = Statuses[0]; _selectedRange = Ranges[0];
    }

    public IReadOnlyList<OperationOption> Operations { get; }
    public IReadOnlyList<StatusOption> Statuses { get; } = new[]
    {
        new StatusOption("Any status", null), new StatusOption("Success", OperationStatus.Success), new StatusOption("Warning", OperationStatus.Warning),
        new StatusOption("Failed", OperationStatus.Failed), new StatusOption("Cancelled", OperationStatus.Cancelled)
    };
    public IReadOnlyList<RangeOption> Ranges { get; } = new[]
    {
        new RangeOption("All time", null), new RangeOption("Last 24 hours", 1), new RangeOption("Last 7 days", 7), new RangeOption("Last 30 days", 30)
    };
    public ObservableCollection<AuditLog> Entries { get; } = new();

    [ObservableProperty] private string _search = "";
    [ObservableProperty] private OperationOption _selectedOperation;
    [ObservableProperty] private StatusOption _selectedStatus;
    [ObservableProperty] private RangeOption _selectedRange;
    [ObservableProperty] private string _recordIdText = "";
    [ObservableProperty] private AuditLog? _selectedEntry;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _total;
    [ObservableProperty] private bool _canUndoBatch;

    public string PageText => $"Page {Page} of {Math.Max(1, TotalPages)}  -  {Total:N0} entr{(Total == 1 ? "y" : "ies")}";
    public bool HasDetail => SelectedEntry is not null;
    partial void OnPageChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnTotalChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnSearchChanged(string value) => _ = DebouncedAsync();
    partial void OnRecordIdTextChanged(string value) => _ = DebouncedAsync();
    partial void OnSelectedOperationChanged(OperationOption value) { if (!_suppress) _ = ResetAsync(); }
    partial void OnSelectedStatusChanged(StatusOption value) { if (!_suppress) _ = ResetAsync(); }
    partial void OnSelectedRangeChanged(RangeOption value) { if (!_suppress) _ = ResetAsync(); }
    partial void OnSelectedEntryChanged(AuditLog? value) { OnPropertyChanged(nameof(HasDetail)); _ = EvaluateUndoAsync(value); }

    private async Task EvaluateUndoAsync(AuditLog? e)
    {
        CanUndoBatch = e?.BatchId is { Length: > 0 } b && await _undo.CanUndoAsync(b);
    }

    public override Task ActivateAsync() => ReloadAsync();

    private async Task DebouncedAsync()
    {
        _debounce?.Cancel(); var cts = _debounce = new CancellationTokenSource();
        try { await Task.Delay(350, cts.Token); await ResetAsync(); } catch (OperationCanceledException) { }
    }
    private Task ResetAsync() { Page = 1; return ReloadAsync(); }

    private AuditQuery BuildQuery() => new(
        string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(), SelectedOperation.Operation, SelectedStatus.Status,
        SelectedRange.Days is { } d ? DateTime.UtcNow.AddDays(-d) : null, null, long.TryParse(RecordIdText.Trim(), out var id) ? id : null, null, null, Page, 100);

    [RelayCommand]
    private async Task ReloadAsync()
    {
        PagedResult<AuditLog>? result = null;
        await RunAsync("Loading activity log", async () => result = await _audit.QueryAsync(BuildQuery()));
        if (result is null) return;
        Entries.Clear(); foreach (var e in result.Items) Entries.Add(e);
        Page = result.Page; TotalPages = result.TotalPages; Total = result.TotalCount;
    }

    [RelayCommand] private async Task NextPage() { if (Page < TotalPages) { Page++; await ReloadAsync(); } }
    [RelayCommand] private async Task PreviousPage() { if (Page > 1) { Page--; await ReloadAsync(); } }
    [RelayCommand] private async Task Clear() { _suppress = true; Search = ""; RecordIdText = ""; SelectedOperation = Operations[0]; SelectedStatus = Statuses[0]; SelectedRange = Ranges[0]; _suppress = false; await ResetAsync(); }

    [RelayCommand]
    private async Task UndoBatch()
    {
        if (SelectedEntry?.BatchId is not { } batch) return;
        if (!Ui.Dialogs.Confirm("Undo this operation?", $"Reverts every change recorded under batch {batch} (restoring previous values and re-creating removed records). Values edited afterwards are left alone.", "Undo")) return;
        UndoResult? res = null;
        if (!await RunAsync("Undoing operation", async () => res = await _undo.UndoAsync(batch), AuditOperation.ContactUpdated) || res is null) return;
        Ui.Notifications.Add(NotificationLevel.Success, "Operation undone", $"{res.Reverted:N0} reverted, {res.Restored:N0} restored, {res.Conflicts:N0} left unchanged.");
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task Export(string? formatName)
    {
        var format = Enum.TryParse<ExportFormat>(formatName, out var f) ? f : ExportFormat.Excel;
        var filter = format switch { ExportFormat.Excel => "Excel workbook (*.xlsx)|*.xlsx", ExportFormat.Csv => "CSV file (*.csv)|*.csv", _ => "PDF document (*.pdf)|*.pdf" };
        var path = Ui.Dialogs.PickSaveFile(_reports.SuggestedFileName(ReportType.Audit, format), filter, Ui.Settings.Current.ExportFolder);
        if (path is null) return;
        await RunAsync("Exporting audit report", async () =>
        {
            var from = SelectedRange.Days is { } d ? DateTime.UtcNow.AddDays(-d) : (DateTime?)null;
            var data = await _reports.BuildAsync(ReportType.Audit, new(from, null, null, SelectedStatus.Status));
            await _reports.ExportAsync(data, format, path);
            if (Ui.Dialogs.Confirm("Report saved", $"{data.RowCount:N0} entr{(data.RowCount == 1 ? "y" : "ies")} saved to:\n{path}\n\nOpen it now?", "Open")) Ui.Dialogs.OpenInShell(path);
        }, AuditOperation.Export);
    }

    [RelayCommand]
    private async Task PurgeOld()
    {
        var days = Ui.Settings.Current.LogRetentionDays;
        if (!Ui.Dialogs.Confirm("Delete old log entries?", $"Entries older than {days} day(s) (your Settings -> log retention) will be permanently removed. Recent history is not affected.", "Delete old entries", danger: true)) return;
        var n = 0;
        if (await RunAsync("Purging old entries", async () => n = await _audit.PurgeOlderThanAsync(days))) { Ui.Notifications.Add(NotificationLevel.Info, "Log cleaned", $"{n:N0} old entr{(n == 1 ? "y" : "ies")} removed."); await ReloadAsync(); }
    }
}
