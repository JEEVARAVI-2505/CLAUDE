using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Cleaning;

/// <summary>Pure analysis: works out what cleaning would change and which problems remain. Never writes anything.</summary>
public static class CleaningEngine
{
    private static readonly HashSet<string> SuspiciousText = new(StringComparer.OrdinalIgnoreCase)
        { "test", "testing", "unknown", "n/a", "na", "none", "null", "xxx", "abc", "asdf", "dummy", "sample", "no name", "not available", "-" };

    public static string Clean(ContactField field, string value, CleaningOptions o, List<string> reasons)
    {
        var v = value;
        if (o.TrimAndCollapseSpaces)
        {
            var t = field is ContactField.Address or ContactField.Notes ? TextCleaner.CleanMultiline(v) : TextCleaner.Clean(v);
            if (t != v) { reasons.Add("Removed extra spaces"); v = t ?? ""; }
        }
        switch (field)
        {
            case ContactField.FirstName or ContactField.LastName or ContactField.FullName when o.ProperCaseNames:
                var proper = NameCaser.Proper(v);
                if (proper != null && proper != v) { reasons.Add("Standardised capitalisation"); v = proper; }
                break;
            case ContactField.Email when o.NormalizeEmail:
                var e = EmailRules.Normalize(v);
                if (e != null && e != v) { reasons.Add("Lower-cased / tidied email"); v = e; }
                break;
            case ContactField.Phone or ContactField.AlternatePhone when o.NormalizePhone:
                var p = PhoneNumberParser.Parse(v, o.DefaultCountryCode);
                var formatted = p.IsValid ? p.Formatted + (p.Extension is null ? "" : " x" + p.Extension) : null;
                if (formatted != null && formatted != v) { reasons.Add("Standardised phone format"); v = formatted; }
                break;
            case ContactField.Website when o.NormalizeWebsite:
                var u = UrlRules.IsValid(v) ? UrlRules.Normalize(v) : null;
                if (u != null && u != v) { reasons.Add("Normalised web address"); v = u; }
                break;
        }
        return v;
    }

    public static CleaningReport Analyze(IReadOnlyList<Contact> contacts, CleaningOptions o, IProgress<ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var report = new CleaningReport { ContactsScanned = contacts.Count };
        var signatures = new Dictionary<string, List<int>>(StringComparer.Ordinal);

        for (var i = 0; i < contacts.Count; i++)
        {
            if ((i & 0x1FF) == 0) { ct.ThrowIfCancellationRequested(); progress?.Report(new ProgressInfo("Analysing", i, contacts.Count)); }
            var c = contacts[i];
            var cleaned = c.Snapshot();
            foreach (var f in Contact.Fields)
            {
                if (f.Field == ContactField.CreatedDate) continue;
                var before = c.Get(f.Field);
                if (before is null) { cleaned.Set(f.Field, null); continue; }
                var reasons = new List<string>();
                var after = Clean(f.Field, before, o, reasons);
                if (after != before) { report.Changes.Add(new CleaningChange(c.Id, c.DisplayName, f.Field, before, after, string.Join("; ", reasons.Distinct()))); cleaned.Set(f.Field, after); }
            }
            FindIssues(c, cleaned, o, report);

            if (o.RemoveExactDuplicates)
            {
                var sig = string.Join("\u001f", Contact.Fields.Select(f => (cleaned.Get(f.Field) ?? "").ToLowerInvariant()));
                if (sig.Replace("\u001f", "").Length > 0)
                {
                    if (!signatures.TryGetValue(sig, out var list)) signatures[sig] = list = new();
                    list.Add(c.Id);
                }
            }
        }
        foreach (var g in signatures.Values.Where(g => g.Count > 1)) report.ExactDuplicateGroups.Add(g.OrderBy(x => x).ToList());
        progress?.Report(new ProgressInfo("Analysing", contacts.Count, contacts.Count));
        return report;
    }

    private static void FindIssues(Contact original, Contact cleaned, CleaningOptions o, CleaningReport r)
    {
        var name = original.DisplayName; var id = original.Id;
        void Add(ContactField f, CleaningIssueKind k, string detail, string? v = null) => r.Issues.Add(new CleaningIssue(id, name, f, k, detail, v));

        if (!ContactQuality.HasName(cleaned)) Add(ContactField.FullName, CleaningIssueKind.EmptyField, "Name is empty");
        if (cleaned.Email is null) Add(ContactField.Email, CleaningIssueKind.EmptyField, "Email is empty");
        if (cleaned.Phone is null) Add(ContactField.Phone, CleaningIssueKind.EmptyField, "Phone is empty");
        if (cleaned.CompanyName is null) Add(ContactField.Company, CleaningIssueKind.EmptyField, "Company is empty");

        if (cleaned.Email is { } em)
        {
            var chk = EmailRules.Check(em);
            if (!chk.IsValid) Add(ContactField.Email, CleaningIssueKind.MalformedEmail, chk.Error ?? "Invalid", em);
            else if (chk.IsPlaceholder) Add(ContactField.Email, CleaningIssueKind.SuspiciousValue, "Looks like a placeholder address", em);
            else if (chk.IsDisposable) Add(ContactField.Email, CleaningIssueKind.SuspiciousValue, "Disposable email domain", em);
        }
        foreach (var f in new[] { ContactField.Phone, ContactField.AlternatePhone })
        {
            var v = cleaned.Get(f); if (v is null) continue;
            var p = PhoneNumberParser.Parse(v, o.DefaultCountryCode);
            if (!p.IsValid) Add(f, CleaningIssueKind.MalformedPhone, p.Error ?? "Invalid", v);
            else if (p.IsSuspicious) Add(f, CleaningIssueKind.SuspiciousValue, "Repeated or sequential digits", v);
        }
        if (original.SourceCreatedDate is { } d && !DateRules.IsPlausible(d)) Add(ContactField.CreatedDate, CleaningIssueKind.InvalidDate, "Date is in the future or before 1970", DateRules.ToIso(d));
        foreach (var f in new[] { ContactField.FirstName, ContactField.LastName, ContactField.FullName, ContactField.Company })
            if (cleaned.Get(f) is { } v && (SuspiciousText.Contains(v) || (v.Length >= 4 && v.Distinct().Count() == 1)))
                Add(f, CleaningIssueKind.SuspiciousValue, "Looks like a placeholder", v);
        foreach (var f in new[] { ContactField.FirstName, ContactField.LastName })
            if (cleaned.Get(f) is { } v && v.Any(char.IsDigit)) Add(f, CleaningIssueKind.SuspiciousValue, "Name contains digits", v);
    }
}
