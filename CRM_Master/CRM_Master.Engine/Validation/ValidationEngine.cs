using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Validation;

/// <summary>
/// Configurable record validation. Email results are SYNTAX checks only - they never prove that a mailbox exists.
/// </summary>
public static class ValidationEngine
{
    private static readonly HashSet<string> PlaceholderText = new(StringComparer.OrdinalIgnoreCase)
        { "test", "testing", "unknown", "n/a", "na", "none", "null", "nil", "xxx", "abc", "asdf", "dummy", "sample", "name", "-", "--", "." };

    public static List<ValidationIssue> ValidateContact(Contact c, ValidationRules r)
    {
        var issues = new List<ValidationIssue>();
        var name = c.DisplayName;
        void Add(ContactField? f, Severity s, string code, string msg, string? fix = null) => issues.Add(new ValidationIssue(c.Id, name, f, s, code, msg, fix));

        foreach (var f in r.Required)
        {
            var missing = f == ContactField.FullName ? !ContactQuality.HasName(c) : c.Get(f) is null;
            if (missing) Add(f, Severity.Error, "REQUIRED_MISSING", $"{Contact.LabelOf(f)} is required but empty");
        }

        if (r.CheckEmail && c.Email is not null)
        {
            var e = EmailRules.Check(c.Email);
            if (!e.IsValid) Add(ContactField.Email, Severity.Error, "EMAIL_INVALID", $"Email is not valid: {e.Error}", e.SuggestedCorrection ?? (e.Normalized != c.Email ? e.Normalized : null));
            else
            {
                if (e.SuggestedCorrection is not null) Add(ContactField.Email, Severity.Warning, "EMAIL_TYPO", "The domain looks like a typo of a popular provider", e.SuggestedCorrection);
                if (e.IsPlaceholder) Add(ContactField.Email, Severity.Warning, "EMAIL_PLACEHOLDER", "Email looks like a placeholder (test/example address)");
                if (e.IsDisposable) Add(ContactField.Email, Severity.Warning, "EMAIL_DISPOSABLE", "Email uses a disposable-mail domain");
                if (r.FlagRoleEmails && e.IsRoleBased) Add(ContactField.Email, Severity.Info, "EMAIL_ROLE", "Shared / role address (info@, sales@ ...) - may not reach one person");
                if (e.Normalized != c.Email) Add(ContactField.Email, Severity.Info, "EMAIL_FORMAT", "Email can be standardised (lower-case, no spaces)", e.Normalized);
            }
        }

        if (r.CheckPhone)
            foreach (var f in new[] { ContactField.Phone, ContactField.AlternatePhone })
            {
                var v = c.Get(f); if (v is null) continue;
                var p = PhoneNumberParser.Parse(v, r.DefaultCountryCode);
                if (!p.IsValid) Add(f, Severity.Error, "PHONE_INVALID", $"{Contact.LabelOf(f)} is not valid: {p.Error}");
                else
                {
                    if (p.IsSuspicious) Add(f, Severity.Warning, "PHONE_SUSPICIOUS", $"{Contact.LabelOf(f)} looks like a placeholder (repeated or sequential digits)");
                    var formatted = p.Formatted + (p.Extension is null ? "" : " x" + p.Extension);
                    if (formatted != v) Add(f, Severity.Info, "PHONE_FORMAT", $"{Contact.LabelOf(f)} can be standardised", formatted);
                }
            }

        if (r.CheckWebsite && c.Website is not null && !UrlRules.IsValid(c.Website))
            Add(ContactField.Website, Severity.Warning, "WEBSITE_INVALID", "Website is not a valid web address", UrlRules.Normalize(c.Website));

        if (r.CheckNames)
            foreach (var f in new[] { ContactField.FirstName, ContactField.LastName, ContactField.FullName })
            {
                var v = c.Get(f); if (v is null) continue;
                if (v.Contains('@')) Add(f, Severity.Error, "NAME_IS_EMAIL", $"{Contact.LabelOf(f)} contains an email address");
                else if (v.Any(char.IsDigit)) Add(f, Severity.Warning, "NAME_DIGITS", $"{Contact.LabelOf(f)} contains digits");
                if (PlaceholderText.Contains(v)) Add(f, Severity.Warning, "NAME_PLACEHOLDER", $"{Contact.LabelOf(f)} looks like a placeholder");
                else if (v.Length == 1 && f != ContactField.FirstName && f != ContactField.LastName) Add(f, Severity.Warning, "NAME_SHORT", $"{Contact.LabelOf(f)} is a single character");
                if (v.Length > 100) Add(f, Severity.Warning, "NAME_LONG", $"{Contact.LabelOf(f)} is unusually long");
                var proper = NameCaser.Proper(v);
                if (proper != v && !v.Contains('@')) Add(f, Severity.Info, "NAME_CASE", "Capitalisation can be standardised", proper);
            }

        if (r.CheckCompany && c.CompanyName is { } co)
        {
            if (CompanyRules.IsPlaceholder(co)) Add(ContactField.Company, Severity.Warning, "COMPANY_PLACEHOLDER", "Company name looks like a placeholder");
            else if (co.All(ch => char.IsDigit(ch) || ch == ' ')) Add(ContactField.Company, Severity.Warning, "COMPANY_NUMERIC", "Company name is only digits");
            else if (co.Contains('@')) Add(ContactField.Company, Severity.Warning, "COMPANY_IS_EMAIL", "Company name contains an email address");
        }

        if (r.CheckDates && c.SourceCreatedDate is { } d && !DateRules.IsPlausible(d))
            Add(ContactField.CreatedDate, Severity.Warning, "DATE_IMPLAUSIBLE", $"Created date {d:yyyy-MM-dd} is in the future or before 1970");

        if (r.CheckCountry && c.Country is { } country)
        {
            var found = CountryTable.Find(country);
            if (found is null)
            {
                var guess = CountryTable.All.Select(x => (x, s: StringSimilarity.JaroWinkler(StringSimilarity.NormalizeText(country), StringSimilarity.NormalizeText(x.Name)))).OrderByDescending(t => t.s).First();
                Add(ContactField.Country, Severity.Warning, "COUNTRY_UNKNOWN", $"'{country}' is not a recognised country name or code", guess.s >= 0.88 ? guess.x.Name : null);
            }
            else if (!string.Equals(found.Name, country, StringComparison.Ordinal))
                Add(ContactField.Country, Severity.Info, "COUNTRY_FORMAT", "Country can be written as its standard name", found.Name);
        }
        return issues;
    }

    /// <summary>Validates every contact and adds dataset-level checks (identifiers shared by several contacts).</summary>
    public static ValidationSummary ValidateAll(IReadOnlyList<Contact> contacts, ValidationRules rules, IProgress<Core.DTOs.ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var summary = new ValidationSummary { TotalContacts = contacts.Count };
        var perContact = new Dictionary<int, List<ValidationIssue>>();
        for (var i = 0; i < contacts.Count; i++)
        {
            if ((i & 0x3FF) == 0) { ct.ThrowIfCancellationRequested(); progress?.Report(new Core.DTOs.ProgressInfo("Validating", i, contacts.Count)); }
            var issues = ValidateContact(contacts[i], rules);
            if (issues.Count > 0) perContact[contacts[i].Id] = issues;
        }

        if (rules.CheckDuplicateIdentifiers)
        {
            AddShared(contacts, c => c.EmailKey ?? ContactKeys.EmailKey(c.Email), "DUPLICATE_EMAIL", ContactField.Email, "Email", perContact);
            AddShared(contacts, c => c.PhoneKey ?? ContactKeys.PhoneKey(c.Phone, rules.DefaultCountryCode), "DUPLICATE_PHONE", ContactField.Phone, "Phone", perContact);
        }

        foreach (var c in contacts)
        {
            if (!perContact.TryGetValue(c.Id, out var list)) { summary.ValidRecords++; continue; }
            summary.Issues.AddRange(list);
            if (list.Any(x => x.Severity == Severity.Error)) summary.RecordsWithErrors++;
            else if (list.Any(x => x.Severity == Severity.Warning)) summary.RecordsWithWarnings++;
            else summary.ValidRecords++;   // informational suggestions only
            if (list.Any(x => x.Code == "REQUIRED_MISSING")) summary.RecordsMissingRequired++;
        }
        return summary;
    }

    private static void AddShared(IReadOnlyList<Contact> contacts, Func<Contact, string?> key, string code, ContactField field, string label, Dictionary<int, List<ValidationIssue>> into)
    {
        foreach (var g in contacts.Where(c => key(c) is not null).GroupBy(c => key(c)!).Where(g => g.Count() > 1))
            foreach (var c in g)
            {
                var others = string.Join(", ", g.Where(o => o.Id != c.Id).Take(3).Select(o => "#" + o.Id));
                if (!into.TryGetValue(c.Id, out var list)) into[c.Id] = list = new();
                list.Add(new ValidationIssue(c.Id, c.DisplayName, field, Severity.Warning, code, $"{label} is also used by contact {others}"));
            }
    }
}
