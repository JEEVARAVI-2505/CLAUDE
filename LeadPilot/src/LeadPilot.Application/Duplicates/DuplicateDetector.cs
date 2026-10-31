using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.Duplicates;

/// <summary>Lightweight projection of a lead used for duplicate matching.</summary>
public sealed class MatchProfile
{
    public int LeadId { get; init; }
    public string LeadCode { get; init; } = "";
    public string Name { get; init; } = "";
    public IReadOnlyList<string> Phones { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Emails { get; init; } = Array.Empty<string>();
    public string? Domain { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }

    /// <summary>Builds a profile from raw (un-normalised) values.</summary>
    public static MatchProfile From(int leadId, string leadCode, string name, string? website, IEnumerable<string?> phones, IEnumerable<string?> emails, string? address, string? city, string region = "IN") => new()
    {
        LeadId = leadId, LeadCode = leadCode, Name = name,
        Phones = phones.Select(p => Normalizer.NormalizePhone(p, region)).Where(p => p != null).Select(p => p!).Distinct().ToArray(),
        Emails = emails.Select(Normalizer.NormalizeEmail).Where(e => e != null).Select(e => e!).Distinct().ToArray(),
        Domain = Normalizer.DomainOf(website), Address = address, City = city
    };
}

/// <summary>Outcome of comparing two profiles.</summary>
public sealed record DuplicateResult(int LeadAId, int LeadBId, double Score, DuplicateConfidence Confidence, IReadOnlyList<string> Reasons);

/// <summary>
/// Weighted duplicate scoring. Each signal contributes a probability p_i; the final score is the
/// noisy-OR 1 - Π(1 - p_i). Contributions: phone 0.80, e-mail 0.85, website domain 0.80 (exact matches),
/// name 0.65·sim⁴, address 0.45·sim (only when sim ≥ 0.6). Thresholds: High ≥ 0.80, Medium ≥ 0.55, Low ≥ 0.35.
/// </summary>
public static class DuplicateDetector
{
    public const double HighThreshold = 0.80, MediumThreshold = 0.55, LowThreshold = 0.35;

    private static readonly HashSet<string> SharedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "facebook.com", "instagram.com", "linkedin.com", "justdial.com", "indiamart.com", "linktr.ee", "wixsite.com", "business.site", "google.com"
    };

    /// <summary>Compares two profiles; returns null when the score is below the Low threshold.</summary>
    public static DuplicateResult? Compare(MatchProfile a, MatchProfile b)
    {
        if (a.LeadId == b.LeadId) return null;
        var reasons = new List<string>();
        var miss = 1.0;

        if (a.Phones.Intersect(b.Phones).FirstOrDefault() is { } ph) { miss *= 1 - 0.80; reasons.Add($"Same phone {ph}"); }
        if (a.Emails.Intersect(b.Emails).FirstOrDefault() is { } em) { miss *= 1 - 0.85; reasons.Add($"Same e-mail {em}"); }
        if (a.Domain != null && a.Domain == b.Domain && !SharedDomains.Contains(a.Domain)) { miss *= 1 - 0.80; reasons.Add($"Same website {a.Domain}"); }

        var nameSim = StringSimilarity.NameSimilarity(a.Name, b.Name);
        if (nameSim >= 0.75)
        {
            miss *= 1 - 0.65 * Math.Pow(nameSim, 4);
            reasons.Add($"Company names {(nameSim >= 0.999 ? "identical" : $"similar ({nameSim:P0})")}");
        }

        var addrSim = StringSimilarity.AddressSimilarity(a.Address, b.Address);
        if (addrSim >= 0.6) { miss *= 1 - 0.45 * addrSim; reasons.Add($"Addresses similar ({addrSim:P0})"); }

        var score = Math.Round(1 - miss, 4);
        if (score < LowThreshold) return null;
        var conf = score >= HighThreshold ? DuplicateConfidence.High : score >= MediumThreshold ? DuplicateConfidence.Medium : DuplicateConfidence.Low;
        var (x, y) = a.LeadId < b.LeadId ? (a, b) : (b, a);
        return new DuplicateResult(x.LeadId, y.LeadId, score, conf, reasons);
    }

    /// <summary>Finds matches for <paramref name="candidate"/> within <paramref name="pool"/> using token blocking.</summary>
    public static IReadOnlyList<DuplicateResult> FindMatches(MatchProfile candidate, IEnumerable<MatchProfile> pool)
    {
        var tokens = BlockTokens(candidate);
        var res = new List<DuplicateResult>();
        foreach (var p in pool)
        {
            if (p.LeadId == candidate.LeadId) continue;
            if (!SharesBlock(candidate, tokens, p)) continue;
            if (Compare(candidate, p) is { } r) res.Add(r);
        }
        return res.OrderByDescending(r => r.Score).ToList();
    }

    /// <summary>Scans a whole pool for duplicate pairs (each pair reported once).</summary>
    public static IReadOnlyList<DuplicateResult> FindAll(IReadOnlyList<MatchProfile> profiles)
    {
        var blocks = new Dictionary<string, List<int>>();
        for (var i = 0; i < profiles.Count; i++)
            foreach (var key in BlockKeys(profiles[i]))
            {
                if (!blocks.TryGetValue(key, out var l)) blocks[key] = l = new List<int>();
                l.Add(i);
            }
        var seen = new HashSet<(int, int)>(); var res = new List<DuplicateResult>();
        foreach (var l in blocks.Values)
        {
            if (l.Count > 400) continue; // very common token: skip to keep this near-linear
            for (var x = 0; x < l.Count; x++)
                for (var y = x + 1; y < l.Count; y++)
                {
                    var key = (Math.Min(l[x], l[y]), Math.Max(l[x], l[y]));
                    if (!seen.Add(key)) continue;
                    if (Compare(profiles[l[x]], profiles[l[y]]) is { } r) res.Add(r);
                }
        }
        return res.OrderByDescending(r => r.Score).ToList();
    }

    private static IEnumerable<string> BlockKeys(MatchProfile p)
    {
        foreach (var ph in p.Phones) yield return "p:" + ph;
        foreach (var em in p.Emails) yield return "e:" + em;
        if (p.Domain != null) yield return "d:" + p.Domain;
        foreach (var t in Normalizer.CoreName(p.Name).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length >= 4)) yield return "n:" + t;
    }

    private static HashSet<string> BlockTokens(MatchProfile p) => new(BlockKeys(p));

    private static bool SharesBlock(MatchProfile candidate, HashSet<string> keys, MatchProfile other) => BlockKeys(other).Any(keys.Contains);

    internal static IEnumerable<string> Keys(MatchProfile p) => BlockKeys(p);
}

/// <summary>Incremental blocking index so that importing N rows against M leads is not O(N·M).</summary>
public sealed class DuplicateIndex
{
    private readonly Dictionary<string, List<MatchProfile>> _blocks = new();

    /// <summary>Builds an index from existing profiles.</summary>
    public DuplicateIndex(IEnumerable<MatchProfile> profiles) { foreach (var p in profiles) Add(p); }

    /// <summary>Adds a profile.</summary>
    public void Add(MatchProfile p)
    {
        foreach (var k in DuplicateDetector.Keys(p))
        {
            if (!_blocks.TryGetValue(k, out var l)) _blocks[k] = l = new List<MatchProfile>();
            l.Add(p);
        }
    }

    /// <summary>Returns duplicate results for the candidate, best first.</summary>
    public IReadOnlyList<DuplicateResult> Find(MatchProfile candidate)
    {
        var seen = new HashSet<int>(); var res = new List<DuplicateResult>();
        foreach (var k in DuplicateDetector.Keys(candidate))
        {
            if (!_blocks.TryGetValue(k, out var l) || l.Count > 400) continue;
            foreach (var p in l)
                if (seen.Add(p.LeadId) && DuplicateDetector.Compare(candidate, p) is { } r) res.Add(r);
        }
        return res.OrderByDescending(r => r.Score).ToList();
    }
}
