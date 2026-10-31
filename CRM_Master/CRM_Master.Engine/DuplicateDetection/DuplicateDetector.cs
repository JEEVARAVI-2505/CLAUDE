using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.DuplicateDetection;

/// <summary>
/// Local duplicate detection: blocking (so 50,000 contacts never need 1.25 billion comparisons), weighted scoring
/// from email / phone / name / company evidence, and classification into Exact / High / Possible / Low.
/// </summary>
public static class DuplicateDetector
{
    public static ScanResult Scan(IReadOnlyList<Contact> contacts, DuplicateOptions o, ISet<(int, int)>? knownSeparate = null,
        IProgress<ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var byId = new Dictionary<int, Contact>(contacts.Count);
        foreach (var c in contacts)
        {
            // keys normally come from the database; compute them for in-memory contacts
            if (c.EmailKey is null && c.PhoneKey is null && c.NameKey is null) ContactKeys.Apply(c, o.DefaultCountryCode);
            byId[c.Id] = c;
        }

        var blocks = BuildBlocks(contacts, o);
        var seen = new HashSet<long>(); var pairs = new List<DuplicatePair>(); int candidates = 0, skipped = 0, done = 0;
        foreach (var (key, ids) in blocks)
        {
            if ((++done & 0x1FF) == 0) { ct.ThrowIfCancellationRequested(); progress?.Report(new ProgressInfo("Comparing records", done, blocks.Count)); }
            var limit = key.StartsWith("n:", StringComparison.Ordinal) || key.StartsWith("s:", StringComparison.Ordinal) ? o.MaxFuzzyBlockSize : o.MaxExactBlockSize;
            if (ids.Count > limit) { skipped++; continue; }
            for (var i = 0; i < ids.Count; i++)
                for (var j = i + 1; j < ids.Count; j++)
                {
                    int a = Math.Min(ids[i], ids[j]), b = Math.Max(ids[i], ids[j]);
                    if (a == b || !seen.Add(((long)a << 32) | (uint)b)) continue;
                    if (knownSeparate?.Contains((a, b)) == true) continue;
                    candidates++;
                    var pair = Compare(byId[a], byId[b], o);
                    if (pair is not null) pairs.Add(pair);
                }
        }
        progress?.Report(new ProgressInfo("Comparing records", blocks.Count, blocks.Count));
        return new ScanResult(Cluster(pairs), contacts.Count, candidates, skipped);
    }

    private static Dictionary<string, List<int>> BuildBlocks(IReadOnlyList<Contact> contacts, DuplicateOptions o)
    {
        var blocks = new Dictionary<string, List<int>>(contacts.Count * 2);
        void Add(string key, int id) { if (!blocks.TryGetValue(key, out var l)) blocks[key] = l = new(2); l.Add(id); }
        foreach (var c in contacts)
        {
            if (o.MatchEmail && c.EmailKey is not null) Add("e:" + c.EmailKey, c.Id);
            if (o.MatchPhone)
            {
                if (c.PhoneKey is not null) Add("p:" + c.PhoneKey, c.Id);
                if (c.AlternatePhoneKey is not null && c.AlternatePhoneKey != c.PhoneKey) Add("p:" + c.AlternatePhoneKey, c.Id);
            }
            if (o.MatchName && c.NameKey is not null)
            {
                Add("n:" + c.NameKey, c.Id);                               // same words, any order
                if (o.UseFuzzy)
                {
                    var tokens = c.NameKey.Split(' ');
                    if (tokens.Length >= 2) Add("s:" + string.Join("|", new[] { StringSimilarity.Soundex(tokens[0]), StringSimilarity.Soundex(tokens[^1]) }.Order()), c.Id);
                    else Add("s:" + StringSimilarity.Soundex(tokens[0]) + "|1", c.Id);
                }
            }
        }
        return blocks;
    }

    private static string? NameOf(Contact c) =>
        !string.IsNullOrWhiteSpace(c.FullName) ? c.FullName : string.IsNullOrWhiteSpace($"{c.FirstName} {c.LastName}") ? null : $"{c.FirstName} {c.LastName}";

    /// <summary>Scores one pair; null when below the lowest threshold or when the evidence is not enabled.</summary>
    public static DuplicatePair? Compare(Contact a, Contact b, DuplicateOptions o)
    {
        var emailEq = o.MatchEmail && a.EmailKey is not null && a.EmailKey == b.EmailKey;
        var phonesA = new[] { a.PhoneKey, a.AlternatePhoneKey }.Where(k => k is not null).ToArray();
        var phoneEq = o.MatchPhone && phonesA.Any(k => b.PhoneKey == k || b.AlternatePhoneKey == k);

        double? nameSim = null;
        if (o.MatchName && NameOf(a) is { } na && NameOf(b) is { } nb)
        {
            var s = StringSimilarity.NameSimilarity(na, nb);
            nameSim = o.UseFuzzy || s >= 0.999 ? s : 0;
        }
        double? compSim = null;
        if (o.MatchCompany && !string.IsNullOrWhiteSpace(a.CompanyName) && !string.IsNullOrWhiteSpace(b.CompanyName)) compSim = CompanyRules.Similarity(a.CompanyName, b.CompanyName);

        var on = new List<string>(); double score = 0;
        if (emailEq) { score = 0.90 + (nameSim >= 0.85 ? 0.05 : 0) + (phoneEq ? 0.05 : 0); on.Add("Email"); }
        if (phoneEq)
        {
            var s = 0.82 + 0.13 * (nameSim ?? 0.4);
            if (s > score) score = s;
            on.Add("Phone");
        }
        if (nameSim is { } ns && ns >= o.LowThreshold)
        {
            var s = compSim is { } cs ? 0.6 * ns + 0.4 * cs : 0.8 * ns;
            if (s > score) score = s;
            on.Add(ns >= 0.999 ? "Name" : "Name (similar)");
            if (compSim is >= 0.9) on.Add("Company");
        }
        if (score < o.LowThreshold) return null;

        MatchClass cls;
        if (IsExact(a, b)) { cls = MatchClass.Exact; score = 1.0; }
        else cls = score >= o.HighThreshold ? MatchClass.High : score >= o.PossibleThreshold ? MatchClass.Possible : MatchClass.Low;
        return new DuplicatePair(a.Id, b.Id, Math.Round(score, 4), cls, string.Join(" + ", on.Distinct()));
    }

    /// <summary>Exact: every populated field that both records have (name, email, phone, company) is identical, and one is an identifier.</summary>
    public static bool IsExact(Contact a, Contact b)
    {
        var comparable = 0; var identifier = false;
        bool Eq(string? x, string? y, bool id)
        {
            if (x is null || y is null) return true;     // not comparable
            comparable++; if (id) identifier = true;
            return x == y;
        }
        var nameA = a.NameKey; var nameB = b.NameKey;
        if (!Eq(nameA, nameB, false)) return false;
        if (!Eq(a.EmailKey, b.EmailKey, true)) return false;
        if (!Eq(a.PhoneKey, b.PhoneKey, true)) return false;
        var ca = CompanyRules.Key(a.CompanyName); var cb = CompanyRules.Key(b.CompanyName);
        if (!Eq(ca.Length == 0 ? null : ca, cb.Length == 0 ? null : cb, false)) return false;
        return comparable >= 2 && identifier;
    }

    /// <summary>Groups pairs: Exact/High/Possible pairs are merged into connected components; each Low pair outside those stays a pair.</summary>
    public static List<DuplicateCluster> Cluster(IReadOnlyList<DuplicatePair> pairs)
    {
        var parent = new Dictionary<int, int>();
        int Find(int x) { if (!parent.TryAdd(x, x)) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } } return x; }
        void Union(int a, int b) { var ra = Find(a); var rb = Find(b); if (ra != rb) parent[Math.Max(ra, rb)] = Math.Min(ra, rb); }

        foreach (var p in pairs.Where(p => p.Class >= MatchClass.Possible)) Union(p.AId, p.BId);
        var clusters = new Dictionary<int, DuplicateCluster>();
        foreach (var p in pairs.Where(p => p.Class >= MatchClass.Possible))
        {
            var root = Find(p.AId);
            if (!clusters.TryGetValue(root, out var cl)) clusters[root] = cl = new DuplicateCluster();
            cl.Pairs.Add(p);
        }
        var result = clusters.Values.ToList();
        foreach (var cl in result) { var ids = cl.Pairs.SelectMany(p => new[] { p.AId, p.BId }).Distinct().Order().ToList(); cl.ContactIds.AddRange(ids); }
        foreach (var p in pairs.Where(p => p.Class == MatchClass.Low))
        {
            var inside = result.Any(c => c.ContactIds.Contains(p.AId) && c.ContactIds.Contains(p.BId));
            if (inside) { result.First(c => c.ContactIds.Contains(p.AId) && c.ContactIds.Contains(p.BId)).Pairs.Add(p); continue; }
            var lc = new DuplicateCluster(); lc.ContactIds.AddRange(new[] { p.AId, p.BId }); lc.Pairs.Add(p); result.Add(lc);
        }
        return result.OrderByDescending(c => c.Class).ThenByDescending(c => c.TopScore).ThenBy(c => c.ContactIds[0]).ToList();
    }

    /// <summary>Field-by-field comparison for the side-by-side view: matching and conflicting fields are flagged.</summary>
    public static List<FieldComparison> CompareFields(Contact left, Contact right)
    {
        var list = new List<FieldComparison>();
        foreach (var f in Contact.Fields)
        {
            var l = left.Get(f.Field); var r = right.Get(f.Field);
            var status = (l, r) switch
            {
                (null, null) => FieldCompareStatus.BothEmpty,
                (not null, null) => FieldCompareStatus.OnlyLeft,
                (null, not null) => FieldCompareStatus.OnlyRight,
                _ => SameValue(f.Field, l!, r!) ? FieldCompareStatus.Match : FieldCompareStatus.Conflict
            };
            list.Add(new FieldComparison(f.Field, f.Label, l, r, status));
        }
        return list;
    }

    private static bool SameValue(ContactField f, string a, string b) => f switch
    {
        ContactField.Email => ContactKeys.EmailKey(a) == ContactKeys.EmailKey(b),
        ContactField.Phone or ContactField.AlternatePhone => ContactKeys.PhoneKey(a, "91") == ContactKeys.PhoneKey(b, "91"),
        ContactField.Website => UrlRules.HostKey(a) == UrlRules.HostKey(b) && UrlRules.HostKey(a) is not null || string.Equals(a, b, StringComparison.OrdinalIgnoreCase),
        ContactField.Company => CompanyRules.Key(a) == CompanyRules.Key(b),
        _ => string.Equals(StringSimilarity.NormalizeText(a), StringSimilarity.NormalizeText(b), StringComparison.Ordinal)
    };
}
