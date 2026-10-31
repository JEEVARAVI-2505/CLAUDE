using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.App.Views;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Engine.Automation;

namespace CRM_Master.App.ViewModels;

/// <summary>A rule in the list with human-readable text.</summary>
public sealed class RuleRow
{
    public RuleRow(AutomationRule r)
    {
        Rule = r;
        var p = AutomationService.Parse(r);
        TriggerText = RuleText.Trigger(r.Trigger);
        ConditionText = p.Error is not null ? "(invalid definition)" : RuleText.Conditions(p.Definition);
        ActionText = p.Error is not null ? p.Error : RuleText.Actions(p.Definition);
    }
    public AutomationRule Rule { get; }
    public int Id => Rule.Id;
    public string Name => Rule.Name;
    public string? Description => Rule.Description;
    public bool IsEnabled => Rule.IsEnabled;
    public string StatusText => Rule.IsEnabled ? "Enabled" : "Disabled";
    public string TriggerText { get; }
    public string ConditionText { get; }
    public string ActionText { get; }
    public string LastRunText => Rule.LastRunAt is { } d ? DateTime.SpecifyKind(d, DateTimeKind.Utc).ToLocalTime().ToString("dd MMM yyyy HH:mm") : "never";
    public int RunCount => Rule.RunCount;
}

public static class RuleText
{
    public static string Trigger(AutomationTrigger t) => t switch
    {
        AutomationTrigger.ContactImported => "When a contact is imported", AutomationTrigger.ContactCreated => "When a contact is created",
        AutomationTrigger.ContactUpdated => "When a contact is edited", AutomationTrigger.ExistingContactMatched => "When an imported row matches an existing contact",
        AutomationTrigger.DuplicateDetected => "When duplicates are detected", AutomationTrigger.ImportCompleted => "When an import completes", _ => "Run manually"
    };
    public static string Op(ConditionOperator o) => o switch
    {
        ConditionOperator.IsEmpty => "is empty", ConditionOperator.IsNotEmpty => "is not empty", ConditionOperator.Equals => "equals", ConditionOperator.NotEquals => "does not equal",
        ConditionOperator.Contains => "contains", ConditionOperator.StartsWith => "starts with", ConditionOperator.EndsWith => "ends with", ConditionOperator.MatchesRegex => "matches pattern",
        ConditionOperator.IsInvalid => "is invalid", _ => "is valid"
    };
    public static string Conditions(RuleDefinition d) => d.Conditions.Count == 0 ? "always"
        : string.Join(d.Match == ConditionMatch.All ? " AND " : " OR ", d.Conditions.Select(c => $"{c.Field} {Op(c.Operator)}{(string.IsNullOrEmpty(c.Value) ? "" : " \"" + c.Value + "\"")}"));
    public static string Action(RuleAction a) => a.Type switch
    {
        ActionType.NormalizePhone => "standardise phone numbers", ActionType.NormalizeEmail => "normalise email", ActionType.NormalizeWebsite => "normalise website",
        ActionType.ProperCaseNames => "capitalise names", ActionType.TrimWhitespace => "trim spaces", ActionType.FlagForReview => "flag for review", ActionType.MarkIncomplete => "mark incomplete",
        ActionType.FlagDuplicate => "flag as possible duplicate", ActionType.SetField => $"set {a.Field} to \"{a.Value}\"", ActionType.SetStatus => $"set status to \"{a.Value}\"",
        ActionType.ProposeUpdate => "propose an update (review in Bulk Update)", _ => "generate an import report"
    };
    public static string Actions(RuleDefinition d) => string.Join("; ", d.Actions.Select(Action));
}

/// <summary>Automation rules: list, enable/disable, edit, run now, execution history.</summary>
public sealed partial class AutomationViewModel : ViewModelBase
{
    private readonly AutomationService _svc;
    public AutomationViewModel(UiServices ui, AutomationService svc) : base(ui) => _svc = svc;

    public ObservableCollection<RuleRow> Rules { get; } = new();
    public ObservableCollection<AutomationExecution> History { get; } = new();

    [ObservableProperty] private RuleRow? _selected;
    [ObservableProperty] private double _progress;
    public bool HasSelection => Selected is not null;
    public bool IsEngineOn => Ui.Settings.Current.AutomationEnabled;
    partial void OnSelectedChanged(RuleRow? value) { OnPropertyChanged(nameof(HasSelection)); _ = LoadHistoryAsync(); }

    public override async Task ActivateAsync() { OnPropertyChanged(nameof(IsEngineOn)); await ReloadAsync(); }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        var keep = Selected?.Id;
        List<AutomationRule> rules = new();
        await RunAsync("Loading rules", async () => rules = await _svc.ListAsync());
        Rules.Clear(); foreach (var r in rules) Rules.Add(new RuleRow(r));
        Selected = Rules.FirstOrDefault(r => r.Id == keep) ?? Rules.FirstOrDefault();
    }

    private async Task LoadHistoryAsync()
    {
        History.Clear();
        var id = Selected?.Id;
        foreach (var h in await _svc.HistoryAsync(id, 100)) History.Add(h);
    }

    private async Task EditAsync(AutomationRule? rule)
    {
        var vm = new RuleEditViewModel(_svc, rule);
        if (Ui.Dialogs.ShowWindow(new RuleEditWindow(vm)) == true) { Ui.Notifications.Add(NotificationLevel.Success, "Rule saved", vm.Name); await ReloadAsync(); }
    }

    [RelayCommand] private Task NewRule() => EditAsync(null);
    [RelayCommand] private Task EditRule() => Selected is null ? Task.CompletedTask : EditAsync(Selected.Rule);

    [RelayCommand]
    private async Task ToggleEnabled()
    {
        if (Selected is null) return;
        var id = Selected.Id; var to = !Selected.IsEnabled;
        if (await RunAsync("Updating rule", () => _svc.SetEnabledAsync(id, to), AuditOperation.AutomationExecution)) await ReloadAsync();
    }

    [RelayCommand]
    private async Task DeleteRule()
    {
        if (Selected is null) return;
        if (!Ui.Dialogs.Confirm("Delete rule?", $"Delete \"{Selected.Name}\" and its execution history? Contacts already changed by the rule are not affected.", "Delete", danger: true)) return;
        var id = Selected.Id;
        if (await RunAsync("Deleting rule", () => _svc.DeleteAsync(id))) await ReloadAsync();
    }

    [RelayCommand]
    private async Task RunNow()
    {
        if (Selected is null) return;
        var rule = Selected;
        if (!Ui.Dialogs.Confirm("Run rule on all contacts?", $"Apply \"{rule.Name}\" to every contact now?\n\nA backup is taken first, every change is audited, and the run can be undone from Activity Logs.", "Run rule")) return;
        var progress = new Progress<ProgressInfo>(p => Progress = p.Fraction);
        (int Matched, int Changed) res = (0, 0);
        var ok = await RunAsync("Running rule", async () => res = await _svc.RunNowAsync(rule.Id, progress), AuditOperation.AutomationExecution);
        Progress = 0;
        if (ok) Ui.Notifications.Add(NotificationLevel.Success, "Rule executed", $"{res.Matched:N0} contact(s) matched, {res.Changed:N0} changed.");
        await ReloadAsync();
    }
}
