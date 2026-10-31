using System.Text.RegularExpressions;
using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Automation;

/// <summary>Pure rule logic: evaluates conditions and applies actions to an in-memory contact.</summary>
public static class RuleEvaluator
{
    public static bool Matches(RuleDefinition def, Contact c, string countryCode)
    {
        if (def.Conditions.Count == 0) return true;
        return def.Match == ConditionMatch.All
            ? def.Conditions.All(x => Test(x, c, countryCode))
            : def.Conditions.Any(x => Test(x, c, countryCode));
    }

    public static bool Test(RuleCondition cond, Contact c, string countryCode)
    {
        if (!Enum.TryParse<ContactField>(cond.Field, true, out var field)) return false;
        var v = c.Get(field);
        if (field == ContactField.FullName && v is null && ContactQuality.HasName(c)) v = c.DisplayName;
        var cmp = StringComparison.OrdinalIgnoreCase;
        return cond.Operator switch
        {
            ConditionOperator.IsEmpty => v is null,
            ConditionOperator.IsNotEmpty => v is not null,
            ConditionOperator.Equals => string.Equals(v ?? "", cond.Value ?? "", cmp),
            ConditionOperator.NotEquals => !string.Equals(v ?? "", cond.Value ?? "", cmp),
            ConditionOperator.Contains => v is not null && cond.Value is not null && v.Contains(cond.Value, cmp),
            ConditionOperator.StartsWith => v is not null && cond.Value is not null && v.StartsWith(cond.Value, cmp),
            ConditionOperator.EndsWith => v is not null && cond.Value is not null && v.EndsWith(cond.Value, cmp),
            ConditionOperator.MatchesRegex => v is not null && SafeRegex(v, cond.Value),
            ConditionOperator.IsInvalid => v is not null && !IsValidValue(field, v, countryCode),
            ConditionOperator.IsValid => v is not null && IsValidValue(field, v, countryCode),
            _ => false
        };
    }

    private static bool SafeRegex(string input, string? pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return false;
        try { return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)); }
        catch (ArgumentException) { return false; }
        catch (RegexMatchTimeoutException) { return false; }
    }

    private static bool IsValidValue(ContactField f, string v, string cc) => f switch
    {
        ContactField.Email => EmailRules.Check(v).IsValid,
        ContactField.Phone or ContactField.AlternatePhone => PhoneNumberParser.Parse(v, cc).IsValid,
        ContactField.Website => UrlRules.IsValid(v),
        ContactField.CreatedDate => DateRules.TryParse(v, out _),
        ContactField.Country => CountryTable.Find(v) is not null,
        _ => true
    };

    /// <summary>Applies one action; returns true if the contact was modified.</summary>
    public static bool Apply(RuleAction a, Contact c, string cc, ContactRuleResult? result = null)
    {
        switch (a.Type)
        {
            case ActionType.NormalizePhone:
                return Rewrite(c, new[] { ContactField.Phone, ContactField.AlternatePhone }, v =>
                { var p = PhoneNumberParser.Parse(v, cc); return p.IsValid ? p.Formatted + (p.Extension is null ? "" : " x" + p.Extension) : v; });
            case ActionType.NormalizeEmail: return Rewrite(c, new[] { ContactField.Email }, EmailRules.Normalize);
            case ActionType.NormalizeWebsite: return Rewrite(c, new[] { ContactField.Website }, v => UrlRules.Normalize(v) ?? v);
            case ActionType.ProperCaseNames: return Rewrite(c, new[] { ContactField.FirstName, ContactField.LastName, ContactField.FullName }, NameCaser.Proper);
            case ActionType.TrimWhitespace: return Rewrite(c, Contact.Fields.Select(f => f.Field).Where(f => f != ContactField.CreatedDate).ToArray(), TextCleaner.Clean);
            case ActionType.FlagForReview: return Flip(c.NeedsReview, () => c.NeedsReview = true);
            case ActionType.MarkIncomplete: return Flip(c.IsIncomplete, () => c.IsIncomplete = true);
            case ActionType.FlagDuplicate: return Flip(c.IsDuplicateFlagged, () => c.IsDuplicateFlagged = true);
            case ActionType.SetStatus: return SetValue(c, ContactField.Status, a.Value);
            case ActionType.SetField: return Enum.TryParse<ContactField>(a.Field, true, out var f) && SetValue(c, f, a.Value);
            case ActionType.ProposeUpdate: if (result is not null) result.ProposeUpdate = true; return false;
            default: return false;
        }
    }

    private static bool Flip(bool current, Action set) { if (current) return false; set(); return true; }

    private static bool SetValue(Contact c, ContactField f, string? value)
    {
        var before = c.Get(f);
        if (!c.Set(f, value)) return false;
        return before != c.Get(f);
    }

    private static bool Rewrite(Contact c, ContactField[] fields, Func<string, string?> fn)
    {
        var changed = false;
        foreach (var f in fields)
        {
            var before = c.Get(f);
            if (before is null) continue;
            var after = fn(before);
            if (after != before) { c.Set(f, after); changed = true; }
        }
        return changed;
    }

    /// <summary>Runs every rule for <paramref name="trigger"/> against the contact, in rule order.</summary>
    public static ContactRuleResult Run(IEnumerable<ParsedRule> rules, AutomationTrigger trigger, Contact c, string cc)
    {
        var result = new ContactRuleResult();
        foreach (var rule in rules.Where(r => r.Trigger == trigger && r.Error is null))
        {
            if (!Matches(rule.Definition, c, cc)) continue;
            var changes = 0;
            foreach (var action in rule.Definition.Actions) if (Apply(action, c, cc, result)) changes++;
            result.Fired.Add((rule, changes));
        }
        return result;
    }
}
