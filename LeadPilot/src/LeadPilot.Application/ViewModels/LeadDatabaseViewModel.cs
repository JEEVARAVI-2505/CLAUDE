using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

/// <summary>Grid row with a selection flag.</summary>
public sealed partial class LeadRowItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    public LeadRow Row { get; init; } = new();
    public string Created => Row.CreatedAt.ToLocalTime().ToString("dd MMM yyyy");
    public string LastContacted => Row.LastContactedAt?.ToLocalTime().ToString("dd MMM yyyy") ?? "";
    public string NextFollowUp => Row.NextFollowUpAt?.ToLocalTime().ToString("dd MMM yyyy") ?? "";
    public string TemperatureText => $"{Row.Temperature} · {Row.Score}";
}

/// <summary>Searchable, filterable, sortable, paged lead grid with bulk actions and manual entry.</summary>
public sealed partial class LeadDatabaseViewModel : ViewModelBase
{
    private readonly ILeadRepository _leads;
    private readonly ISystemRepository _sys;
    private readonly LeadIngestService _ingest;
    private readonly ScoringService _scoring;
    private readonly DataGovernanceService _gov;
    private readonly IExportService _export;
    private readonly ICurrentUser _user;
    private readonly INavigator _nav;
    private readonly IDialogService _dlg;
    private bool _loading;
    private bool _loadedOnce;
    private LeadSort _sort = LeadSort.Created;
    private bool _desc = true;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _statusFilter = FilterOptionsHelper.All;
    [ObservableProperty] private string _temperatureFilter = FilterOptionsHelper.All;
    [ObservableProperty] private string _sourceFilter = FilterOptionsHelper.All;
    [ObservableProperty] private string _industryFilter = FilterOptionsHelper.All;
    [ObservableProperty] private string _cityFilter = FilterOptionsHelper.All;
    [ObservableProperty] private bool _onlyIncomplete;
    [ObservableProperty] private bool _showFilters;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _pageCount = 1;
    [ObservableProperty] private int _total;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private bool _allSelected;
    [ObservableProperty] private SalesUser? _assignTo;
    [ObservableProperty] private string _bulkStatus = nameof(LeadStatus.Contacted);
    [ObservableProperty] private int? _campaignId;
    // manual entry
    [ObservableProperty] private bool _isAdding;
    [ObservableProperty] private string _newCompany = "";
    [ObservableProperty] private string _newContact = "";
    [ObservableProperty] private string _newPhone = "";
    [ObservableProperty] private string _newEmail = "";
    [ObservableProperty] private string _newWebsite = "";
    [ObservableProperty] private string _newIndustry = "";
    [ObservableProperty] private string _newCity = "";
    [ObservableProperty] private string _newNotes = "";

    public ObservableCollection<LeadRowItem> Rows { get; } = new();
    public ObservableCollection<string> Statuses { get; } = new(FilterOptionsHelper.WithAll<LeadStatus>());
    public ObservableCollection<string> Temperatures { get; } = new(FilterOptionsHelper.WithAll<LeadTemperature>());
    public ObservableCollection<string> Sources { get; } = new() { FilterOptionsHelper.All };
    public ObservableCollection<string> Industries { get; } = new() { FilterOptionsHelper.All };
    public ObservableCollection<string> Cities { get; } = new() { FilterOptionsHelper.All };
    public ObservableCollection<SalesUser> Users { get; } = new();
    public IReadOnlyList<string> StatusChoices { get; } = Enum.GetNames<LeadStatus>();
    public bool IsEmpty => _loadedOnce && Rows.Count == 0;
    public string PageText => $"Page {Page} of {PageCount}  ·  {Total:N0} lead(s)";

    public LeadDatabaseViewModel(ILeadRepository leads, ISystemRepository sys, LeadIngestService ingest, ScoringService scoring, DataGovernanceService gov, IExportService export, ICurrentUser user, INavigator nav, IDialogService dlg)
    { _leads = leads; _sys = sys; _ingest = ingest; _scoring = scoring; _gov = gov; _export = export; _user = user; _nav = nav; _dlg = dlg; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        if (parameter is int campaign) CampaignId = campaign;
        else if (parameter is string text) SearchText = text;
        await LoadOptionsAsync();
        await ReloadAsync();
    }

    private async Task LoadOptionsAsync()
    {
        var (opts, users) = await Task.Run(() => (_leads.GetFilterOptions(), _sys.GetUsers().Where(u => u.IsActive).ToList()));
        Fill(Sources, opts.Sources); Fill(Industries, opts.Industries); Fill(Cities, opts.Cities);
        Users.Clear(); foreach (var u in users) Users.Add(u);
    }

    private static void Fill(ObservableCollection<string> target, IEnumerable<string> items) { target.Clear(); target.Add(FilterOptionsHelper.All); foreach (var i in items) target.Add(i); }

    private LeadQuery Query(int page, int size) => new()
    {
        Text = SearchText, Status = FilterOptionsHelper.Parse<LeadStatus>(StatusFilter), Temperature = FilterOptionsHelper.Parse<LeadTemperature>(TemperatureFilter),
        Source = StatusOrNull(SourceFilter), Industry = StatusOrNull(IndustryFilter), City = StatusOrNull(CityFilter), OnlyIncomplete = OnlyIncomplete, CampaignId = CampaignId,
        Sort = _sort, Descending = _desc, Page = page, PageSize = size
    };

    private static string? StatusOrNull(string s) => s == FilterOptionsHelper.All ? null : s;

    [RelayCommand]
    private Task Reload() => RunAsync(LoadRowsAsync);

    private async Task LoadRowsAsync()
    {
        _loading = true;
        try
        {
            var page = await Task.Run(() => _leads.Search(Query(Page, 50)));
            if (Page > page.PageCount) { Page = page.PageCount; page = await Task.Run(() => _leads.Search(Query(Page, 50))); }
            Rows.Clear();
            foreach (var r in page.Rows) { var item = new LeadRowItem { Row = r }; item.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(LeadRowItem.IsSelected)) SelectedCount = Rows.Count(x => x.IsSelected); }; Rows.Add(item); }
            Total = page.Total; PageCount = page.PageCount; SelectedCount = 0; AllSelected = false; _loadedOnce = true;
            OnPropertyChanged(nameof(PageText)); OnPropertyChanged(nameof(IsEmpty));
        }
        finally { _loading = false; }
    }

    private Task ReloadAsync() => ReloadCommand.ExecuteAsync(null);

    [RelayCommand] private Task Search() { Page = 1; return ReloadAsync(); }
    [RelayCommand] private void ToggleFilters() => ShowFilters = !ShowFilters;

    [RelayCommand]
    private Task ClearFilters()
    {
        SearchText = ""; StatusFilter = TemperatureFilter = SourceFilter = IndustryFilter = CityFilter = FilterOptionsHelper.All; OnlyIncomplete = false; CampaignId = null; Page = 1;
        return ReloadAsync();
    }

    [RelayCommand] private Task NextPage() { if (Page < PageCount) { Page++; return ReloadAsync(); } return Task.CompletedTask; }
    [RelayCommand] private Task PrevPage() { if (Page > 1) { Page--; return ReloadAsync(); } return Task.CompletedTask; }

    [RelayCommand]
    private Task SortBy(string column)
    {
        var s = column switch { "Company" => LeadSort.Company, "Score" => LeadSort.Score, "Status" => LeadSort.Status, "Created" => LeadSort.Created, "NextFollowUp" => LeadSort.NextFollowUp, "LastContacted" => LeadSort.LastContacted, _ => LeadSort.LeadId };
        _desc = s == _sort ? !_desc : s is LeadSort.Score or LeadSort.Created; _sort = s; Page = 1;
        return ReloadAsync();
    }

    partial void OnAllSelectedChanged(bool value) { if (!_loading) foreach (var r in Rows) r.IsSelected = value; }

    private List<int> SelectedIds() => Rows.Where(r => r.IsSelected).Select(r => r.Row.Id).ToList();

    [RelayCommand] private void Open(LeadRowItem? item) { if (item != null) _nav.Navigate(PageKey.LeadDetails, item.Row.Id); }

    [RelayCommand]
    private Task ApplyStatus() => RunAsync(async () =>
    {
        var ids = SelectedIds(); if (ids.Count == 0) { ErrorMessage = "Select leads first."; return; }
        if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to edit leads.");
        var status = Enum.Parse<LeadStatus>(BulkStatus);
        await Task.Run(() => _leads.BulkSetStatus(ids, status));
        StatusMessage = $"{ids.Count} lead(s) set to {status}.";
        await LoadRowsAsync();
    });

    [RelayCommand]
    private Task ApplyAssign() => RunAsync(async () =>
    {
        var ids = SelectedIds(); if (ids.Count == 0) { ErrorMessage = "Select leads first."; return; }
        if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to edit leads.");
        await Task.Run(() => _leads.BulkAssign(ids, AssignTo?.Id));
        StatusMessage = $"{ids.Count} lead(s) assigned to {AssignTo?.Name ?? "nobody"}.";
        await LoadRowsAsync();
    });

    [RelayCommand]
    private Task DeleteSelected() => RunAsync(async () =>
    {
        var ids = SelectedIds(); if (ids.Count == 0) { ErrorMessage = "Select leads first."; return; }
        if (!_user.Can(Permission.DeleteLeads)) throw new UnauthorizedAccessException("You do not have permission to delete leads.");
        if (!_dlg.Confirm("Delete leads", $"Permanently delete {ids.Count} lead(s) and their history? This cannot be undone (you can restore from a backup).")) return;
        await Task.Run(() => _gov.DeleteLeads(ids));
        StatusMessage = $"{ids.Count} lead(s) deleted.";
        await LoadOptionsAsync(); await LoadRowsAsync();
    });

    [RelayCommand]
    private Task ExportSelected() => RunAsync(async () =>
    {
        if (!_user.Can(Permission.Export)) throw new UnauthorizedAccessException("You do not have permission to export.");
        var ids = SelectedIds();
        var path = _dlg.PickSaveFile("Export leads", "Excel workbook (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv|PDF report (*.pdf)|*.pdf", ids.Count > 0 ? "selected-leads.xlsx" : "filtered-leads.xlsx");
        if (path == null) return;
        var rows = await Task.Run(() => _leads.Export(Query(1, 50), ids.Count > 0 ? ids : null));
        await Task.Run(() => ExportBy(path, rows));
        StatusMessage = $"Exported {rows.Count} lead(s) to {path}";
    });

    private void ExportBy(string path, IReadOnlyList<LeadRow> rows)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".csv": _export.ExportCsv(rows, path); break;
            case ".pdf": _export.ExportPdf(rows, path, "LeadPilot AI – leads"); break;
            default: _export.ExportXlsx(rows, path); break;
        }
    }

    [RelayCommand] private void StartAdd() { IsAdding = true; ErrorMessage = ""; }
    [RelayCommand] private void CancelAdd() { IsAdding = false; ClearNew(); }

    private void ClearNew() { NewCompany = NewContact = NewPhone = NewEmail = NewWebsite = NewIndustry = NewCity = NewNotes = ""; }

    [RelayCommand]
    private Task SaveNew() => RunAsync(async () =>
    {
        if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to add leads.");
        if (string.IsNullOrWhiteSpace(NewCompany)) { ErrorMessage = "Enter the company name."; return; }
        var cand = new ProspectCandidate
        {
            Name = NewCompany.Trim(), ContactName = Blank(NewContact), Phone = Blank(NewPhone), Email = Blank(NewEmail), Website = Blank(NewWebsite), Industry = Blank(NewIndustry), City = Blank(NewCity),
            IntentEvidence = Blank(NewNotes), SourceName = "Manual entry", SourceType = "Manual", RetrievedAt = DateTime.UtcNow
        };
        var res = await Task.Run(() => _ingest.Ingest(cand, new IngestContext { Profile = _scoring.GetActiveProfile(), UserId = _user.UserId, AssignedUserId = _user.UserId, SkipHighConfidenceDuplicates = false }));
        if (res.Status != IngestStatus.Created) { ErrorMessage = res.Message; return; }
        StatusMessage = res.Message; IsAdding = false; ClearNew();
        await LoadOptionsAsync(); await LoadRowsAsync();
    });

    private static string? Blank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    [RelayCommand] private void GoImport() => _nav.Navigate(PageKey.Import);
    [RelayCommand] private void GoFinder() => _nav.Navigate(PageKey.LeadFinder);
}
