using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Engine.Reporting;

namespace CRM_Master.App.ViewModels;

public sealed record ReportTypeOption(string Title, ReportType Type, string Description);
public sealed record RangeOption(string Label, int? Days);
public sealed record StatusOption(string Label, OperationStatus? Status);
public sealed record SummaryLine(string Label, string Value);

/// <summary>Reports and analytics: choose report, date range, category and status; preview; export to Excel, CSV or PDF.</summary>
public sealed partial class ReportsViewModel : ViewModelBase
{
    private readonly ReportService _reports;
    private readonly IContactRepository _repo;
    private ReportData? _data;

    public ReportsViewModel(UiServices ui, ReportService reports, IContactRepository repo) : base(ui)
    {
        _reports = reports; _repo = repo;
        _selectedType = Types[0]; _selectedRange = Ranges[1]; _selectedStatus = Statuses[0]; _selectedCategory = AllCategories;
    }

    public const string AllCategories = "All categories";
    public IReadOnlyList<ReportTypeOption> Types { get; } = new[]
    {
        new ReportTypeOption("Import summary", ReportType.ImportSummary, "Every import: file, outcome, rows imported, duplicates and failures."),
        new ReportTypeOption("Duplicate report", ReportType.Duplicates, "Duplicate pairs with scores, match reasons and both records' details."),
        new ReportTypeOption("Data-quality report", ReportType.DataQuality, "Completeness, invalid and missing values, and validation findings."),
        new ReportTypeOption("Invalid-record report", ReportType.InvalidRecords, "Contacts needing attention with the problem and a suggested fix."),
        new ReportTypeOption("Bulk-update report", ReportType.BulkUpdate, "Every field changed by bulk updates: previous and new values."),
        new ReportTypeOption("Automation execution report", ReportType.AutomationExecution, "When each rule ran and how many contacts it affected."),
        new ReportTypeOption("Audit report", ReportType.Audit, "The full audit trail of operations."),
    };
    public IReadOnlyList<RangeOption> Ranges { get; } = new[]
    {
        new RangeOption("All time", null), new RangeOption("Last 30 days", 30), new RangeOption("Last 7 days", 7), new RangeOption("Last 90 days", 90), new RangeOption("Last 365 days", 365), new RangeOption("Custom dates", -1)
    };
    public IReadOnlyList<StatusOption> Statuses { get; } = new[]
    {
        new StatusOption("Any status", null), new StatusOption("Success", OperationStatus.Success), new StatusOption("Warning", OperationStatus.Warning),
        new StatusOption("Failed", OperationStatus.Failed), new StatusOption("Cancelled", OperationStatus.Cancelled)
    };
    public ObservableCollection<string> Categories { get; } = new() { AllCategories };
    public ObservableCollection<SummaryLine> Summary { get; } = new();

    [ObservableProperty] private ReportTypeOption _selectedType;
    [ObservableProperty] private RangeOption _selectedRange;
    [ObservableProperty] private StatusOption _selectedStatus;
    [ObservableProperty] private string _selectedCategory;
    [ObservableProperty] private string _fromText = "";
    [ObservableProperty] private string _toText = "";
    [ObservableProperty] private string _batchId = "";
    [ObservableProperty] private DataView? _table;
    [ObservableProperty] private string? _previewNote;
    [ObservableProperty] private bool _hasData;

    public bool IsCustomRange => SelectedRange.Days == -1;
    public bool ShowBatch => SelectedType.Type == ReportType.BulkUpdate || SelectedType.Type == ReportType.Audit;
    partial void OnSelectedRangeChanged(RangeOption value) => OnPropertyChanged(nameof(IsCustomRange));
    partial void OnSelectedTypeChanged(ReportTypeOption value) { OnPropertyChanged(nameof(ShowBatch)); HasData = false; _data = null; }

    public override async Task ActivateAsync()
    {
        var keep = SelectedCategory;
        var statuses = await _repo.GetDistinctAsync(ContactField.Status);
        Categories.Clear(); Categories.Add(AllCategories);
        foreach (var s in statuses) Categories.Add(s);
        SelectedCategory = Categories.Contains(keep) ? keep : AllCategories;
    }

    private ReportFilter BuildFilter()
    {
        DateTime? from = null, to = null;
        if (SelectedRange.Days is > 0 and var d) from = DateTime.UtcNow.AddDays(-d);
        else if (IsCustomRange)
        {
            if (!string.IsNullOrWhiteSpace(FromText)) from = ParseDate(FromText, "From") ;
            if (!string.IsNullOrWhiteSpace(ToText)) to = ParseDate(ToText, "To")?.AddDays(1).AddTicks(-1);
        }
        return new ReportFilter(from, to, SelectedCategory == AllCategories ? null : SelectedCategory, SelectedStatus.Status, string.IsNullOrWhiteSpace(BatchId) ? null : BatchId.Trim());
    }

    private static DateTime? ParseDate(string text, string label)
    {
        if (!Core.BusinessRules.DateRules.TryParse(text, out var d)) throw new ArgumentException($"'{label}' date '{text}' is not valid. Use a format like 2025-01-31 or 31/01/2025.");
        return DateTime.SpecifyKind(d.Date, DateTimeKind.Local).ToUniversalTime();
    }

    [RelayCommand]
    private async Task Preview()
    {
        ReportData? data = null;
        var ok = await RunAsync("Building report", async () => data = await _reports.BuildAsync(SelectedType.Type, BuildFilter()));
        if (!ok || data is null) return;
        _data = data; HasData = true;
        Summary.Clear(); foreach (var (label, value) in data.Summary) Summary.Add(new SummaryLine(label, value));

        var dt = new DataTable();
        for (var i = 0; i < data.Columns.Count; i++) dt.Columns.Add(new DataColumn($"c{i}", typeof(string)) { Caption = data.Columns[i] });
        foreach (var row in data.Rows.Take(1000))
            dt.Rows.Add(row.Select(v => v switch { null => (object)DBNull.Value, DateTime d => d.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture), _ => Convert.ToString(v, CultureInfo.CurrentCulture) ?? "" }).ToArray());
        Table = dt.DefaultView;
        PreviewNote = data.RowCount > 1000 ? $"Showing the first 1,000 of {data.RowCount:N0} rows. The exported file contains all rows." : $"{data.RowCount:N0} row(s).";
    }

    [RelayCommand]
    private async Task Export(string? formatName)
    {
        var format = Enum.TryParse<ExportFormat>(formatName, out var f) ? f : ExportFormat.Excel;
        if (_data is null) { await Preview(); if (_data is null) return; }
        var filter = format switch { ExportFormat.Excel => "Excel workbook (*.xlsx)|*.xlsx", ExportFormat.Csv => "CSV file (*.csv)|*.csv", _ => "PDF document (*.pdf)|*.pdf" };
        var path = Ui.Dialogs.PickSaveFile(_reports.SuggestedFileName(SelectedType.Type, format), filter, Ui.Settings.Current.ExportFolder);
        if (path is null) return;
        var data = _data;
        await RunAsync("Exporting report", async () =>
        {
            await _reports.ExportAsync(data, format, path);
            Ui.Notifications.Add(NotificationLevel.Success, "Report saved", Path.GetFileName(path));
            if (Ui.Dialogs.Confirm("Report saved", $"{data.RowCount:N0} row(s) saved to:\n{path}\n\nOpen it now?", "Open")) Ui.Dialogs.OpenInShell(path);
        }, AuditOperation.Export);
    }
}
