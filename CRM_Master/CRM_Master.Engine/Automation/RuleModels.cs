using System.Text.Json;
using System.Text.Json.Serialization;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Automation;

public enum ConditionOperator { IsEmpty, IsNotEmpty, Equals, NotEquals, Contains, StartsWith, EndsWith, MatchesRegex, IsInvalid, IsValid }
public enum ConditionMatch { All, Any }
public enum ActionType
{
    NormalizePhone, NormalizeEmail, NormalizeWebsite, ProperCaseNames, TrimWhitespace,
    FlagForReview, MarkIncomplete, FlagDuplicate, SetField, SetStatus, ProposeUpdate, GenerateImportReport
}

/// <summary>Condition on one contact field (Field is a <see cref="ContactField"/> name).</summary>
public sealed record RuleCondition(string Field, ConditionOperator Operator, string? Value = null);

/// <summary>What a rule does. Field/Value are used by SetField and SetStatus.</summary>
public sealed record RuleAction(ActionType Type, string? Field = null, string? Value = null);

/// <summary>The editable body of a rule (stored as JSON in the AutomationRules table).</summary>
public sealed class RuleDefinition
{
    public ConditionMatch Match { get; set; } = ConditionMatch.All;
    public List<RuleCondition> Conditions { get; set; } = new();
    public List<RuleAction> Actions { get; set; } = new();

    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() }, PropertyNameCaseInsensitive = true };

    public static string SerializeConditions(RuleDefinition d) => JsonSerializer.Serialize(new { d.Match, d.Conditions }, Json);
    public static string SerializeActions(RuleDefinition d) => JsonSerializer.Serialize(d.Actions, Json);

    public static RuleDefinition Parse(string conditionsJson, string actionsJson)
    {
        var def = new RuleDefinition();
        using (var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(conditionsJson) ? "{}" : conditionsJson))
        {
            if (doc.RootElement.TryGetProperty("Match", out var m) || doc.RootElement.TryGetProperty("match", out m))
                def.Match = Enum.Parse<ConditionMatch>(m.GetString() ?? "All", true);
            if (doc.RootElement.TryGetProperty("Conditions", out var c) || doc.RootElement.TryGetProperty("conditions", out c))
                def.Conditions = JsonSerializer.Deserialize<List<RuleCondition>>(c.GetRawText(), Json) ?? new();
        }
        def.Actions = JsonSerializer.Deserialize<List<RuleAction>>(string.IsNullOrWhiteSpace(actionsJson) ? "[]" : actionsJson, Json) ?? new();
        return def;
    }
}

/// <summary>A rule loaded from the database and parsed; <see cref="Error"/> is set when its JSON is unusable.</summary>
public sealed record ParsedRule(int Id, string Name, AutomationTrigger Trigger, RuleDefinition Definition, string? Error = null);

/// <summary>What happened when rules ran against one contact.</summary>
public sealed class ContactRuleResult
{
    public List<(ParsedRule Rule, int Changes)> Fired { get; } = new();
    public bool ProposeUpdate { get; set; }
}
