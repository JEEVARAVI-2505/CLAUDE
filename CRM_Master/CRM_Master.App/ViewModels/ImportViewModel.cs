using System.Collections.ObjectModel;
using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Engine.Import;
using CRM_Master.Engine.Reporting;

namespace CRM_Master.App.ViewModels;

public sealed record PolicyOption(string Label, ImportDuplicatePolicy Policy, string Description);

/// <summary>Import wizard: file -> preview -> column mapping -> dry run -> import with progress and cancellation -> history.</summary>
public sealed partial class ImportViewModel : ViewModelBase
{
    private readonly ImportService _import;
    private readonly ReportService _reports;
    private CancellationTokenSource? _cts;

    public ImportViewModel(UiServices ui, ImportService import, ReportService reports) : base(ui)
    {
        _import = import; _reports = reports;
        _selectedPolicy = PolicyOptions[0];
    }

    public IReadOnlyList<PolicyOption> PolicyOptions { get; } = new[]
    {
        new PolicyOption("Skip duplicates", ImportDuplicatePolicy.Skip, "Rows whose email or phone already exists are not imported (they are listed in the import report)."),
        new PolicyOption("Import and flag for review", ImportDuplicatePolicy.ImportAndFlag, "Duplicates are imported but flagged so you can review them in Duplicate Detection."),
        new PolicyOption("Propose an update to the existing contact", ImportDuplicatePolicy.ProposeUpdate, "Nothing is changed; the incoming values are stored so you can review them in Bulk Update."),
    };

    public ObservableCollection<MappingRow> MappingRows { get; } = new();
    public ObservableCollection<string> SheetNames { get; } = new();
    public ObservableCollection<PreviewRow> PreviewRows { get; } = new();
    public ObservableCollection<ImportJob> History { get; } = new();
    public ObservableCollection<string> MappingErrors { get; } = new();

    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private string? _fileInfoText;
    [ObservableProperty] private string? _selectedSheet;
    [ObservableProperty] private DataView? _filePreview;
    [ObservableProperty] private bool _cleanOnImport = true;
    [ObservableProperty] private PolicyOption _selectedPolicy;
    [ObservableProperty] private ImportPreview? _preview;
    [ObservableProperty] private ImportResult? _result;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressText;
    [ObservableProperty] private bool _isImporting;
    private bool _loading;

    public bool HasFile => FilePath is not null;
    public bool HasPreview => Preview is not null && Preview.MappingErrors.Count == 0;
    public bool HasResult => Result is not null;
    public bool HasSheets => SheetNames.Count > 1;
    public bool CanImport => HasFile && !IsImporting && !IsBusy;

    partial void OnFilePathChanged(string? value) { OnPropertyChanged(nameof(HasFile)); OnPropertyChanged(nameof(CanImport)); }
    partial void OnPreviewChanged(ImportPreview? value) => OnPropertyChanged(nameof(HasPreview));
    partial void OnResultChanged(ImportResult? value) => OnPropertyChanged(nameof(HasResult));
    partial void OnIsImportingChanged(bool value) => OnPropertyChanged(nameof(CanImport));
    partial void OnSelectedSheetChanged(string? value) { if (!_loading && FilePath is not null && value is not null) _ = LoadFileAsync(FilePath, value); }

    public override async Task ActivateAsync()
    {
        CleanOnImport = Ui.Settings.Current.CleanOnImport;
        SelectedPolicy = PolicyOptions.First(p => p.Policy == Ui.Settings.Current.ImportDuplicatePolicy);
        await RefreshHistoryAsync();
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var path = Ui.Dialogs.PickOpenFile("Spreadsheets and CSV (*.xlsx;*.xlsm;*.csv;*.tsv;*.txt)|*.xlsx;*.xlsm;*.csv;*.tsv;*.txt|All files (*.*)|*.*", "Choose a file to import");
        if (path is not null) await LoadFileAsync(path, null);
    }

    /// <summary>Called by the drop zone (drag-and-drop).</summary>
    [RelayCommand]
    private Task LoadDropped(string? path) => path is null ? Task.CompletedTask : LoadFileAsync(path, null);

    public async Task LoadFileAsync(string path, string? sheet)
    {
        await RunAsync("Reading file", async () =>
        {
            if (!TableReader.IsSupported(path)) throw new ImportException($"'{Path.GetExtension(path)}' files are not supported. Use .xlsx, .xlsm or .csv.");
            var table = await TableReader.ReadAsync(path, 200, sheet);
            _loading = true;
            FilePath = path; Result = null; Preview = null; PreviewRows.Clear(); MappingErrors.Clear(); Progress = 0; ProgressText = null;
            var sheets = TableReader.SheetNames(path);
            SheetNames.Clear(); foreach (var s in sheets) SheetNames.Add(s);
            SelectedSheet = table.SheetName; OnPropertyChanged(nameof(HasSheets));
            _loading = false;

            var size = new FileInfo(path).Length;
            FileInfoText = $"{Path.GetFileName(path)}  -  {FormatSize(size)}  -  {table.Headers.Count} column(s)  -  {(table.IsTruncated ? "first 200 rows shown" : table.Rows.Count + " row(s)")}";
            MappingRows.Clear();
            foreach (var m in ColumnMapper.Suggest(table)) MappingRows.Add(new MappingRow(m, table));
            FilePreview = BuildPreviewTable(table);
        });
    }

    private static DataView BuildPreviewTable(ImportTable t)
    {
        var dt = new DataTable();
        for (var i = 0; i < t.Headers.Count; i++) dt.Columns.Add(new DataColumn(t.Headers[i].Replace('.', '_'), typeof(string)) { Caption = t.Headers[i] });
        foreach (var row in t.Rows.Take(8)) dt.Rows.Add(row.Select(c => (object?)c ?? DBNull.Value).ToArray());
        return dt.DefaultView;
    }

    private static string FormatSize(long b) => b < 1024 ? $"{b} B" : b < 1024 * 1024 ? $"{b / 1024.0:0.#} KB" : $"{b / 1048576.0:0.#} MB";

    [RelayCommand]
    private async Task ResetMapping()
    {
        if (FilePath is null) return;
        await LoadFileAsync(FilePath, SelectedSheet);
    }

    private ImportOptions CurrentOptions() => new(MappingRows.Select(r => r.ToMapping()).ToList(), CleanOnImport, SelectedPolicy.Policy, SelectedSheet);

    [RelayCommand]
    private async Task PreviewImport()
    {
        if (FilePath is null) return;
        await RunAsync("Checking import", async () =>
        {
            var p = await _import.PreviewAsync(FilePath, CurrentOptions());
            MappingErrors.Clear(); foreach (var e in p.MappingErrors) MappingErrors.Add(e);
            Preview = p;
            PreviewRows.Clear(); foreach (var r in p.Sample) PreviewRows.Add(r);
        });
    }

    [RelayCommand]
    private async Task StartImport()
    {
        if (FilePath is null) return;
        var opts = CurrentOptions();
        var errors = ColumnMapper.Validate(opts.Mapping);
        MappingErrors.Clear(); foreach (var e in errors) MappingErrors.Add(e);
        if (errors.Count > 0) { Ui.Dialogs.Error("Check the column mapping", string.Join("\n", errors)); return; }

        var scope = Preview is null ? "all rows in the file" : $"{Preview.Importable:N0} new row(s) ({Preview.DuplicateExisting + Preview.DuplicateInFile:N0} duplicate(s) handled as '{SelectedPolicy.Label}')";
        if (!Ui.Dialogs.Confirm("Start import?", $"Import {scope} from \"{Path.GetFileName(FilePath)}\"?\n\nThe import runs as one transaction: if you cancel or anything fails, no contacts are added. A database backup is taken first when you already have contacts.", "Import")) return;

        _cts = new CancellationTokenSource(); IsImporting = true; Result = null; Progress = 0;
        var progress = new Progress<ProgressInfo>(p => { Progress = p.Fraction; ProgressText = p.Total > 0 ? $"{p.Stage}: {p.Done:N0} of {p.Total:N0}" : p.Stage; });
        ImportResult? result = null;
        await RunAsync("Importing contacts", async () => result = await _import.ImportAsync(FilePath, opts, progress, _cts.Token), AuditOperation.Import);
        IsImporting = false; _cts.Dispose(); _cts = null;
        if (result is null) return;

        Result = result; Progress = result.Status == ImportJobStatus.Cancelled ? 0 : 1;
        ProgressText = null;
        switch (result.Status)
        {
            case ImportJobStatus.Failed: Ui.Notifications.Add(NotificationLevel.Error, "Import failed", result.Error ?? "Unknown error"); Ui.Dialogs.Error("Import failed", result.Error ?? "The import could not be completed."); break;
            case ImportJobStatus.Cancelled: Ui.Notifications.Add(NotificationLevel.Warning, "Import cancelled", "No contacts were added."); break;
            default:
                Ui.Notifications.Add(NotificationLevel.Success, "Import finished", $"{result.Imported:N0} contact(s) imported from {Path.GetFileName(FilePath)} in {result.Elapsed.TotalSeconds:0.#}s.");
                if (result.Proposed > 0) Ui.Notifications.Add(NotificationLevel.Info, "Update proposals stored", $"{result.Proposed:N0} row(s) match existing contacts. Review them in Bulk Update.");
                break;
        }
        await RefreshHistoryAsync();
    }

    [RelayCommand] private void CancelImport() { _cts?.Cancel(); ProgressText = "Cancelling - rolling back..."; }

    [RelayCommand]
    private async Task RefreshHistory()
    {
        var jobs = await _import.HistoryAsync(50);
        History.Clear(); foreach (var j in jobs) History.Add(j);
    }
    private Task RefreshHistoryAsync() => RefreshHistory();

    [RelayCommand]
    private async Task OpenReport(ImportJob? job)
    {
        if (job is null) return;
        await RunAsync("Creating import report", async () =>
        {
            var path = await _reports.GenerateImportSummaryAsync(job.Id);
            if (path is not null && Ui.Dialogs.Confirm("Import report saved", $"The report was saved to:\n{path}\n\nOpen it now?", "Open")) Ui.Dialogs.OpenInShell(path);
        }, AuditOperation.Export);
    }

    [RelayCommand] private void GoToDuplicates() => Ui.Navigation.Navigate(Module.Duplicates);
    [RelayCommand] private void GoToBulkUpdate() => Ui.Navigation.Navigate(Module.BulkUpdate);
    [RelayCommand] private void GoToContacts() => Ui.Navigation.Navigate(Module.Contacts);
}
