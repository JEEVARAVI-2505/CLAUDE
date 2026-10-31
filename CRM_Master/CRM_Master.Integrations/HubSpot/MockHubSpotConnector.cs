using CRM_Master.Integrations.Interfaces;

namespace CRM_Master.Integrations.HubSpot;

/// <summary>Development-only mock for HubSpot. Does not connect to HubSpot.</summary>
public sealed class MockHubSpotConnector : MockConnectorBase
{
    public MockHubSpotConnector() : base("hubspot", "HubSpot") { }
}
