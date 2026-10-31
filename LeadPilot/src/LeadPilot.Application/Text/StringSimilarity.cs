namespace LeadPilot.Application.Text;

/// <summary>String similarity measures in the range 0..1.</summary>
public static class StringSimilarity
{
    /// <summary>Jaro-Winkler similarity (prefix scale 0.1, max prefix 4).</summary>
    public static double JaroWinkler(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 1;
        if (a.Length == 0 || b.Length == 0) return 0;
        if (a == b) return 1;
        var range = Math.Max(Math.Max(a.Length, b.Length) / 2 - 1, 0);
        var am = new bool[a.Length]; var bm = new bool[b.Length];
        var matches = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var lo = Math.Max(0, i - range); var hi = Math.Min(b.Length - 1, i + range);
            for (var j = lo; j <= hi; j++)
            {
                if (bm[j] || a[i] != b[j]) continue;
                am[i] = bm[j] = true; matches++; break;
            }
        }
        if (matches == 0) return 0;
        var k = 0; var trans = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (!am[i]) continue;
            while (!bm[k]) k++;
            if (a[i] != b[k]) trans++;
            k++;
        }
        double m = matches;
        var jaro = (m / a.Length + m / b.Length + (m - trans / 2.0) / m) / 3.0;
        var prefix = 0;
        for (var i = 0; i < Math.Min(4, Math.Min(a.Length, b.Length)) && a[i] == b[i]; i++) prefix++;
        return jaro + prefix * 0.1 * (1 - jaro);
    }

    /// <summary>Sørensen–Dice coefficient over token sets.</summary>
    public static double TokenDice(IReadOnlyCollection<string> a, IReadOnlyCollection<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var sa = new HashSet<string>(a); var sb = new HashSet<string>(b);
        var inter = sa.Count(sb.Contains);
        return 2.0 * inter / (sa.Count + sb.Count);
    }

    /// <summary>Best of Jaro-Winkler on the whole string and token Dice, both on normalised company names.</summary>
    public static double NameSimilarity(string? a, string? b)
    {
        var ca = Normalizer.CoreName(a); var cb = Normalizer.CoreName(b);
        if (ca.Length == 0 || cb.Length == 0) return 0;
        return Math.Max(JaroWinkler(ca, cb), TokenDice(ca.Split(' '), cb.Split(' ')));
    }

    /// <summary>Token Dice similarity of two free-text addresses.</summary>
    public static double AddressSimilarity(string? a, string? b) => TokenDice(Normalizer.Tokens(a), Normalizer.Tokens(b));
}
