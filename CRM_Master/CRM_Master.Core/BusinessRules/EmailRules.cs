using System.Globalization;
using System.Text.RegularExpressions;

namespace CRM_Master.Core.BusinessRules;

/// <summary>Outcome of checking an email address. "Valid" is syntax-only: it does not prove the mailbox exists.</summary>
public sealed record EmailCheck(bool IsValid, string? Normalized, string? Error, string? SuggestedCorrection, bool IsPlaceholder, bool IsDisposable, bool IsRoleBased);

/// <summary>Offline email normalisation and syntax validation with typo suggestions.</summary>
public static partial class EmailRules
{
    [GeneratedRegex(@"^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$")]
    private static partial Regex Syntax();

    private static readonly string[] PopularDomains =
        { "gmail.com", "yahoo.com", "yahoo.co.in", "outlook.com", "hotmail.com", "rediffmail.com", "icloud.com", "live.com", "protonmail.com", "aol.com" };

    private static readonly HashSet<string> Disposable = new(StringComparer.OrdinalIgnoreCase)
        { "mailinator.com", "guerrillamail.com", "10minutemail.com", "tempmail.com", "yopmail.com", "trashmail.com", "throwawaymail.com", "sharklasers.com" };

    private static readonly HashSet<string> RoleLocals = new(StringComparer.OrdinalIgnoreCase)
        { "info", "admin", "sales", "support", "contact", "hello", "office", "noreply", "no-reply", "enquiry", "enquiries", "accounts", "billing", "hr", "careers", "marketing" };

    private static readonly HashSet<string> PlaceholderDomains = new(StringComparer.OrdinalIgnoreCase)
        { "example.com", "example.org", "test.com", "domain.com", "email.com", "abc.com", "xyz.com", "na.com", "none.com", "noemail.com", "sample.com" };

    private static readonly HashSet<string> PlaceholderLocals = new(StringComparer.OrdinalIgnoreCase)
        { "test", "abc", "xyz", "na", "none", "null", "noemail", "no", "nomail", "dummy", "sample", "asdf", "qwerty", "xxx" };

    /// <summary>Lower-cases, strips "mailto:", angle brackets, quotes and inner spaces. Returns null when empty.</summary>
    public static string? Normalize(string? raw)
    {
        var t = TextCleaner.Clean(raw);
        if (t is null) return null;
        if (t.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) t = t[7..];
        t = t.Trim('<', '>', '"', '\'', ';', ',', ' ').Replace(" ", "");
        return t.Length == 0 ? null : t.ToLowerInvariant();
    }

    /// <summary>Checks syntax and flags placeholder / disposable / role-based addresses.</summary>
    public static EmailCheck Check(string? raw)
    {
        var n = Normalize(raw);
        if (n is null) return new EmailCheck(false, null, "Empty", null, false, false, false);
        if (n.Length > 254) return Bad(n, "Longer than 254 characters");
        var at = n.LastIndexOf('@');
        if (at <= 0 || at == n.Length - 1) return Bad(n, "Must contain one '@' with text before and after it");
        if (n.IndexOf('@') != at) return Bad(n, "Contains more than one '@'");
        var local = n[..at]; var domain = n[(at + 1)..];
        if (local.Length > 64) return Bad(n, "Part before '@' is longer than 64 characters");

        if (domain.Contains("..")) return Bad(n, "Domain has consecutive dots");
        try { domain = new IdnMapping().GetAscii(domain); }
        catch (ArgumentException) { return Bad(n, "Domain is not valid"); }

        if (!Syntax().IsMatch(local + "@" + domain)) return Bad(n, DescribeSyntaxProblem(local, domain), Suggest(local, domain));

        var placeholder = PlaceholderDomains.Contains(domain) || PlaceholderLocals.Contains(local);
        return new EmailCheck(true, n, null, Suggest(local, domain), placeholder, Disposable.Contains(domain), RoleLocals.Contains(local));
    }

    private static EmailCheck Bad(string n, string error, string? suggestion = null) =>
        new(false, n, error, suggestion, false, false, false);

    private static string DescribeSyntaxProblem(string local, string domain)
    {
        if (local.Contains("..") || local.StartsWith('.') || local.EndsWith('.')) return "Part before '@' has misplaced dots";
        if (!domain.Contains('.')) return "Domain needs a dot (e.g. company.com)";
        if (domain.Contains("..")) return "Domain has consecutive dots";
        return "Not a valid email address format";
    }

    /// <summary>Suggests a fix for common domain typos (gmial.com -> gmail.com, .con -> .com). Null when nothing to suggest.</summary>
    public static string? Suggest(string local, string domain)
    {
        if (PopularDomains.Contains(domain)) return null;
        foreach (var (wrong, right) in new[] { (".con", ".com"), (".coom", ".com"), (".cmo", ".com"), (".vom", ".com"), (".comm", ".com") })
            if (domain.EndsWith(wrong, StringComparison.Ordinal)) return $"{local}@{domain[..^wrong.Length]}{right}";
        string? best = null; var bestDist = int.MaxValue;
        foreach (var p in PopularDomains)
        {
            var d = StringSimilarity.OsaDistance(domain, p);
            var limit = p.Length >= 9 ? 2 : 1;
            if (d <= limit && d < bestDist) { best = p; bestDist = d; }
        }
        return best is null ? null : $"{local}@{best}";
    }
}
