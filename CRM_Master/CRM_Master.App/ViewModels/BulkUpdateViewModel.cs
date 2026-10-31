using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Engine;
using CRM_Master.Engine.BulkUpdate;
using CRM_Master.Engine.Import;
using CRM_Master.Engine.Reporting;

namespace CRM_Master.App.ViewModels;

public sealed record PolicyChoice(string Label, BulkUpdatePolicy Policy, string Description);
public sealed record MatchChoice(string Label, MatchBy Match);

/// <summary>A field the user can include in the update.</summary>
public sealed partial class FieldOption : ObservableObject
{
    public FieldOption(ContactField f) { Field = f; Label = Contact.LabelOf(f); }
    public ContactField Field { get; }
    public string Label { get; }
    [ObservableProperty] private bool _isSelected = true;
}

/// <summary>One proposed field change; <see cref="Approved"/> is the user's tick-box.</summary>
public sealed partial class ProposalRow : ObservableObject
{
    private readonly FieldProposal _p; private readonly Action _changed;
    public ProposalRow(RecordProposal rec, FieldProposal p, Action changed) { Record = rec; _p = p; _changed = changed; _approved = p.Approved; }
    public RecordProposal Record { get; }
    public int ContactId => Record.ContactId;
    public string ContactName => Record.ContactName;
    public int RowNumber => Record.RowNumber;
    public string MatchedBy => Record.MatchedBy;
    public string Field => _p.Label;
    public string? Current => _p.Current;
    public string? Incoming => _p.Incoming;
    public string Kind => Record.Skipped ? "Skipped record" : _p.Kind switch
    {
        ProposalKind.FillEmpty => "Fill empty", ProposalKind.Overwrite => "Replace", ProposalKind.KeptExisting => "Kept existing", ProposalKind.Conflict => "Conflict",
        ProposalKind.RejectedInvalid => "Invalid - rejected", ProposalKind.Same => "Same", _ => "Incoming empty"
    };
    public string Reason => Record.Skipped ? Record.SkipReason ?? "" : _p.Reason;
    public bool IsActionable => !Record.Skipped && _p.IsActionable;
    [ObservableProperty] private bool _approved;
    partial void OnApprovedChanged(bool value) { _p.Approved = value; _changed(); }
    public void Set(bool v) { if (IsActionable) Approved = v; }
}

/// <summary>Bulk update: propose -> review field by field -> apply approved changes -> report / undo. Nothing is overwritten unseen.</summary>
public sealed partial class BulkUpdateViewModel : ViewModelBase
{
    private readonly BulkUpdateService _svc;
    private readonly ImportService _import;
    private readonly ReportService _reports;
    private readonly BatchUndoService _undo;
    private BulkUpdatePlan? _plan;

    public BulkUpdateViewModel(UiServices ui, BulkUpdateService svc, ImportService import, ReportService reports, BatchUndoService undo) : base(ui)
    {
        _svc = svc; _import = import; _reports = reports; _undo = undo;
        foreach (var f in Contact.Fields) FieldOptions.Add(new FieldOption(f.Field));
        _selectedPolicy = Policies[0]; _selectedMatch = Matches[0];
    }

    public IReadOnlyList<PolicyChoice> Policies { get; } = new[]
    {
        new PolicyChoice("Update empty fields only", BulkUpdatePolicy.EmptyFieldsOnly, "Fills blanks in the selected fields. Existing values are never changed."),
        new PolicyChoice("Update all selected fields", BulkUpdatePolicy.AllSelectedFields, "Replaces existing values in the selected fields with the incoming (non-blank) values."),
        new PolicyChoice("Preserve existing values", BulkUpdatePolicy.PreserveExisting, "Existing values always win. Only blanks are filled - in every field, not just the selected ones."),
        new PolicyChoice("Update only if incoming values are valid", BulkUpdatePolicy.OnlyIfValid, "Like 'all selected fields', but an incoming email, phone, website, date or country must pass validation."),
        new PolicyChoice("Skip conflicting records", BulkUpdatePolicy.SkipConflicts, "A record where any field would replace a different existing value is skipped entirely."),
        new PolicyChoice("Require manual approval", BulkUpdatePolicy.ManualApproval, "Everything is proposed but nothing is ticked - you approve each change yourself."),
    };
    public IReadOnlyList<MatchChoice> Matches { get; } = new[]
    {
        new MatchChoice("Email, then phone", MatchBy.EmailThenPhone), new MatchChoice("Email only", MatchBy.EmailOnly), new MatchChoice("Phone only", MatchBy.PhoneOnly)
    };

    public ObservableCollection<MappingRow> MappingRows { get; } = new();
    public ObservableCollection<FieldOption> FieldOptions { get; } = new();
    public ObservableCollection<ProposalRow> Proposals { get; } = new();
    public ObservableCollection<UnmatchedRow> Unmatched { get; } = new();
    public ObservableCollection<ImportJob> ProposalSources { get; } = new();

    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private string? _fileInfoText;
    [ObservableProperty] private PolicyChoice _selectedPolicy;
    [ObservableProperty] private MatchChoice _selectedMatch;
    [ObservableProperty] private ImportJob? _selectedSource;
    [ObservableProperty] private bool _hasPlan;
    [ObservableProperty] private bool _showAll;
    [ObservableProperty] private string _summary = "";
    [ObservableProperty] private string? _resultText;
    [ObservableProperty] private string? _lastBatchId;
    [ObservableProperty] private bool _canUndo;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressText;

    public bool HasFile => FilePath is not null;
    public int ApprovedCount => _plan?.ApprovedFieldChanges ?? 0;
    public string ApprovedText => $"{ApprovedCount:N0} field change(s) approved";
    public bool CanApply => HasPlan && ApprovedCount > 0 && !IsBusy;
    partial void OnFilePathChanged(string? value) => OnPropertyChanged(nameof(HasFile));
    partial void OnShowAllChanged(bool value) => RebuildRows();
    partial void OnHasPlanChanged(bool value) => OnPropertyChanged(nameof(CanApply));

    public override async Task ActivateAsync()
    {
        var jobs = await _import.HistoryAsync(40);
        ProposalSources.Clear();
        foreach (var j in jobs) if ((await _import.RecordsAsync(j.Id, ImportRecordStatus.Proposed, 1)).Count > 0) ProposalSources.Add(j);
        SelectedSource ??= ProposalSources.FirstOrDefault();
    }

    [RelayCommand]
    private async Task Browse()
    {
        var path = Ui.Dialogs.PickOpenFile("Spreadsheets and CSV (*.xlsx;*.xlsm;*.csv;*.tsv;*.txt)|*.xlsx;*.xlsm;*.csv;*.tsv;*.txt|All files (*.*)|*.*", "Choose the file with updated contacts");
        if (path is null) return;
        await RunAsync("Reading file", async () =>
        {
            var table = await TableReader.ReadAsync(path, 200);
            FilePath = path; FileInfoText = $"{Path.GetFileName(path)}  -  {table.Headers.Count} column(s)";
            MappingRows.Clear(); foreach (var m in ColumnMapper.Suggest(table)) MappingRows.Add(new MappingRow(m, table));
            ClearPlan();
        });
    }

    private void ClearPlan() { _plan = null; HasPlan = false; Proposals.Clear(); Unmatched.Clear(); Summary = ""; ResultText = null; NotifyApproved(); }

    private IReadOnlyCollection<ContactField> SelectedFields() => FieldOptions.Where(f => f.IsSelected).Select(f => f.Field).ToList();

    [RelayCommand]
    private async Task BuildProposal()
    {
        if (FilePath is null) { Ui.Dialogs.Info("Choose a file", "Select the spreadsheet with the updated contacts first."); return; }
        var fields = SelectedFields();
        if (fields.Count == 0 && SelectedPolicy.Policy != BulkUpdatePolicy.PreserveExisting) { Ui.Dialogs.Info("Select fields", "Tick at least one field to update."); return; }
        var progress = new Progress<ProgressInfo>(p => { Progress = p.Fraction; ProgressText = $"{p.Stage}: {p.Done:N0} of {p.Total:N0}"; });
        BulkUpdatePlan? plan = null;
        var ok = await RunAsync("Comparing with existing contacts", async () =>
            plan = await _svc.PlanFromFileAsync(FilePath, MappingRows.Select(r => r.ToMapping()).ToList(), SelectedPolicy.Policy, fields, SelectedMatch.Match, null, progress), AuditOperation.BulkUpdate);
        ProgressText = null; Progress = 0;
        if (ok && plan is not null) ShowPlan(plan);
    }

    [RelayCommand]
    private async Task LoadFromImport()
    {
        if (SelectedSource is null) { Ui.Dialogs.Info("No proposals", "Import a file with the policy 'Propose an update to the existing contact' first, then choose that import here."); return; }
        var fields = SelectedFields(); var job = SelectedSource;
        BulkUpdatePlan? plan = null;
        var ok = await RunAsync("Loading proposals", async () => plan = await _svc.PlanFromImportProposalsAsync(job.Id, SelectedPolicy.Policy, fields), AuditOperation.BulkUpdate);
        if (ok && plan is not null) { FilePath = null; ShowPlan(plan); }
    }

    private void ShowPlan(BulkUpdatePlan plan)
    {
        _plan = plan; HasPlan = true; ResultText = null; CanUndo = false;
        Summary = $"{plan.IncomingRows:N0} incoming row(s): {plan.Matched:N0} matched an existing contact, {plan.Unmatched.Count:N0} did not match. " +
                  $"{plan.WithChanges:N0} contact(s) have proposed changes, {plan.Unchanged:N0} are already up to date, {plan.Skipped:N0} skipped. {plan.ProposedFieldChanges:N0} field change(s) proposed.";
        Unmatched.Clear(); foreach (var u in plan.Unmatched.Take(2000)) Unmatched.Add(u);
        RebuildRows();
    }

    private void RebuildRows()
    {
        Proposals.Clear();
        if (_plan is null) { NotifyApproved(); return; }
        var count = 0;
        foreach (var rec in _plan.Records)
            foreach (var f in rec.Fields)
            {
                var interesting = f.IsActionable || f.Kind is ProposalKind.Conflict or ProposalKind.KeptExisting or ProposalKind.RejectedInvalid;
                if (!ShowAll && !interesting) continue;
                if (++count > 20000) break;
                Proposals.Add(new ProposalRow(rec, f, NotifyApproved));
            }
        NotifyApproved();
    }

    private void NotifyApproved() { OnPropertyChanged(nameof(ApprovedCount)); OnPropertyChanged(nameof(ApprovedText)); OnPropertyChanged(nameof(CanApply)); }

    [RelayCommand] private void ApproveAll() { foreach (var r in Proposals) r.Set(true); }
    [RelayCommand] private void RejectAll() { foreach (var r in Proposals) r.Set(false); }

    [RelayCommand]
    private async Task Apply()
    {
        if (_plan is null) return;
        var msg = $"Apply {ApprovedCount:N0} approved field change(s)?\n\nOnly the changes you approved are written. A backup is taken first, the previous and new value of every change is recorded, and the whole update can be undone. Values edited since the proposal was built are skipped.";
        if (!Ui.Dialogs.Confirm("Apply approved changes?", msg, "Apply changes")) return;
        var progress = new Progress<ProgressInfo>(p => { Progress = p.Fraction; ProgressText = $"{p.Stage}: {p.Done:N0} of {p.Total:N0}"; });
        BulkUpdateResult? res = null;
        var ok = await RunAsync("Applying updates", async () => res = await _svc.ApplyAsync(_plan, progress), AuditOperation.BulkUpdate);
        ProgressText = null; Progress = 0;
        if (!ok || res is null) return;
        LastBatchId = res.BatchId; CanUndo = res.ContactsUpdated > 0;
        ResultText = $"Updated {res.ContactsUpdated:N0} contact(s), {res.FieldsChanged:N0} field(s)." + (res.SkippedBecauseChanged > 0 ? $" {res.SkippedBecauseChanged:N0} value(s) skipped because they were edited meanwhile." : "") + (res.BackupPath is null ? "" : " Backup: " + res.BackupPath);
        Ui.Notifications.Add(NotificationLevel.Success, "Bulk update applied", ResultText);
        _plan = null; HasPlan = false; Proposals.Clear(); NotifyApproved();
    }

    [RelayCommand]
    private async Task UndoLast()
    {
        if (LastBatchId is null) return;
        if (!Ui.Dialogs.Confirm("Undo bulk update?", "Restores the previous values recorded for this update. Anything edited afterwards is left alone.", "Undo")) return;
        UndoResult? res = null;
        if (!await RunAsync("Undoing bulk update", async () => res = await _undo.UndoAsync(LastBatchId), AuditOperation.BulkUpdate) || res is null) return;
        CanUndo = false; ResultText = $"Undone: {res.Reverted:N0} contact(s) reverted" + (res.Conflicts > 0 ? $", {res.Conflicts:N0} field(s) left because they were edited later." : ".");
    }

    [RelayCommand]
    private async Task ExportReport(string? formatName)
    {
        if (LastBatchId is null) { Ui.Dialogs.Info("No update yet", "Apply an update first; the report lists every change made by it."); return; }
        var format = Enum.TryParse<ExportFormat>(formatName, out var f) ? f : ExportFormat.Excel;
        var filter = format switch { ExportFormat.Excel => "Excel workbook (*.xlsx)|*.xlsx", ExportFormat.Csv => "CSV file (*.csv)|*.csv", _ => "PDF document (*.pdf)|*.pdf" };
        var path = Ui.Dialogs.PickSaveFile(_reports.SuggestedFileName(ReportType.BulkUpdate, format), filter, Ui.Settings.Current.ExportFolder);
        if (path is null) return;
        await RunAsync("Exporting update report", async () =>
        {
            var data = await _reports.BuildAsync(ReportType.BulkUpdate, new(BatchId: LastBatchId));
            await _reports.ExportAsync(data, format, path);
            if (Ui.Dialogs.Confirm("Report saved", $"Saved to:\n{path}\n\nOpen it now?", "Open")) Ui.Dialogs.OpenInShell(path);
        }, AuditOperation.Export);
    }
}
