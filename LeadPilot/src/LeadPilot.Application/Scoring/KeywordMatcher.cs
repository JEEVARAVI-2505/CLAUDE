using System.Reflection;
using System.Text.Json;
using LeadPilot.Application.Text;

namespace LeadPilot.Application.Scoring;

/// <summary>Synonym-aware keyword matching driven by <c>Rules/synonyms.json</c>.</summary>
public static class KeywordMatcher
{
    private static readonly Lazy<Rules> Loaded = new(Load);

    private sealed record Rules(HashSet<string> Stop, List<string[]> Groups, Dictionary<string, string[]> IndustryKeywords);

    private static Rules Load()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("LeadPilot.Application.Rules.synonyms.json")
                      ?? throw new InvalidOperationException("synonyms.json resource missing");
        using var doc = JsonDocument.Parse(s);
        var root = doc.RootElement;
        var stop = root.GetProperty("stopwords").EnumerateArray().Select(e => Stem(e.GetString()!)).ToHashSet();
        var groups = root.GetProperty("groups").EnumerateArray().Select(g => g.EnumerateArray().Select(e => StemPhrase(e.GetString()!)).Distinct().ToArray()).ToList();
        var ind = root.GetProperty("industryKeywords").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(e => StemPhrase(e.GetString()!)).ToArray());
        return new Rules(stop, groups, ind);
    }

    /// <summary>Very light stemmer: plural and -ing/-er forms.</summary>
    public static string Stem(string token)
    {
        var t = token.ToLowerInvariant();
        if (t.Length > 4 && t.EndsWith("ies", StringComparison.Ordinal)) return t[..^3] + "y";
        if (t.Length > 4 && t.EndsWith("ers", StringComparison.Ordinal)) return t[..^1];
        if (t.Length > 3 && t.EndsWith('s') && !t.EndsWith("ss", StringComparison.Ordinal)) return t[..^1];
        return t;
    }

    private static string StemPhrase(string phrase) => string.Join(' ', Normalizer.Tokens(phrase).Select(Stem));

    /// <summary>Normalised, stemmed, space-padded text suitable for <see cref="ContainsPhrase"/>.</summary>
    public static string Prepare(params string?[] parts) => " " + string.Join(' ', parts.SelectMany(Normalizer.Tokens).Select(Stem)) + " ";

    /// <summary>True when the prepared text contains the phrase on word boundaries. A phrase ending in a partial stem (e.g. "manufactur") matches word prefixes.</summary>
    public static bool ContainsPhrase(string preparedText, string stemmedPhrase)
        => stemmedPhrase.Length > 0 && (preparedText.Contains(" " + stemmedPhrase + " ", StringComparison.Ordinal) ||
            (stemmedPhrase.Length >= 6 && preparedText.Contains(" " + stemmedPhrase, StringComparison.Ordinal)));

    /// <summary>Meaningful stemmed tokens of a user phrase (stop-words removed).</summary>
    public static IReadOnlyList<string> KeyTokens(string? phrase)
        => Normalizer.Tokens(phrase).Select(Stem).Where(t => t.Length > 1 && !Loaded.Value.Stop.Contains(t)).Distinct().ToList();

    /// <summary>All phrases equivalent to <paramref name="stemmedToken"/> (itself plus its synonym groups).</summary>
    public static IReadOnlyList<string> Expand(string stemmedToken)
    {
        var set = new HashSet<string> { stemmedToken };
        foreach (var g in Loaded.Value.Groups)
            if (g.Contains(stemmedToken)) foreach (var x in g) set.Add(x);
        return set.ToList();
    }

    /// <summary>First phrase of any token's expansion found in the text, or null.</summary>
    public static string? FirstMatch(string preparedText, IEnumerable<string> tokens)
        => tokens.SelectMany(Expand).FirstOrDefault(p => ContainsPhrase(preparedText, p));

    /// <summary>Guesses an industry label from free text using the keyword table; null when nothing matches.</summary>
    public static string? GuessIndustry(params string?[] parts)
    {
        var text = Prepare(parts);
        string? best = null; var bestHits = 0;
        foreach (var (label, kws) in Loaded.Value.IndustryKeywords)
        {
            var hits = kws.Count(k => ContainsPhrase(text, k));
            if (hits > bestHits) { best = label; bestHits = hits; }
        }
        return best;
    }
}
