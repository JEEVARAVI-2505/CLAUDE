using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Validation;

public enum Severity { Error, Warning, Info }

/// <summary>One finding about one contact, with an optional suggested fix.</summary>
public sealed record ValidationIssue(int ContactId, string ContactName, ContactField? Field, Severity Severity, string Code, string Message, string? Suggestion = null);

/// <summary>Which checks run. Defaults come from Settings.</summary>
public sealed class ValidationRules
{
    public bool CheckEmail { get; set; } = true;
    public bool CheckPhone { get; set; } = true;
    public bool CheckWebsite { get; set; } = true;
    public bool CheckNames { get; set; } = true;
    public bool CheckCompany { get; set; } = true;
    public bool CheckDates { get; set; } = true;
    public bool CheckCountry { get; set; } = true;
    public bool CheckDuplicateIdentifiers { get; set; } = true;
    public bool FlagRoleEmails { get; set; } = true;
    public List<ContactField> Required { get; set; } = new() { ContactField.FullName, ContactField.Email };
    public string DefaultCountryCode { get; set; } = "91";

    public static ValidationRules FromSettings(AppSettings s)
    {
        var q = s.ToQualityOptions();
        return new ValidationRules { Required = q.RequiredFields.ToList(), DefaultCountryCode = s.DefaultCountryCode, CheckWebsite = s.ValidateWebsite, CheckCountry = s.ValidateCountry };
    }
}

/// <summary>Aggregated validation outcome for a set of contacts.</summary>
public sealed class ValidationSummary
{
    public int TotalContacts { get; set; }
    public int ValidRecords { get; set; }
    public int RecordsWithErrors { get; set; }
    public int RecordsWithWarnings { get; set; }
    public int RecordsMissingRequired { get; set; }
    public List<ValidationIssue> Issues { get; } = new();
    public Dictionary<string, int> ByCode => Issues.GroupBy(i => i.Code).ToDictionary(g => g.Key, g => g.Count());
}
