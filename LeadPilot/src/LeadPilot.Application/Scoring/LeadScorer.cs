using LeadPilot.Application.Models;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.Scoring;

/// <summary>What the customer is selling and to whom; the yardstick for relevance.</summary>
public sealed class ScoringProfile
{
    public string Product { get; set; } = "";
    public string TargetCustomer { get; set; } = "";
    public string Location { get; set; } = "";
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
}

/// <summary>Facts about a lead that the scorer looks at.</summary>
public sealed class LeadFacts
{
    public string Name { get; set; } = "";
    public string? Industry { get; set; }
    public string? Services { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public string? CompanySize { get; set; }
    public bool HasValidPhone { get; set; }
    public bool HasValidEmail { get; set; }
    public bool HasWebsite { get; set; }
    public bool HasContactPerson { get; set; }
    public string? IntentEvidence { get; set; }
}

/// <summary>One line of the score breakdown.</summary>
public sealed record FactorScore(string Name, double Weight, double Fraction, bool Applicable, string Explanation)
{
    public double Points => Applicable ? Weight * Fraction : 0;
}

/// <summary>Score, bucket and the full explanation.</summary>
public sealed class ScoreResult
{
    public int Score { get; init; }
    public LeadTemperature Temperature { get; init; }
    public IReadOnlyList<FactorScore> Factors { get; init; } = Array.Empty<FactorScore>();
    public const string Disclaimer = "This score measures how well the company matches your target market. It is not evidence that the company intends to buy.";

    /// <summary>Multi-line human-readable explanation.</summary>
    public string Explain() =>
        $"Relevance score {Score}/100 ({Temperature}).\n" +
        string.Join('\n', Factors.Select(f => f.Applicable
            ? $"• {f.Name}: {f.Points:0.#}/{f.Weight:0.#} – {f.Explanation}"
            : $"• {f.Name}: not scored – {f.Explanation}")) + "\n" + Disclaimer;
}

/// <summary>Explainable 0-100 relevance scoring. Factors that do not apply (e.g. no size requirement) are excluded and the rest re-scaled.</summary>
public static class LeadScorer
{
    private const double PhonePts = 6, EmailPts = 6, WebsitePts = 4, PersonPts = 2, AddressPts = 2, ContactMax = 20;

    /// <summary>Scores a lead against a profile with the given weights.</summary>
    public static ScoreResult Score(ScoringProfile p, LeadFacts f, ScoringWeights w)
    {
        var factors = new List<FactorScore>
        {
            Industry(p, f, w.Industry), Location(p, f, w.Location), Size(p, f, w.CompanySize),
            Product(p, f, w.ProductRelevance), Contact(f, w.ContactCompleteness), Intent(f, w.IntentEvidence)
        };
        var applicable = factors.Where(x => x.Applicable).Sum(x => x.Weight);
        var score = string.IsNullOrWhiteSpace(f.Name) || applicable <= 0 ? 0 : (int)Math.Round(100 * factors.Sum(x => x.Points) / applicable, MidpointRounding.AwayFromZero);
        score = Math.Clamp(score, 0, 100);
        return new ScoreResult { Score = score, Temperature = Bucket(score, w), Factors = factors };
    }

    /// <summary>Maps a score to Hot/Warm/Cold/Unqualified using the configured thresholds.</summary>
    public static LeadTemperature Bucket(int score, ScoringWeights w) =>
        score >= w.HotThreshold ? LeadTemperature.Hot : score >= w.WarmThreshold ? LeadTemperature.Warm : score >= w.ColdThreshold ? LeadTemperature.Cold : LeadTemperature.Unqualified;

    private static FactorScore Industry(ScoringProfile p, LeadFacts f, double w)
    {
        var tokens = KeywordMatcher.KeyTokens(p.TargetCustomer + " " + p.Industry);
        if (tokens.Count == 0) return new("Target industry", w, 0, false, "no target customer type given");
        var inField = KeywordMatcher.FirstMatch(KeywordMatcher.Prepare(f.Industry), tokens);
        if (inField != null) return new("Target industry", w, 1, true, $"industry \"{f.Industry}\" matches \"{inField}\"");
        var inText = KeywordMatcher.FirstMatch(KeywordMatcher.Prepare(f.Name, f.Services, f.Description), tokens);
        if (inText != null) return new("Target industry", w, 0.6, true, $"company text mentions \"{inText}\" (industry field does not confirm it)");
        return new("Target industry", w, 0, true, string.IsNullOrWhiteSpace(f.Industry) ? "industry unknown and nothing in the text matches" : $"industry \"{f.Industry}\" does not match the target");
    }

    private static FactorScore Location(ScoringProfile p, LeadFacts f, double w)
    {
        var targets = (p.Location ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Normalizer.NormalizeText).Where(t => t.Length >= 2).ToList();
        if (targets.Count == 0) return new("Target location", w, 0, false, "no target location given");
        var near = " " + string.Join(' ', Normalizer.Tokens(f.City + " " + f.Region)) + " ";
        var addr = " " + string.Join(' ', Normalizer.Tokens(f.Address)) + " ";
        var country = " " + string.Join(' ', Normalizer.Tokens(f.Country)) + " ";
        foreach (var t in targets)
        {
            if (near.Contains(" " + t + " ", StringComparison.Ordinal)) return new("Target location", w, 1, true, $"located in {t}");
            if (addr.Contains(" " + t + " ", StringComparison.Ordinal)) return new("Target location", w, 0.8, true, $"address mentions {t}");
            if (country.Contains(" " + t + " ", StringComparison.Ordinal)) return new("Target location", w, 0.5, true, $"in the target country ({t}) but city not confirmed");
        }
        var known = string.IsNullOrWhiteSpace(f.City + f.Region + f.Address + f.Country);
        return new("Target location", w, 0, true, known ? "location unknown" : $"located elsewhere ({f.City ?? f.Region ?? f.Country})");
    }

    private static FactorScore Size(ScoringProfile p, LeadFacts f, double w)
    {
        if (string.IsNullOrWhiteSpace(p.CompanySize)) return new("Company size", w, 0, false, "no size requirement");
        if (string.IsNullOrWhiteSpace(f.CompanySize)) return new("Company size", w, 0, true, "company size unknown");
        var match = string.Equals(Normalizer.NormalizeText(p.CompanySize), Normalizer.NormalizeText(f.CompanySize), StringComparison.Ordinal);
        return new("Company size", w, match ? 1 : 0, true, match ? $"size {f.CompanySize} matches" : $"size {f.CompanySize} differs from {p.CompanySize}");
    }

    private static FactorScore Product(ScoringProfile p, LeadFacts f, double w)
    {
        var tokens = KeywordMatcher.KeyTokens(p.Product);
        if (tokens.Count == 0) return new("Product/service relevance", w, 0, false, "no product given");
        var text = KeywordMatcher.Prepare(f.Name, f.Industry, f.Services, f.Description);
        var matched = tokens.Where(t => KeywordMatcher.Expand(t).Any(x => KeywordMatcher.ContainsPhrase(text, x))).ToList();
        var frac = matched.Count / (double)tokens.Count;
        return new("Product/service relevance", w, frac, true, matched.Count == 0 ? "company text has no mention of your product terms" : $"company text relates to {string.Join(", ", matched)}");
    }

    private static FactorScore Contact(LeadFacts f, double w)
    {
        var pts = (f.HasValidPhone ? PhonePts : 0) + (f.HasValidEmail ? EmailPts : 0) + (f.HasWebsite ? WebsitePts : 0) + (f.HasContactPerson ? PersonPts : 0) + (!string.IsNullOrWhiteSpace(f.Address) ? AddressPts : 0);
        var missing = new List<string>();
        if (!f.HasValidPhone) missing.Add("phone"); if (!f.HasValidEmail) missing.Add("e-mail"); if (!f.HasWebsite) missing.Add("website");
        if (!f.HasContactPerson) missing.Add("contact person"); if (string.IsNullOrWhiteSpace(f.Address)) missing.Add("address");
        return new("Contact completeness", w, pts / ContactMax, true, missing.Count == 0 ? "all contact fields present" : "missing " + string.Join(", ", missing));
    }

    private static FactorScore Intent(LeadFacts f, double w) =>
        string.IsNullOrWhiteSpace(f.IntentEvidence)
            ? new("Buying-intent evidence", w, 0, true, "none recorded (relevance is not intent)")
            : new("Buying-intent evidence", w, 1, true, "recorded: " + f.IntentEvidence);
}
