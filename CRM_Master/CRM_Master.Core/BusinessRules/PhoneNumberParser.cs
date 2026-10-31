using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CRM_Master.Core.BusinessRules;

public enum PhoneKind { Unknown, Mobile, Landline }

/// <summary>Outcome of parsing a phone number. <see cref="IsValid"/> means plausible format only - not that the line is in service.</summary>
public sealed record PhoneParseResult(
    bool IsValid, string? E164, string? Formatted, string? CountryCode, string? NationalNumber,
    PhoneKind Kind, string? Extension, string? Error, bool IsSuspicious)
{
    /// <summary>Digits-only matching key (country code + national number) or null when invalid.</summary>
    public string? Key => IsValid ? E164![1..] : null;
}

/// <summary>
/// Offline phone-number parser. Full rules for India (+91, 10 digit national numbers, mobiles start 6-9),
/// North America (+1) and generic ITU length rules for every country in <see cref="CountryTable"/>.
/// </summary>
public static partial class PhoneNumberParser
{
    [GeneratedRegex(@"(?:\b(?:ext|extn|extension)\.?|\bx|#)\s*(\d{1,6})\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex ExtensionRegex();

    [GeneratedRegex(@"^\d(\.\d+)?[eE]\+?\d+$")]
    private static partial Regex ScientificRegex();

    [GeneratedRegex(@"^[\d\s().+\-/]+$")]
    private static partial Regex AllowedChars();

    /// <summary>Parses <paramref name="raw"/>. <paramref name="defaultCountryCode"/> (digits only, e.g. "91") is applied to numbers without a country prefix.</summary>
    public static PhoneParseResult Parse(string? raw, string defaultCountryCode = "91")
    {
        var text = TextCleaner.Clean(raw);
        if (text is null) return Fail("Empty");

        // Excel often turns long numbers into scientific notation (9.87654E+09).
        if (ScientificRegex().IsMatch(text) &&
            decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var sci) && sci > 0 && sci < 1e16m)
            text = decimal.Truncate(sci).ToString(CultureInfo.InvariantCulture);

        string? ext = null;
        var em = ExtensionRegex().Match(text);
        if (em.Success) { ext = em.Groups[1].Value; text = text[..em.Index].Trim(); }

        if (text.Length == 0 || !AllowedChars().IsMatch(text)) return Fail("Contains letters or unsupported characters");

        var hasPlus = text.StartsWith('+');
        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (digits.Length < 5) return Fail("Too short");
        if (!hasPlus && digits.StartsWith("00", StringComparison.Ordinal)) { hasPlus = true; digits = digits[2..]; }

        string cc, national;
        if (hasPlus)
        {
            var code = DetectCallingCode(digits);
            if (code is null) return Fail("Unknown country calling code");
            cc = code; national = digits[code.Length..];
            if (cc != "91" && national.StartsWith('0')) national = national.TrimStart('0'); // "+44 (0) 79.." style
        }
        else if (!TryApplyDefault(digits, defaultCountryCode, out cc, out national, out var err))
            return Fail(err);

        return Validate(cc, national, ext);
    }

    private static string? DetectCallingCode(string digits)
    {
        for (var len = 3; len >= 1; len--)
            if (digits.Length > len && CountryTable.IsKnownCallingCode(digits[..len])) return digits[..len];
        return null;
    }

    private static bool TryApplyDefault(string digits, string defaultCc, out string cc, out string national, out string error)
    {
        cc = defaultCc; national = digits; error = "";
        if (defaultCc == "91")
        {
            if (digits.Length == 10) return true;
            if (digits.Length == 11 && digits[0] == '0') { national = digits[1..]; return true; }
            if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal)) { national = digits[2..]; return true; }
            if (digits.Length == 13 && digits.StartsWith("091", StringComparison.Ordinal)) { national = digits[3..]; return true; }
            error = digits.Length < 10 ? "Too short for an Indian number (needs 10 digits)" : "Too long for an Indian number";
            return false;
        }
        if (digits.StartsWith(defaultCc, StringComparison.Ordinal) && digits.Length - defaultCc.Length >= 7) national = digits[defaultCc.Length..];
        national = national.TrimStart('0');
        return true;
    }

    private static PhoneParseResult Validate(string cc, string national, string? ext)
    {
        if (national.Length == 0 || national.Length + cc.Length > 15) return Fail("Wrong length");
        var kind = PhoneKind.Unknown;

        switch (cc)
        {
            case "91":
                if (national.Length != 10) return Fail("Indian numbers need exactly 10 digits");
                if (national[0] is >= '6' and <= '9') kind = PhoneKind.Mobile;
                else if (national[0] is >= '1' and <= '5') kind = PhoneKind.Landline;
                else return Fail("Indian numbers cannot start with 0");
                break;
            case "1" when CountryTable.FromCallingCode("1") is not null:
                if (national.Length != 10) return Fail("North American numbers need 10 digits");
                if (national[0] is '0' or '1' || national[3] is '0' or '1') return Fail("Invalid area / exchange code");
                break;
            default:
                if (national.Length < 4 || national.Length > 14) return Fail("Wrong length");
                break;
        }

        return new PhoneParseResult(true, "+" + cc + national, Format(cc, national), cc, national, kind, ext, null, IsSuspicious(national));
    }

    private static string Format(string cc, string national)
    {
        var sb = new StringBuilder("+").Append(cc).Append(' ');
        if (cc == "91" && national.Length == 10) sb.Append(national, 0, 5).Append(' ').Append(national, 5, 5);
        else if (cc == "1" && national.Length == 10) sb.Append(national, 0, 3).Append(' ').Append(national, 3, 3).Append(' ').Append(national, 6, 4);
        else sb.Append(national);
        return sb.ToString();
    }

    /// <summary>Placeholder-like numbers: one repeated digit, simple sequences, or a two-digit pattern repeated.</summary>
    public static bool IsSuspicious(string national)
    {
        if (national.Length < 6) return false;
        if (national.Distinct().Count() == 1) return true;
        if ("01234567890123456789".Contains(national) || "98765432109876543210".Contains(national)) return true;
        if (national.Length % 2 == 0)
        {
            var pair = national[..2];
            if (string.Concat(Enumerable.Repeat(pair, national.Length / 2)) == national) return true;
        }
        return false;
    }

    private static PhoneParseResult Fail(string error) =>
        new(false, null, null, null, null, PhoneKind.Unknown, null, error, false);
}
