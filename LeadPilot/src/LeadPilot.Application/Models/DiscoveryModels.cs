namespace LeadPilot.Application.Models;

/// <summary>A single field value with where it came from (kept as field-level evidence).</summary>
public sealed class CandidateEvidence
{
    public string Field { get; init; } = "";
    public string Value { get; init; } = "";
    public string? SourceUrl { get; init; }
    public double Confidence { get; init; } = 0.8;
    public string? Note { get; init; }
}

/// <summary>A prospect returned by a provider or file import, before it becomes a stored lead.</summary>
public sealed class ProspectCandidate
{
    public string Name { get; set; } = "";
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? Industry { get; set; }
    public string? Services { get; set; }
    public string? Description { get; set; }
    public string? CompanySize { get; set; }
    public string? ContactName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? IntentEvidence { get; set; }
    public string? Status { get; set; }
    public string SourceName { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string? SourceUrl { get; set; }
    public string? ExternalId { get; set; }
    public string? License { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    /// <summary>True when contact details were found in a public listing (rather than typed by a user).</summary>
    public bool IsPublicListing { get; set; }
    public List<CandidateEvidence> Evidence { get; } = new();
}

/// <summary>The four required inputs plus optional filters.</summary>
public sealed class DiscoveryRequest
{
    public string Product { get; set; } = "";
    public string TargetCustomer { get; set; } = "";
    public string Location { get; set; } = "";
    public int Count { get; set; } = 25;
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
    public string? BusinessCategory { get; set; }
    public string? JobTitles { get; set; }
    public bool RequireWebsite { get; set; }
    public List<string> ExcludedCompanies { get; set; } = new();
    public int MinScore { get; set; }

    /// <summary>Returns a validation message, or null when the required inputs are present.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Product)) return "Enter the product or service you offer.";
        if (string.IsNullOrWhiteSpace(TargetCustomer)) return "Enter the type of customer you want to reach.";
        if (string.IsNullOrWhiteSpace(Location)) return "Enter a target location.";
        if (Count < 1 || Count > 1000) return "Number of prospects must be between 1 and 1000.";
        return null;
    }
}

/// <summary>One page of provider results. <see cref="NextPageToken"/> is null on the last page.</summary>
public sealed record ProviderPage(IReadOnlyList<ProspectCandidate> Candidates, string? NextPageToken, IReadOnlyList<string> Warnings)
{
    public static ProviderPage Empty(params string[] warnings) => new(Array.Empty<ProspectCandidate>(), null, warnings);
}

/// <summary>Runtime configuration handed to a provider call.</summary>
public sealed record ProviderSettings(string? ApiKey, string? Endpoint, string? ContactEmail);

/// <summary>Whether a provider can be used right now, and why not.</summary>
public sealed record ProviderAvailability(bool IsAvailable, string Message);

/// <summary>Per-provider outcome of a discovery run.</summary>
public sealed class ProviderRunResult
{
    public string ProviderId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public bool Succeeded { get; set; }
    public int Returned { get; set; }
    public int Requests { get; set; }
    public string? Error { get; set; }
    public List<string> Warnings { get; } = new();
}

/// <summary>Everything returned from one discovery run.</summary>
public sealed class DiscoveryRun
{
    public DiscoveryRequest Request { get; init; } = new();
    public List<ProspectCandidate> Candidates { get; } = new();
    public List<ProviderRunResult> Providers { get; } = new();
    public bool AnyProviderSucceeded => Providers.Any(p => p.Succeeded);
}

/// <summary>Result of fetching and parsing a company website.</summary>
public sealed class WebsiteExtraction
{
    public string Url { get; init; } = "";
    public string? FinalUrl { get; set; }
    public bool Success { get; set; }
    public bool BlockedByRobots { get; set; }
    public string? Error { get; set; }
    public int? StatusCode { get; set; }
    public string? CompanyName { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? ContactPageUrl { get; set; }
    public List<string> Phones { get; } = new();
    public List<string> Emails { get; } = new();
    public List<string> Services { get; } = new();
    public string? AboutText { get; set; }
    public List<string> SourceUrls { get; } = new();
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Which page each phone/e-mail was found on.</summary>
    public Dictionary<string, string> ValueSources { get; } = new();
}

/// <summary>Outcome of probing a website URL.</summary>
public sealed record WebsiteProbeResult(bool Reachable, int? StatusCode, string? FinalUrl, bool Redirected, string? Error);

/// <summary>Outcome of DNS checks for an e-mail domain.</summary>
public sealed record DnsCheck(bool DomainExists, bool HasMailServer, string? Error);

/// <summary>A possible official website for a company name.</summary>
public sealed record WebsiteCandidate(string Url, string Title, string SourceName);
