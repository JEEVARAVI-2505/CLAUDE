using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using PhoneNumbers;

namespace LeadPilot.Application.Text;

/// <summary>Result of parsing a phone number.</summary>
/// <param name="E164">Canonical +CC… form, or null when the input is not a valid number.</param>
/// <param name="Region">ISO region the number belongs to (e.g. IN), if identified.</param>
/// <param name="IsValid">True when the number is plausible for its region.</param>
public sealed record PhoneInfo(string? E164, string? Region, bool IsValid);

/// <summary>Deterministic normalisation of names, phones, e-mails, domains and addresses.</summary>
public static partial class Normalizer
{
    private static readonly HashSet<string> LegalSuffixes = new(StringComparer.Ordinal)
    {
        "pvt", "private", "ltd", "limited", "llp", "inc", "incorporated", "llc", "co", "corp", "corporation",
        "company", "gmbh", "pty", "plc", "the", "and", "&", "of", "sa", "bv", "ag"
    };

    private static readonly HashSet<string> FreeMailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "yahoo.com", "yahoo.in", "outlook.com", "hotmail.com", "rediffmail.com", "live.com", "icloud.com", "proton.me", "protonmail.com", "aol.com"
    };

    [GeneratedRegex(@"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)*\.[A-Za-z]{2,}$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"[^\p{L}\p{Nd}\s]")]
    private static partial Regex NonWord();

    /// <summary>Lower-cases, strips diacritics and punctuation and collapses whitespace.</summary>
    public static string NormalizeText(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var ch in d)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        var t = NonWord().Replace(sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Replace("&", " and "), " ");
        return string.Join(' ', t.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Company name without legal suffixes (Pvt Ltd, LLC, …) for comparison.</summary>
    public static string CoreName(string? name)
    {
        var tokens = NormalizeText(name).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !LegalSuffixes.Contains(t)).ToArray();
        return tokens.Length == 0 ? NormalizeText(name) : string.Join(' ', tokens);
    }

    /// <summary>Significant tokens of a name (used for blocking and Dice similarity).</summary>
    public static string[] Tokens(string? s) => NormalizeText(s).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Parses a phone number; <paramref name="defaultRegion"/> is used when no +country code is present.</summary>
    public static PhoneInfo ParsePhone(string? raw, string defaultRegion = "IN")
    {
        if (string.IsNullOrWhiteSpace(raw)) return new PhoneInfo(null, null, false);
        try
        {
            var util = PhoneNumberUtil.GetInstance();
            var trimmed = raw.Trim();
            var num = util.Parse(trimmed, defaultRegion.ToUpperInvariant());
            if (!util.IsValidNumber(num)) return new PhoneInfo(null, util.GetRegionCodeForNumber(num), false);
            return new PhoneInfo(util.Format(num, PhoneNumberFormat.E164), util.GetRegionCodeForNumber(num), true);
        }
        catch (NumberParseException)
        {
            return new PhoneInfo(null, null, false);
        }
    }

    /// <summary>Returns the E.164 form or null.</summary>
    public static string? NormalizePhone(string? raw, string defaultRegion = "IN") => ParsePhone(raw, defaultRegion).E164;

    /// <summary>Syntactic e-mail check only (no network). Does not prove the mailbox exists.</summary>
    public static bool IsValidEmailFormat(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var e = email.Trim();
        if (e.Length > 254 || e.Contains("..", StringComparison.Ordinal)) return false;
        var at = e.LastIndexOf('@');
        if (at < 1 || at > 64) return false;
        if (!EmailRegex().IsMatch(e)) return false;
        try { _ = new MailAddress(e); return true; } catch (FormatException) { return false; }
    }

    /// <summary>Lower-cased e-mail or null when the format is invalid.</summary>
    public static string? NormalizeEmail(string? email) => IsValidEmailFormat(email) ? email!.Trim().ToLowerInvariant() : null;

    /// <summary>Domain part of an e-mail address.</summary>
    public static string? EmailDomain(string? email) => NormalizeEmail(email) is { } e ? e[(e.LastIndexOf('@') + 1)..] : null;

    /// <summary>True for consumer mail domains such as gmail.com (not a company identifier).</summary>
    public static bool IsFreeMailDomain(string? domain) => domain != null && FreeMailDomains.Contains(domain);

    /// <summary>Ensures a scheme and returns an absolute http(s) URL, or null.</summary>
    public static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var u = url.Trim();
        if (!u.Contains("://", StringComparison.Ordinal)) u = "https://" + u;
        if (!Uri.TryCreate(u, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !uri.Host.Contains('.')) return null;
        return uri.PathAndQuery == "/" ? uri.GetLeftPart(UriPartial.Authority) : uri.GetLeftPart(UriPartial.Path) + uri.Query;
    }

    /// <summary>Registrable-looking host without leading www., lower-cased; null when not a URL.</summary>
    public static string? DomainOf(string? url)
    {
        var n = NormalizeUrl(url);
        if (n == null) return null;
        var host = new Uri(n).IdnHost.ToLowerInvariant();
        return host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
    }

    /// <summary>True when two hosts are the same site (ignoring www. and sub-domain of the other).</summary>
    public static bool SameSite(string? a, string? b)
    {
        var da = DomainOf(a) ?? a?.ToLowerInvariant();
        var db = DomainOf(b) ?? b?.ToLowerInvariant();
        if (da == null || db == null) return false;
        return da == db || da.EndsWith("." + db, StringComparison.Ordinal) || db.EndsWith("." + da, StringComparison.Ordinal);
    }
}
