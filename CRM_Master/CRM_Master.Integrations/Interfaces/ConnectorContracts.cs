namespace CRM_Master.Integrations.Interfaces;

/// <summary>A contact as an external CRM represents it (mapped to/from the local Contact by the sync layer, when it exists).</summary>
public sealed record RemoteContact(
    string ExternalId, string? FirstName, string? LastName, string? Email, string? Phone, string? Company,
    string? JobTitle, DateTime? ModifiedUtc);

/// <summary>Describes a connector to the UI. <see cref="IsMock"/> adapters never touch a real service.</summary>
public sealed record ConnectorInfo(string Id, string DisplayName, bool IsMock, ConnectorMaturity Maturity, string Description);

/// <summary>How real a connector is - the UI must show this honestly.</summary>
public enum ConnectorMaturity { Planned, DevelopmentMock, Beta, Production }

public sealed record ConnectionTestResult(bool Success, string Message);
public sealed record PullResult(IReadOnlyList<RemoteContact> Contacts, string? NextCursor);
public sealed record PushResult(int Created, int Updated, int Failed, IReadOnlyList<string> Errors);

/// <summary>
/// Contract every CRM connector (HubSpot, Zoho, Salesforce, Pipedrive ...) will implement. Real connectors will use the vendor's official
/// OAuth APIs and require internet access; the local application never depends on them being available.
/// </summary>
public interface ICrmConnector
{
    ConnectorInfo Info { get; }
    Task<ConnectionTestResult> TestConnectionAsync(CancellationToken ct = default);
    Task<PullResult> PullContactsAsync(DateTime? modifiedSinceUtc, string? cursor, CancellationToken ct = default);
    Task<PushResult> PushContactsAsync(IReadOnlyList<RemoteContact> contacts, CancellationToken ct = default);
}

/// <summary>Lists the connectors compiled into this build.</summary>
public interface IConnectorRegistry
{
    IReadOnlyList<ICrmConnector> All { get; }
    ICrmConnector? Find(string id);
}

/// <summary>Stores secrets (OAuth tokens, API keys). Implementations must never write them in plain text.</summary>
public interface ICredentialStore
{
    /// <summary>False when the platform offers no safe storage; Save then throws instead of falling back to plain text.</summary>
    bool IsAvailable { get; }
    Task SaveAsync(string key, string secret, CancellationToken ct = default);
    Task<string?> LoadAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
