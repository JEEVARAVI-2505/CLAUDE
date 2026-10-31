namespace CRM_Master.Integrations.Interfaces;

/// <summary>
/// DEVELOPMENT-ONLY connector used to exercise the integration plumbing. It never contacts the vendor, never sends or receives
/// real customer data, and always reports <see cref="ConnectorMaturity.DevelopmentMock"/>.
/// </summary>
public abstract class MockConnectorBase : ICrmConnector
{
    private readonly List<RemoteContact> _sample;
    private readonly List<RemoteContact> _pushed = new();

    protected MockConnectorBase(string id, string vendor)
    {
        Info = new ConnectorInfo(id, vendor, true, ConnectorMaturity.DevelopmentMock,
            $"Development mock for {vendor}. It does NOT connect to {vendor} and does not synchronise real data. A genuine OAuth connector is planned.");
        _sample = Enumerable.Range(1, 3).Select(i => new RemoteContact($"mock-{id}-{i}", $"Sample{i}", "Contact", $"sample{i}@example.invalid", null, "Example Co", null, new DateTime(2025, 1, i, 0, 0, 0, DateTimeKind.Utc))).ToList();
    }

    public ConnectorInfo Info { get; }
    public IReadOnlyList<RemoteContact> PushedContacts => _pushed;

    public Task<ConnectionTestResult> TestConnectionAsync(CancellationToken ct = default) =>
        Task.FromResult(new ConnectionTestResult(true, $"Mock adapter reachable. This is a development mock - no connection to {Info.DisplayName} was made."));

    public Task<PullResult> PullContactsAsync(DateTime? modifiedSinceUtc, string? cursor, CancellationToken ct = default) =>
        Task.FromResult(new PullResult(_sample.Where(c => modifiedSinceUtc is null || c.ModifiedUtc > modifiedSinceUtc).ToList(), null));

    public Task<PushResult> PushContactsAsync(IReadOnlyList<RemoteContact> contacts, CancellationToken ct = default)
    {
        _pushed.AddRange(contacts);   // kept in memory only
        return Task.FromResult(new PushResult(contacts.Count, 0, 0, Array.Empty<string>()));
    }
}
