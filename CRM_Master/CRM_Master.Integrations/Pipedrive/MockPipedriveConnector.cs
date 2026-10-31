using CRM_Master.Integrations.Interfaces;

namespace CRM_Master.Integrations.Pipedrive;

/// <summary>Development-only mock for Pipedrive. Does not connect to Pipedrive.</summary>
public sealed class MockPipedriveConnector : MockConnectorBase
{
    public MockPipedriveConnector() : base("pipedrive", "Pipedrive") { }
}
