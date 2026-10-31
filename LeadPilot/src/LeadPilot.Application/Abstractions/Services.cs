using LeadPilot.Application.Models;

namespace LeadPilot.Application.Abstractions;

/// <summary>Clock abstraction for deterministic tests.</summary>
public interface IClock { DateTime UtcNow { get; } }

/// <summary>System clock.</summary>
public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }

/// <summary>A source adapter (directory, API, …). Implementations must not bypass CAPTCHAs, logins or terms of service.</summary>
public interface IDiscoveryProvider
{
    string Id { get; }
    string DisplayName { get; }
    string SourceType { get; }
    string Description { get; }
    bool RequiresApiKey { get; }
    /// <summary>Plain-language note on cost and limits shown to the customer.</summary>
    string CostNote { get; }
    /// <summary>Licence / attribution requirements for data from this source.</summary>
    string LicenseNote { get; }
    /// <summary>Minimum delay between two requests to this provider.</summary>
    TimeSpan MinInterval { get; }
    Task<ProviderAvailability> CheckAvailabilityAsync(ProviderSettings settings, CancellationToken ct);
    Task<ProviderPage> SearchPageAsync(DiscoveryRequest request, string? pageToken, ProviderSettings settings, CancellationToken ct);
    Task<ProspectCandidate?> GetDetailsAsync(string externalId, ProviderSettings settings, CancellationToken ct);
}

/// <summary>Fetches and parses a public company website (respecting robots.txt).</summary>
public interface IWebsiteExtractor { Task<WebsiteExtraction> ExtractAsync(string url, CancellationToken ct); }

/// <summary>Checks whether a URL is reachable.</summary>
public interface IWebsiteProbe { Task<WebsiteProbeResult> ProbeAsync(string url, CancellationToken ct); }

/// <summary>Checks that a domain exists and publishes mail servers.</summary>
public interface IDnsResolver { Task<DnsCheck> CheckAsync(string domain, CancellationToken ct); }

/// <summary>Finds candidate official websites for a company (optional, needs a customer-supplied search key).</summary>
public interface IWebsiteFinder
{
    bool IsConfigured { get; }
    Task<IReadOnlyList<WebsiteCandidate>> FindAsync(string companyName, string? location, CancellationToken ct);
}

/// <summary>Optional language-model access. All features must work when <see cref="IsAvailable"/> is false.</summary>
public interface IAiClient
{
    bool IsAvailable { get; }
    string Name { get; }
    Task<string?> CompleteAsync(string system, string prompt, CancellationToken ct);
}

/// <summary>Stores API keys encrypted at rest.</summary>
public interface ISecretStore
{
    string? Get(string name);
    void Set(string name, string? value);
}

/// <summary>Application settings load/save.</summary>
public interface ISettingsService
{
    AppSettings Current { get; }
    void Save(AppSettings s);
    /// <summary>Re-reads settings from the database (after a restore).</summary>
    void Reload();
    event EventHandler? Changed;
}

/// <summary>Signed-in user and permission checks.</summary>
public interface ICurrentUser
{
    Domain.SalesUser? User { get; }
    int? UserId { get; }
    bool Can(Permission p);
    event EventHandler? Changed;
}

/// <summary>Capabilities gated by role.</summary>
public enum Permission { ViewLeads, EditLeads, DeleteLeads, Export, ManageUsers, ManageSettings, ManageBackups, ManageIntegrations, RunDiscovery }

/// <summary>Licence tier information.</summary>
public sealed record LicenseInfo(string Tier, string Holder, int MaxLeads, int MaxUsers, DateTime? ExpiresUtc, bool IsValid, string Message, bool IsTrial);

/// <summary>Licence validation (offline).</summary>
public interface ILicenseService
{
    LicenseInfo Current { get; }
    LicenseInfo Activate(string licenseKey);
    string MachineId { get; }
}

/// <summary>Desktop-only interactions the view-models need.</summary>
public interface IDialogService
{
    string? PickOpenFile(string title, string filter);
    string? PickSaveFile(string title, string filter, string defaultName);
    string? PickFolder(string title);
    bool Confirm(string title, string message);
    void Info(string title, string message);
    void Error(string title, string message);
    string? PromptPassword(string title, string message, bool confirm);
    string? PromptText(string title, string message, string? defaultValue = null);
    void OpenUrl(string url);
    void CopyToClipboard(string text);
    void SetTheme(string theme);
}

/// <summary>Folders the user may want to see or choose by default.</summary>
public interface IAppFolders
{
    string Root { get; }
    string Backups { get; }
    string Exports { get; }
    string Logs { get; }
}

/// <summary>Names under which secrets are stored.</summary>
public static class SecretNames
{
    public const string BraveSearch = "provider:brave-search";
    public const string AiKey = "ai:key";
    public static string Provider(string id) => "provider:" + id;
}
