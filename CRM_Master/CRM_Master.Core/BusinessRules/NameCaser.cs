using System.Text;

namespace CRM_Master.Core.BusinessRules;

/// <summary>
/// Standardises capitalisation of personal names. Names that already use mixed case
/// (e.g. "McDonald", "DeSouza") are left alone - only ALL CAPS or all lowercase input is re-cased.
/// </summary>
public static class NameCaser
{
    private static readonly HashSet<string> Particles = new(StringComparer.OrdinalIgnoreCase)
        { "de", "van", "von", "der", "den", "bin", "bint", "al", "el", "la", "le", "da", "di", "du", "dos", "das", "ibn", "of" };

    private static readonly HashSet<string> Roman = new(StringComparer.OrdinalIgnoreCase) { "ii", "iii", "iv", "vi", "vii", "viii", "ix" };

    /// <summary>Returns the properly capitalised name, or the original (cleaned) text when it is already mixed case.</summary>
    public static string? Proper(string? name)
    {
        var cleaned = TextCleaner.Clean(name);
        if (cleaned is null) return null;
        if (!IsSingleCase(cleaned)) return cleaned;

        var tokens = cleaned.Split(' ');
        var sb = new StringBuilder();
        for (var i = 0; i < tokens.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            var t = tokens[i];
            if (i > 0 && Particles.Contains(t)) sb.Append(t.ToLowerInvariant());
            else if (Roman.Contains(t)) sb.Append(t.ToUpperInvariant());
            else sb.Append(CaseToken(t));
        }
        return sb.ToString();
    }

    private static bool IsSingleCase(string s)
    {
        var hasUpper = false; var hasLower = false;
        foreach (var c in s)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
        }
        return !(hasUpper && hasLower);
    }

    // Capitalises letters after start, hyphen, apostrophe or dot: o'brien -> O'Brien, a.r. -> A.R., mary-jane -> Mary-Jane
    private static string CaseToken(string token)
    {
        var lower = token.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        var upperNext = true;
        foreach (var c in lower)
        {
            sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
            upperNext = c is '-' or '\'' or '’' or '.';
        }
        var result = sb.ToString();
        // Mc prefix: mcdonald -> McDonald (only the safe, common "Mc" rule)
        if (result.Length > 2 && result.StartsWith("Mc", StringComparison.Ordinal) && char.IsLetter(result[2]))
            result = "Mc" + char.ToUpperInvariant(result[2]) + result[3..];
        return result;
    }
}
