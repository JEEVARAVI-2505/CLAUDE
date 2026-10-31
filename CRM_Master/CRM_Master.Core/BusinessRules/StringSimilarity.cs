using System.Globalization;
using System.Text;

namespace CRM_Master.Core.BusinessRules;

/// <summary>Deterministic string-distance and similarity algorithms used by fuzzy matching.</summary>
public static class StringSimilarity
{
    /// <summary>Classic Levenshtein edit distance (insert / delete / substitute).</summary>
    public static int Levenshtein(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var prev = new int[b.Length + 1]; var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }

    /// <summary>Optimal-string-alignment distance: Levenshtein plus adjacent transposition (gmial -> gmail = 1).</summary>
    public static int OsaDistance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1]) d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        return d[a.Length, b.Length];
    }

    /// <summary>1 - Levenshtein/maxLength, in [0,1].</summary>
    public static double LevenshteinRatio(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 1;
        return 1.0 - (double)Levenshtein(a, b) / Math.Max(a.Length, b.Length);
    }

    /// <summary>Jaro similarity in [0,1].</summary>
    public static double Jaro(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 1;
        if (a.Length == 0 || b.Length == 0) return 0;
        var window = Math.Max(Math.Max(a.Length, b.Length) / 2 - 1, 0);
        var aMatched = new bool[a.Length]; var bMatched = new bool[b.Length];
        var matches = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var lo = Math.Max(0, i - window); var hi = Math.Min(b.Length - 1, i + window);
            for (var j = lo; j <= hi; j++)
            {
                if (bMatched[j] || a[i] != b[j]) continue;
                aMatched[i] = bMatched[j] = true; matches++; break;
            }
        }
        if (matches == 0) return 0;
        var transpositions = 0; var k = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (!aMatched[i]) continue;
            while (!bMatched[k]) k++;
            if (a[i] != b[k]) transpositions++;
            k++;
        }
        var m = (double)matches;
        return (m / a.Length + m / b.Length + (m - transpositions / 2.0) / m) / 3.0;
    }

    /// <summary>Jaro-Winkler (prefix scale 0.1, max prefix 4, boost threshold 0.7).</summary>
    public static double JaroWinkler(string a, string b)
    {
        var j = Jaro(a, b);
        if (j < 0.7) return j;
        var prefix = 0;
        for (var i = 0; i < Math.Min(4, Math.Min(a.Length, b.Length)) && a[i] == b[i]; i++) prefix++;
        return j + prefix * 0.1 * (1 - j);
    }

    /// <summary>Lower-case letters/digits only, accents removed, tokens separated by single spaces.</summary>
    public static string NormalizeText(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var decomposed = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Similarity of two personal names in [0,1]; tolerant of word order ("Smith John"), initials ("J Smith")
    /// and small spelling differences ("Jon" / "John").
    /// </summary>
    public static double NameSimilarity(string? a, string? b)
    {
        var na = NormalizeText(a); var nb = NormalizeText(b);
        if (na.Length == 0 || nb.Length == 0) return 0;
        if (na == nb) return 1;
        var ta = na.Split(' '); var tb = nb.Split(' ');
        if (ta.Length == 1 && tb.Length == 1) return JaroWinkler(na, nb);
        var shortT = ta.Length <= tb.Length ? ta : tb; var longT = ta.Length <= tb.Length ? tb : ta;
        var used = new bool[longT.Length]; double total = 0;
        foreach (var s in shortT)
        {
            var best = 0.0; var bestIdx = -1;
            for (var i = 0; i < longT.Length; i++)
            {
                if (used[i]) continue;
                var sim = TokenSimilarity(s, longT[i]);
                if (sim > best) { best = sim; bestIdx = i; }
            }
            if (bestIdx >= 0) used[bestIdx] = true;
            total += best;
        }
        var coverage = total / shortT.Length;
        var lengthPenalty = 1.0 - 0.08 * (longT.Length - shortT.Length); // "John Smith" vs "John Michael Smith" scores a little lower
        return Math.Clamp(coverage * Math.Max(lengthPenalty, 0.6), 0, 1);
    }

    private static double TokenSimilarity(string a, string b)
    {
        if (a == b) return 1;
        if ((a.Length == 1 && b.StartsWith(a, StringComparison.Ordinal)) || (b.Length == 1 && a.StartsWith(b, StringComparison.Ordinal))) return 0.85; // initial
        return JaroWinkler(a, b);
    }

    /// <summary>American Soundex code (letter + 3 digits); empty for input without letters. Used as a cheap blocking key.</summary>
    public static string Soundex(string? s)
    {
        var letters = NormalizeText(s).Where(char.IsLetter).ToArray();
        if (letters.Length == 0) return "";
        const string codes = "01230120022455012623010202"; // a..z
        var sb = new StringBuilder().Append(char.ToUpperInvariant(letters[0]));
        var last = codes[char.ToLowerInvariant(letters[0]) - 'a'];
        for (var i = 1; i < letters.Length && sb.Length < 4; i++)
        {
            var c = char.ToLowerInvariant(letters[i]);
            if (c is < 'a' or > 'z') continue;
            var code = codes[c - 'a'];
            if (code != '0' && code != last) sb.Append(code);
            if (c is not ('h' or 'w')) last = code;
        }
        return sb.ToString().PadRight(4, '0');
    }
}
