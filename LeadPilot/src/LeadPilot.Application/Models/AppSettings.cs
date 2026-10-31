namespace LeadPilot.Application.Models;

/// <summary>User-adjustable relevance weights. Defaults sum to 100.</summary>
public sealed class ScoringWeights
{
    public double Industry { get; set; } = 25;
    public double Location { get; set; } = 20;
    public double CompanySize { get; set; } = 10;
    public double ProductRelevance { get; set; } = 15;
    public double ContactCompleteness { get; set; } = 20;
    public double IntentEvidence { get; set; } = 10;
    public int HotThreshold { get; set; } = 75;
    public int WarmThreshold { get; set; } = 50;
    public int ColdThreshold { get; set; } = 25;
}

/// <summary>Settings stored as one JSON document in the Settings table. Never contains secrets.</summary>
public sealed class AppSettings
{
    public bool SetupCompleted { get; set; }
    public string CompanyName { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string DefaultRegion { get; set; } = "IN";
    public string Theme { get; set; } = "Light";
    public string? ContactEmail { get; set; }
    public ScoringWeights Scoring { get; set; } = new();
    public int RetentionDays { get; set; }
    public string AiProvider { get; set; } = "None";
    public string? AiEndpoint { get; set; }
    public string? AiModel { get; set; }
    public bool VisitWebsitesDuringDiscovery { get; set; } = true;
    public List<string> EnabledProviders { get; set; } = new() { "osm-overpass" };
    public Dictionary<string, string> ProviderEndpoints { get; set; } = new();
    public DateTime? TrialStartedUtc { get; set; }
    public int MaxRequestsPerMinute { get; set; } = 30;
}
