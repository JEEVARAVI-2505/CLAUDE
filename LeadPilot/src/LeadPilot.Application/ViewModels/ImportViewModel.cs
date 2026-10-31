using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Services;

namespace LeadPilot.Application.ViewModels;

/// <summary>One target field and the file column mapped to it.</summary>
public sealed partial class MappingRow : ObservableObject
{
    public const string None = "(not mapped)";
    [ObservableProperty] private string _column = None;
    public ImportField Field { get; init; }
    public string Label { get; init; } = "";
    public bool Required { get; init; }
}

public sealed record PreviewRow(IReadOnlyList<string> Cells);
public sealed record ImportHistoryRow(string When, string File, int Rows, int Imported, int Duplicates, int Errors);

/// <summary>Import wizard: select file, preview, map columns, validate, confirm, report.</summary>
public sealed partial class ImportViewModel : ViewModelBase
{
    private readonly ImportService _import;
    private readonly ISystemRepository _sys;
    private readonly ICurrentUser _user;
    private readonly IDialogService _dlg;
    private readonly INavigator _nav;
    private ImportPreview? _preview;

    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private bool _hasPreview;
    [ObservableProperty] private string _fileNote = "";
    [ObservableProperty] private string _validationSummary = "";
    [ObservableProperty] private string _reportSummary = "";
    [ObservableProperty] private bool _hasReport;
    [ObservableProperty] private bool _validated;

    public ObservableCollection<string> Columns { get; } = new();
    public ObservableCollection<PreviewRow> Rows { get; } = new();
    public ObservableCollection<MappingRow> Mapping { get; } = new();
    public ObservableCollection<string> Issues { get; } = new();
    public ObservableCollection<string> ReportLines { get; } = new();
    public ObservableCollection<ImportHistoryRow> History { get; } = new();

    public ImportViewModel(ImportService import, ISystemRepository sys, ICurrentUser user, IDialogService dlg, INavigator nav)
    { _import = import; _sys = sys; _user = user; _dlg = dlg; _nav = nav; }

    public override Task OnNavigatedToAsync(object? parameter) => RunAsync(async () =>
    {
        var h = await Task.Run(() => _sys.GetImports(20));
        History.Clear();
        foreach (var i in h) History.Add(new ImportHistoryRow(i.ImportedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"), i.FileName, i.TotalRows, i.Imported, i.Duplicates, i.Errors));
    });

    [RelayCommand]
    private Task Choose()
    {
        var p = _dlg.PickOpenFile("Choose a spreadsheet to import", "Excel or CSV (*.xlsx;*.csv)|*.xlsx;*.csv|All files (*.*)|*.*");
        return p == null ? Task.CompletedTask : LoadFileAsync(p);
    }

    /// <summary>Reads a file and prepares the preview and the suggested mapping.</summary>
    public Task LoadFileAsync(string path) => RunAsync(async () =>
    {
        HasReport = false; Validated = false; Issues.Clear(); ValidationSummary = "";
        _preview = await Task.Run(() => _import.Preview(path));
        FilePath = path; HasPreview = true;
        Columns.Clear(); Columns.Add(MappingRow.None); foreach (var h in _preview.Table.Headers) Columns.Add(h);
        Rows.Clear(); foreach (var r in _preview.Table.Rows.Take(8)) Rows.Add(new PreviewRow(r.Select(c => c.Length > 40 ? c[..40] + "…" : c).ToList()));
        Mapping.Clear();
        foreach (var f in Enum.GetValues<ImportField>())
        {
            var m = new MappingRow { Field = f, Label = Label(f), Required = f == ImportField.CompanyName };
            if (_preview.SuggestedMapping.TryGetValue(f, out var idx) && idx < _preview.Table.Headers.Count) m.Column = _preview.Table.Headers[idx];
            Mapping.Add(m);
        }
        FileNote = $"{_preview.Table.Rows.Count:N0} data row(s), {_preview.Table.Headers.Count} column(s)." + (_preview.AlreadyImported ? " ⚠ This exact file was imported before – importing again will mostly find duplicates." : "");
    }, "Reading file…");

    private static string Label(ImportField f) => f switch
    {
        ImportField.CompanyName => "Company name *", ImportField.ContactName => "Contact person", ImportField.ContactTitle => "Job title", ImportField.IntentEvidence => "Notes / enquiry text",
        ImportField.CompanySize => "Company size", _ => string.Concat(f.ToString().Select((c, i) => i > 0 && char.IsUpper(c) ? " " + char.ToLowerInvariant(c) : c.ToString()))
    };

    private Dictionary<ImportField, int> CurrentMap()
    {
        var map = new Dictionary<ImportField, int>();
        if (_preview == null) return map;
        foreach (var m in Mapping)
        {
            var i = _preview.Table.Headers.ToList().IndexOf(m.Column);
            if (m.Column != MappingRow.None && i >= 0) map[m.Field] = i;
        }
        return map;
    }

    [RelayCommand]
    private Task Validate() => RunAsync(async () =>
    {
        if (_preview == null) return;
        var v = await Task.Run(() => _import.Validate(_preview.Table, CurrentMap()));
        Issues.Clear(); foreach (var e in v.Errors.Take(50)) Issues.Add("✖ " + e); foreach (var w in v.Warnings.Take(50)) Issues.Add("⚠ " + w);
        ValidationSummary = v.Errors.Count > 0 && v.ValidRows == 0 ? v.Errors[0] : $"{v.ValidRows:N0} of {v.TotalRows:N0} row(s) can be imported · {v.Errors.Count} error(s) · {v.Warnings.Count} warning(s). Rows with errors are skipped; warnings are imported but flagged.";
        Validated = v.ValidRows > 0;
    });

    [RelayCommand]
    private Task ConfirmImport() => RunAsync(async () =>
    {
        if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to import leads.");
        if (_preview == null) return;
        var map = CurrentMap();
        var rep = await Task.Run(() => _import.Import(_preview.Table, map, null));
        ReportSummary = $"{rep.Imported:N0} imported · {rep.DuplicatesLinked:N0} already existed (source added) · {rep.FlaggedForReview:N0} imported & flagged as possible duplicates · {rep.Skipped:N0} skipped · {rep.Errors:N0} error(s)";
        ReportLines.Clear(); foreach (var m in rep.Messages.Take(200)) ReportLines.Add(m);
        HasReport = true; Validated = false;
        var h = await Task.Run(() => _sys.GetImports(20));
        History.Clear(); foreach (var i in h) History.Add(new ImportHistoryRow(i.ImportedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"), i.FileName, i.TotalRows, i.Imported, i.Duplicates, i.Errors));
    }, "Importing…");

    [RelayCommand]
    private void SaveTemplate()
    {
        var path = _dlg.PickSaveFile("Save sample CSV template", "CSV (*.csv)|*.csv", "leadpilot-import-template.csv");
        if (path == null) return;
        File.WriteAllText(path, "Company Name,Contact Person,Job Title,Phone,Email,Website,Address,City,Industry,Notes\n\"Example Industries Pvt Ltd\",\"A. Kumar\",\"Purchase Manager\",\"+91 98765 43210\",\"info@example.in\",\"https://example.in\",\"12 Mount Road\",\"Chennai\",\"Manufacturing\",\"Asked for a quote on our website\"\n");
        StatusMessage = "Template saved: " + path;
    }

    [RelayCommand] private void GoDatabase() => _nav.Navigate(PageKey.LeadDatabase);
}
