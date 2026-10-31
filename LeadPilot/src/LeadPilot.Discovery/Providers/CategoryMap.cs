using System.Reflection;
using System.Text.Json;
using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Application.Text;

namespace LeadPilot.Discovery.Providers;

/// <summary>Maps free-text customer types ("manufacturing companies") to OpenStreetMap tags and name patterns. Rules live in <c>category-map.json</c>.</summary>
public sealed class CategoryMap
{
    public sealed record Entry(string[] Keywords, string[] Tags, string NameRegex, string Industry);

    private static readonly Lazy<IReadOnlyList<Entry>> Entries = new(Load);

    private static IReadOnlyList<Entry> Load()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("LeadPilot.Discovery.Rules.category-map.json")
                      ?? throw new InvalidOperationException("category-map.json resource missing");
        using var doc = JsonDocument.Parse(s);
        return doc.RootElement.GetProperty("categories").EnumerateArray().Select(e => new Entry(
            e.GetProperty("keywords").EnumerateArray().Select(x => x.GetString()!).ToArray(),
            e.GetProperty("tags").EnumerateArray().Select(x => x.GetString()!).ToArray(),
            e.GetProperty("nameRegex").GetString()!, e.GetProperty("industry").GetString()!)).ToList();
    }

    /// <summary>Entries whose keywords occur in the request's target-customer / category / industry text.</summary>
    public static IReadOnlyList<Entry> Match(DiscoveryRequest r)
    {
        var text = " " + Normalizer.NormalizeText($"{r.TargetCustomer} {r.BusinessCategory} {r.Industry}") + " ";
        return Entries.Value.Where(e => e.Keywords.Any(k => text.Contains(" " + Normalizer.NormalizeText(k).TrimEnd(), StringComparison.Ordinal))).ToList();
    }

    /// <summary>Fallback name pattern built from the customer-type words themselves.</summary>
    public static string FallbackRegex(DiscoveryRequest r) =>
        string.Join('|', KeywordMatcher.KeyTokens($"{r.TargetCustomer} {r.BusinessCategory}").Where(t => t.Length >= 4).Select(System.Text.RegularExpressions.Regex.Escape).Take(4));
}
