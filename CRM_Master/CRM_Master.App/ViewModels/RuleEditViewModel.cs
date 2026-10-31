using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.Core.Entities;
using CRM_Master.Engine.Automation;

namespace CRM_Master.App.ViewModels;

public sealed partial class ConditionRow : ObservableObject
{
    public static IReadOnlyList<string> Fields { get; } = Contact.Fields.Select(f => f.Field.ToString()).ToList();
    public static IReadOnlyList<ConditionOperator> Operators { get; } = Enum.GetValues<ConditionOperator>();
    [ObservableProperty] private string _field = nameof(ContactField.Email);
    [ObservableProperty] private ConditionOperator _operator = ConditionOperator.IsEmpty;
    [ObservableProperty] private string? _value;
    public bool NeedsValue => Operator is ConditionOperator.Equals or ConditionOperator.NotEquals or ConditionOperator.Contains or ConditionOperator.StartsWith or ConditionOperator.EndsWith or ConditionOperator.MatchesRegex;
    partial void OnOperatorChanged(ConditionOperator value) => OnPropertyChanged(nameof(NeedsValue));
}

public sealed partial class ActionRow : ObservableObject
{
    public static IReadOnlyList<ActionType> Types { get; } = Enum.GetValues<ActionType>();
    public static IReadOnlyList<string> Fields { get; } = Contact.Fields.Select(f => f.Field.ToString()).ToList();
    [ObservableProperty] private ActionType _type = ActionType.FlagForReview;
    [ObservableProperty] private string? _field;
    [ObservableProperty] private string? _value;
    public bool NeedsField => Type == ActionType.SetField;
    public bool NeedsValue => Type is ActionType.SetField or ActionType.SetStatus;
    partial void OnTypeChanged(ActionType value) { OnPropertyChanged(nameof(NeedsField)); OnPropertyChanged(nameof(NeedsValue)); }
}

/// <summary>Rule builder: name, trigger, conditions (all / any), actions, enabled.</summary>
public sealed partial class RuleEditViewModel : ObservableObject
{
    private readonly AutomationService _svc;
    private readonly int? _id;

    public RuleEditViewModel(AutomationService svc, AutomationRule? existing)
    {
        _svc = svc; _id = existing?.Id;
        _name = existing?.Name ?? ""; _description = existing?.Description; _isEnabled = existing?.IsEnabled ?? true;
        _trigger = existing?.Trigger ?? AutomationTrigger.ContactImported;
        if (existing is not null)
        {
            var parsed = AutomationService.Parse(existing);
            _match = parsed.Definition.Match;
            foreach (var c in parsed.Definition.Conditions) Conditions.Add(new ConditionRow { Field = c.Field, Operator = c.Operator, Value = c.Value });
            foreach (var a in parsed.Definition.Actions) Actions.Add(new ActionRow { Type = a.Type, Field = a.Field, Value = a.Value });
        }
        else Actions.Add(new ActionRow());
    }

    public IReadOnlyList<AutomationTrigger> Triggers { get; } = Enum.GetValues<AutomationTrigger>();
    public IReadOnlyList<ConditionMatch> Matches { get; } = Enum.GetValues<ConditionMatch>();
    public ObservableCollection<ConditionRow> Conditions { get; } = new();
    public ObservableCollection<ActionRow> Actions { get; } = new();

    [ObservableProperty] private string _name;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private AutomationTrigger _trigger;
    [ObservableProperty] private ConditionMatch _match = ConditionMatch.All;
    [ObservableProperty] private string? _error;
    public string Title => _id is null ? "New automation rule" : "Edit automation rule";
    public string TriggerHelp => RuleText.Trigger(Trigger);
    partial void OnTriggerChanged(AutomationTrigger value) => OnPropertyChanged(nameof(TriggerHelp));
    public bool Saved { get; private set; }
    public event Action? RequestClose;

    [RelayCommand] private void AddCondition() => Conditions.Add(new ConditionRow());
    [RelayCommand] private void RemoveCondition(ConditionRow? c) { if (c is not null) Conditions.Remove(c); }
    [RelayCommand] private void AddAction() => Actions.Add(new ActionRow());
    [RelayCommand] private void RemoveAction(ActionRow? a) { if (a is not null) Actions.Remove(a); }
    [RelayCommand] private void Cancel() => RequestClose?.Invoke();

    [RelayCommand]
    private async Task SaveAsync()
    {
        var def = new RuleDefinition { Match = Match };
        foreach (var c in Conditions) def.Conditions.Add(new RuleCondition(c.Field, c.Operator, c.NeedsValue ? c.Value : null));
        foreach (var a in Actions) def.Actions.Add(new RuleAction(a.Type, a.NeedsField ? a.Field : null, a.NeedsValue ? a.Value : null));
        var errors = AutomationService.Validate(Name, Trigger, def);
        if (errors.Count > 0) { Error = string.Join("\n", errors); return; }
        try { await _svc.SaveAsync(_id, Name, Description, Trigger, def, IsEnabled); Saved = true; RequestClose?.Invoke(); }
        catch (Exception ex) when (ex is ArgumentException or Microsoft.EntityFrameworkCore.DbUpdateException) { Error = ex.Message; }
    }
}
