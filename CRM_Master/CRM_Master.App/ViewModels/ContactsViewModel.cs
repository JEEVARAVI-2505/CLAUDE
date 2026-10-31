using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.App.Views;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Engine.Automation;
using CRM_Master.Engine.Reporting;

namespace CRM_Master.App.ViewModels;

/// <summary>A selectable row in the contact grid.</summary>
public sealed partial class ContactRow : ObservableObject
{
    public ContactRow(Contact c) => Contact = c;
    public Contact Contact { get; }
    [ObservableProperty] private bool _isSelected;
    public int Id => Contact.Id;
    public string Name => Contact.DisplayName;
    public string? Company => Contact.CompanyName;
    public string? Email => Contact.Email;
    public string? Phone => Contact.Phone;
    public string? City => Contact.City;
    public string Status => Contact.Status;
    public ValidationState EmailState => Contact.EmailState;
    public ValidationState PhoneState => Contact.PhoneState;
    public DateTime UpdatedAt => Contact.UpdatedAt;
    public string Flags => string.Join(", ", new[] { Contact.IsIncomplete ? "Incomplete" : null, Contact.IsDuplicateFlagged ? "Duplicate?" : null, Contact.NeedsReview ? "Review" : null }.Where(x => x is not null));
}

public sealed record QualityOption(string Label, QualityFilter Filter);

/// <summary>Contact list with search, filter, sort, paging, bulk selection and export.</summary>
public sealed partial class ContactsViewModel : ViewModelBase
{
    private readonly IContactRepository _repo;
    private readonly IBackupService _backup;
    private readonly ReportService _reports;
    private readonly AutomationService _automation;
    private CancellationTokenSource? _searchCts;
    private bool _suppress;

    public ContactsViewModel(UiServices ui, IContactRepository repo, IBackupService backup, ReportService reports, AutomationService automation) : base(ui)
    { _repo = repo; _backup = backup; _reports = reports; _automation = automation; _selectedQuality = QualityOptions[0]; }

    public ObservableCollection<ContactRow> Rows { get; } = new();
    public ObservableCollection<string> StatusOptions { get; } = new() { AllStatuses };
    public const string AllStatuses = "All statuses";
    public IReadOnlyList<QualityOption> QualityOptions { get; } = new[]
    {
        new QualityOption("All quality states", QualityFilter.All), new QualityOption("Invalid email", QualityFilter.InvalidEmail), new QualityOption("Invalid phone", QualityFilter.InvalidPhone),
        new QualityOption("Missing email", QualityFilter.MissingEmail), new QualityOption("Missing phone", QualityFilter.MissingPhone), new QualityOption("Incomplete", QualityFilter.Incomplete),
        new QualityOption("Flagged for review", QualityFilter.NeedsReview), new QualityOption("Possible duplicate", QualityFilter.FlaggedDuplicate), new QualityOption("Clean records", QualityFilter.Clean),
    };

    [ObservableProperty] private string _search = "";
    [ObservableProperty] private string _statusFilter = AllStatuses;
    [ObservableProperty] private QualityOption _selectedQuality;
    [ObservableProperty] private string _sortBy = "UpdatedAt";
    [ObservableProperty] private bool _descending = true;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private bool _allSelected;
    [ObservableProperty] private ContactRow? _selectedRow;

    public string PageText => $"Page {Page} of {Math.Max(1, TotalPages)}  -  {TotalCount:N0} contact(s)";
    public bool HasNoResults => TotalCount == 0 && !IsBusy;

    partial void OnSearchChanged(string value) => _ = DebouncedReloadAsync();
    partial void OnStatusFilterChanged(string value) { if (!_suppress) _ = ResetAndReloadAsync(); }
    partial void OnSelectedQualityChanged(QualityOption value) { if (!_suppress) _ = ResetAndReloadAsync(); }
    partial void OnPageChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnTotalCountChanged(int value) { OnPropertyChanged(nameof(PageText)); OnPropertyChanged(nameof(HasNoResults)); }

    partial void OnAllSelectedChanged(bool value) { foreach (var r in Rows) r.IsSelected = value; }

    public override async Task ActivateAsync()
    {
        _suppress = true;
        var statuses = await _repo.GetDistinctAsync(ContactField.Status);
        var keep = StatusFilter;
        StatusOptions.Clear(); StatusOptions.Add(AllStatuses);
        foreach (var s in new[] { "Lead", "Prospect", "Customer", "Inactive" }.Concat(statuses).Distinct()) StatusOptions.Add(s);
        StatusFilter = StatusOptions.Contains(keep) ? keep : AllStatuses;
        _suppress = false;
        await ReloadAsync();
    }

    private async Task DebouncedReloadAsync()
    {
        _searchCts?.Cancel(); var cts = _searchCts = new CancellationTokenSource();
        try { await Task.Delay(350, cts.Token); Page = 1; await ReloadAsync(); } catch (OperationCanceledException) { }
    }

    private async Task ResetAndReloadAsync() { Page = 1; await ReloadAsync(); }

    private ContactQuery BuildQuery() => new(string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(), StatusFilter == AllStatuses ? null : StatusFilter, null,
        SelectedQuality.Filter, SortBy, Descending, Page, Ui.Settings.Current.PageSize);

    [RelayCommand]
    public async Task ReloadAsync()
    {
        PagedResult<Contact>? result = null;
        await RunAsync("Loading contacts", async () => result = await _repo.QueryAsync(BuildQuery()));
        if (result is null) return;
        Rows.Clear();
        foreach (var c in result.Items) { var row = new ContactRow(c); row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ContactRow.IsSelected)) SelectedCount = Rows.Count(r => r.IsSelected); }; Rows.Add(row); }
        Page = result.Page; TotalPages = result.TotalPages; TotalCount = result.TotalCount;
        _suppress = true; AllSelected = false; _suppress = false; SelectedCount = 0;
        OnPropertyChanged(nameof(HasNoResults));
    }

    [RelayCommand] private Task Sort(string? column)
    {
        if (string.IsNullOrEmpty(column) || !ContactQuery.SortableColumns.Contains(column)) return Task.CompletedTask;
        if (SortBy == column) Descending = !Descending; else { SortBy = column; Descending = false; }
        return ResetAndReloadAsync();
    }
    [RelayCommand] private async Task NextPage() { if (Page < TotalPages) { Page++; await ReloadAsync(); } }
    [RelayCommand] private async Task PreviousPage() { if (Page > 1) { Page--; await ReloadAsync(); } }
    [RelayCommand] private async Task FirstPage() { Page = 1; await ReloadAsync(); }
    [RelayCommand] private async Task LastPage() { Page = Math.Max(1, TotalPages); await ReloadAsync(); }
    [RelayCommand] private async Task ClearFilters() { _suppress = true; Search = ""; StatusFilter = AllStatuses; SelectedQuality = QualityOptions[0]; _suppress = false; await ResetAndReloadAsync(); }

    private async Task OpenEditorAsync(Contact? contact, bool readOnly)
    {
        var vm = new ContactEditViewModel(Ui, _repo, _automation, contact, readOnly);
        await vm.LoadAsync();
        Ui.Dialogs.ShowWindow(new ContactEditWindow(vm));
        if (vm.WasSaved) await ReloadAsync();
    }

    [RelayCommand] private Task Add() => OpenEditorAsync(null, false);
    [RelayCommand] private async Task Edit(ContactRow? row) { if (row is null) return; var full = await _repo.GetAsync(row.Id) ?? row.Contact; await OpenEditorAsync(full, false); }
    [RelayCommand] private async Task View(ContactRow? row) { if (row is null) return; var full = await _repo.GetAsync(row.Id) ?? row.Contact; await OpenEditorAsync(full, true); }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        var ids = Rows.Where(r => r.IsSelected).Select(r => r.Id).ToList();
        if (ids.Count == 0 && SelectedRow is not null) ids.Add(SelectedRow.Id);
        if (ids.Count == 0) { Ui.Dialogs.Info("Nothing selected", "Tick the contacts you want to delete (or click a row) first."); return; }
        var names = string.Join(", ", Rows.Where(r => ids.Contains(r.Id)).Take(3).Select(r => r.Name));
        if (!Ui.Dialogs.Confirm("Delete contacts?", $"You are about to delete {ids.Count} contact(s): {names}{(ids.Count > 3 ? ", ..." : "")}.\n\nA backup is taken first and the full records are kept in the audit log, so this can be undone from Activity Logs.", "Delete", danger: true)) return;
        await RunAsync("Deleting contacts", async () =>
        {
            if (Ui.Settings.Current.AutoBackupBeforeBulk && ids.Count > 1) await _backup.CreateAsync(BackupKind.PreOperation, $"Before deleting {ids.Count} contacts");
            var n = await _repo.DeleteAsync(ids);
            Ui.Notifications.Add(NotificationLevel.Success, "Contacts deleted", $"{n} contact(s) deleted.");
        }, AuditOperation.ContactDeleted);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task Export(string? formatName)
    {
        var format = Enum.TryParse<ExportFormat>(formatName, out var f) ? f : ExportFormat.Excel;
        var selected = Rows.Where(r => r.IsSelected).Select(r => r.Id).ToList();
        var scope = selected.Count > 0 ? $"{selected.Count} selected contact(s)" : $"all {TotalCount:N0} contact(s) matching the current filter";
        var filter = format switch { ExportFormat.Excel => "Excel workbook (*.xlsx)|*.xlsx", ExportFormat.Csv => "CSV file (*.csv)|*.csv", _ => "PDF document (*.pdf)|*.pdf" };
        var path = Ui.Dialogs.PickSaveFile($"contacts-{DateTime.Now:yyyyMMdd-HHmm}{ReportExporter.Extension(format)}", filter, Ui.Settings.Current.ExportFolder);
        if (path is null) return;
        await RunAsync("Exporting contacts", async () =>
        {
            List<Contact> list;
            if (selected.Count > 0) list = await _repo.GetByIdsAsync(selected);
            else { var ids = await _repo.QueryIdsAsync(BuildQuery() with { Page = 1 }); list = await _repo.GetByIdsAsync(ids); list = list.OrderBy(c => ids.IndexOf(c.Id)).ToList(); }
            await _reports.ExportContactsAsync(list, format, path);
            Ui.Notifications.Add(NotificationLevel.Success, "Export complete", $"{list.Count:N0} contact(s) saved to {Path.GetFileName(path)} ({scope}).");
            if (Ui.Dialogs.Confirm("Export complete", $"{list.Count:N0} contact(s) were saved to:\n{path}\n\nOpen the file now?", "Open")) Ui.Dialogs.OpenInShell(path);
        }, AuditOperation.Export);
    }
}
