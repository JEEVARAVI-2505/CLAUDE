using CRM_Master.Core.Entities;

namespace CRM_Master.Core.BusinessRules;

/// <summary>Computes the indexed lookup keys on a contact so duplicate checks never need full scans.</summary>
public static class ContactKeys
{
    /// <summary>Order-insensitive name key: lower-case letter tokens sorted alphabetically ("Smith, John" == "John Smith").</summary>
    public static string? NameKey(Contact c)
    {
        var name = !string.IsNullOrWhiteSpace(c.FullName) ? c.FullName : $"{c.FirstName} {c.LastName}";
        var tokens = StringSimilarity.NormalizeText(name).Split(' ', StringSplitOptions.RemoveEmptyEntries).OrderBy(t => t, StringComparer.Ordinal);
        var key = string.Join(' ', tokens);
        return key.Length == 0 ? null : key;
    }

    public static string? EmailKey(string? email) => EmailRules.Normalize(email);

    public static string? PhoneKey(string? phone, string defaultCountryCode)
    {
        if (TextCleaner.IsBlank(phone)) return null;
        var parsed = PhoneNumberParser.Parse(phone, defaultCountryCode);
        if (parsed.IsValid) return parsed.Key;
        var digits = new string(phone!.Where(char.IsDigit).ToArray()).TrimStart('0');
        return digits.Length >= 7 ? digits : null;
    }

    /// <summary>Fills <c>EmailKey</c>, <c>PhoneKey</c>, <c>AlternatePhoneKey</c> and <c>NameKey</c>.</summary>
    public static void Apply(Contact c, string defaultCountryCode)
    {
        c.EmailKey = EmailKey(c.Email);
        c.PhoneKey = PhoneKey(c.Phone, defaultCountryCode);
        c.AlternatePhoneKey = PhoneKey(c.AlternatePhone, defaultCountryCode);
        c.NameKey = NameKey(c);
    }
}
