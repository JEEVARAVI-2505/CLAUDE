using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Engine.Validation;

namespace CRM_Master.App.ViewModels;

public sealed record SeverityOption(string Label, Severity? Severity);

/// <summary>Configurable validation of the whole contact list, with suggested corrections you can apply one by one.</summary>
public sealed partial class ValidationViewModel : ViewModelBase
{
    private const int ShowLimit = 5000;
    private readonly IContactRepository _repo;
    private readonly IBackupService _backup;
    private List<ValidationIssue> _all = new();
    private CancellationTokenSource? _cts;

    public ValidationViewModel(UiServices ui, IContactRepository repo, IBackupService backup) : base(ui)
    { _repo = repo; _backup = backup; _selectedSeverity = SeverityOptions[0]; }

    public IReadOnlyList<SeverityOption> SeverityOptions { get; } = new[]
    {
        new SeverityOption("All findings", null), new SeverityOption("Errors", Severity.Error), new SeverityOption("Warnings", Severity.Warning), new SeverityOption("Suggestions", Severity.Info)
    };
    public ObservableCollection<ValidationIssue> Issues { get; } = new();

    [ObservableProperty] private bool _checkEmail = true;
    [ObservableProperty] private bool _checkPhone = true;
    [ObservableProperty] private bool _checkWebsite = true;
    [ObservableProperty] private bool _checkNames = true;
    [ObservableProperty] private bool _checkCompany = true;
    [ObservableProperty] private bool _checkDates = true;
    [ObservableProperty] private bool _checkCountry = true;
    [ObservableProperty] private bool _checkDuplicates = true;
    [ObservableProperty] private SeverityOption _selectedSeverity;
    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private int _total;
    [ObservableProperty] private int _valid;
    [ObservableProperty] private int _withErrors;
    [ObservableProperty] private int _withWarnings;
    [ObservableProperty] private int _missingRequired;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressText;

    partial void OnSelectedSeverityChanged(SeverityOption value) => ApplyFilter();
    partial void OnFilterTextChanged(string value) => ApplyFilter();

    private ValidationRules BuildRules()
    {
        var r = ValidationRules.FromSettings(Ui.Settings.Current);
        r.CheckEmail = CheckEmail; r.CheckPhone = CheckPhone; r.CheckWebsite = CheckWebsite; r.CheckNames = CheckNames; r.CheckCompany = CheckCompany;
        r.CheckDates = CheckDates; r.CheckCountry = CheckCountry; r.CheckDuplicateIdentifiers = CheckDuplicates;
        return r;
    }

    [RelayCommand]
    private async Task Run()
    {
        _cts = new CancellationTokenSource(); var rules = BuildRules();
        var progress = new Progress<ProgressInfo>(p => { Progress = p.Fraction; ProgressText = $"{p.Stage}: {p.Done:N0} of {p.Total:N0}"; });
        ValidationSummary? summary = null;
        var ok = await RunAsync("Validating contacts", async () =>
        {
            var contacts = await _repo.GetAllAsync(_cts.Token);
            summary = await Task.Run(() => ValidationEngine.ValidateAll(contacts, rules, progress, _cts.Token), _cts.Token);
        });
        ProgressText = null; Progress = 0;
        if (!ok || summary is null) return;
        _all = summary.Issues.OrderBy(i => i.Severity).ThenBy(i => i.ContactId).ToList();
        Total = summary.TotalContacts; Valid = summary.ValidRecords; WithErrors = summary.RecordsWithErrors; WithWarnings = summary.RecordsWithWarnings; MissingRequired = summary.RecordsMissingRequired;
        HasResults = true; ApplyFilter();
        Ui.Notifications.Add(NotificationLevel.Info, "Validation finished", $"{summary.RecordsWithErrors:N0} record(s) with errors, {summary.RecordsWithWarnings:N0} with warnings.");
    }

    [RelayCommand] private void Cancel() => _cts?.Cancel();

    private void ApplyFilter()
    {
        IEnumerable<ValidationIssue> q = _all;
        if (SelectedSeverity.Severity is { } s) q = q.Where(i => i.Severity == s);
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var t = FilterText.Trim();
            q = q.Where(i => i.Message.Contains(t, StringComparison.OrdinalIgnoreCase) || i.ContactName.Contains(t, StringComparison.OrdinalIgnoreCase) || i.Code.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
        var list = q.ToList();
        Issues.Clear(); foreach (var i in list.Take(ShowLimit)) Issues.Add(i);
        Note = $"Showing {Math.Min(list.Count, ShowLimit):N0} of {list.Count:N0} finding(s)." + (list.Count > ShowLimit ? " Narrow the filter to see the rest, or export the Invalid Records report." : "");
    }

    private async Task<bool> ApplyOneAsync(ValidationIssue issue)
    {
        if (issue.Field is not { } field || issue.Suggestion is null) return false;
        var c = await _repo.GetAsync(issue.ContactId);
        if (c is null) return false;
        c.Set(field, issue.Suggestion);
        await _repo.UpdateAsync(c);
        return true;
    }

    [RelayCommand]
    private async Task ApplySuggestion(ValidationIssue? issue)
    {
        if (issue?.Suggestion is null) { Ui.Dialogs.Info("No suggestion", "This finding has no automatic correction - edit the contact to fix it."); return; }
        if (!Ui.Dialogs.Confirm("Apply suggested correction?", $"Change {issue.Field} of {issue.ContactName} to:\n\n{issue.Suggestion}\n\nThe old value is kept in the audit log.", "Apply")) return;
        if (await RunAsync("Applying correction", async () => await ApplyOneAsync(issue), AuditOperation.ContactUpdated)) { _all.Remove(issue); Issues.Remove(issue); }
    }

    [RelayCommand]
    private async Task ApplyAllVisible()
    {
        var targets = Issues.Where(i => i.Suggestion is not null && i.Field is not null).ToList();
        if (targets.Count == 0) { Ui.Dialogs.Info("Nothing to apply", "None of the findings currently shown has a suggested correction."); return; }
        if (!Ui.Dialogs.Confirm("Apply all suggested corrections?", $"{targets.Count:N0} suggested correction(s) in the current list will be applied. Review them first - a typo suggestion is a guess. A backup is taken first and every change is audited.", "Apply all", danger: true)) return;
        var done = 0;
        var ok = await RunAsync("Applying corrections", async () =>
        {
            if (Ui.Settings.Current.AutoBackupBeforeBulk) await _backup.CreateAsync(BackupKind.PreOperation, $"Before applying {targets.Count} validation corrections");
            foreach (var t in targets) { if (await ApplyOneAsync(t)) { done++; _all.Remove(t); } }
        }, AuditOperation.ContactUpdated);
        ApplyFilter();
        if (ok) Ui.Notifications.Add(NotificationLevel.Success, "Corrections applied", $"{done:N0} correction(s) applied.");
    }

    [RelayCommand]
    private async Task RefreshStoredFlags()
    {
        var n = 0;
        if (await RunAsync("Re-evaluating quality flags", async () => n = await _repo.ReevaluateQualityAsync()))
            Ui.Notifications.Add(NotificationLevel.Success, "Quality flags refreshed", $"{n:N0} contact(s) changed status (valid / invalid / incomplete).");
    }

    [RelayCommand] private void OpenContacts() => Ui.Navigation.Navigate(Module.Contacts);
}
