using CRM_Master.Core.Entities;

namespace CRM_Master.Core.BusinessRules;

/// <summary>Settings that decide what "complete" means.</summary>
public sealed record QualityOptions(IReadOnlyCollection<ContactField> RequiredFields, string DefaultCountryCode = "91")
{
    public static QualityOptions Default { get; } = new(new[] { ContactField.FullName, ContactField.Email });
}

/// <summary>Derives the persisted quality flags (email / phone state, completeness) from a contact.</summary>
public static class ContactQuality
{
    /// <summary>True when the "name" requirement is met by a full name or a first/last name.</summary>
    public static bool HasName(Contact c) =>
        !TextCleaner.IsBlank(c.FullName) || !TextCleaner.IsBlank(c.FirstName) || !TextCleaner.IsBlank(c.LastName);

    /// <summary>Lists required fields that are empty. FullName is satisfied by any name part.</summary>
    public static List<ContactField> MissingRequired(Contact c, QualityOptions options)
    {
        var missing = new List<ContactField>();
        foreach (var f in options.RequiredFields)
        {
            var empty = f == ContactField.FullName ? !HasName(c) : c.Get(f) is null;
            if (empty) missing.Add(f);
        }
        return missing;
    }

    /// <summary>Updates EmailState, PhoneState and IsIncomplete in place.</summary>
    public static void Evaluate(Contact c, QualityOptions options)
    {
        c.EmailState = TextCleaner.IsBlank(c.Email) ? ValidationState.Missing
            : EmailRules.Check(c.Email).IsValid ? ValidationState.Valid : ValidationState.Invalid;
        c.PhoneState = TextCleaner.IsBlank(c.Phone) ? ValidationState.Missing
            : PhoneNumberParser.Parse(c.Phone, options.DefaultCountryCode).IsValid ? ValidationState.Valid : ValidationState.Invalid;
        c.IsIncomplete = MissingRequired(c, options).Count > 0;
    }
}
