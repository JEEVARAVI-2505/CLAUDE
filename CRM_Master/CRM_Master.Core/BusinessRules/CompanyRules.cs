namespace CRM_Master.Core.BusinessRules;

/// <summary>Company-name normalisation used for matching and for the Companies table key.</summary>
public static class CompanyRules
{
    private static readonly HashSet<string> LegalSuffixes = new(StringComparer.Ordinal)
    {
        "pvt", "private", "ltd", "limited", "llp", "llc", "inc", "incorporated", "corp", "corporation", "co", "company",
        "plc", "gmbh", "pty", "sa", "ag", "bv", "opc", "lp", "pte", "sdn", "bhd"
    };

    private static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
        { "n/a", "na", "none", "null", "nil", "-", "--", "test", "unknown", "not available", "no company", "self", "xxx" };

    /// <summary>"Infosys Pvt. Ltd." -> "infosys"; "The Tata Group" -> "tata group". Empty string when nothing remains.</summary>
    public static string Key(string? name)
    {
        var norm = StringSimilarity.NormalizeText(name?.Replace("&", " and "));
        if (norm.Length == 0) return "";
        var tokens = norm.Split(' ').ToList();
        if (tokens.Count > 1 && tokens[0] == "the") tokens.RemoveAt(0);
        while (tokens.Count > 1 && LegalSuffixes.Contains(tokens[^1])) tokens.RemoveAt(tokens.Count - 1);
        return string.Join(' ', tokens);
    }

    public static bool IsPlaceholder(string? name) => name is not null && Placeholders.Contains(name.Trim());

    /// <summary>Similarity of two company names in [0,1] after normalisation.</summary>
    public static double Similarity(string? a, string? b)
    {
        var ka = Key(a); var kb = Key(b);
        if (ka.Length == 0 || kb.Length == 0) return 0;
        return ka == kb ? 1 : StringSimilarity.JaroWinkler(ka, kb);
    }
}
