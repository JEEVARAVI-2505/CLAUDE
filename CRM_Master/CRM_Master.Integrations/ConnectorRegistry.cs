using CRM_Master.Integrations.HubSpot;
using CRM_Master.Integrations.Interfaces;
using CRM_Master.Integrations.Pipedrive;
using CRM_Master.Integrations.Salesforce;
using CRM_Master.Integrations.Zoho;

namespace CRM_Master.Integrations;

/// <summary>Built-in connectors. Version 1.0 ships mock adapters only (see ConnectorMaturity).</summary>
public sealed class ConnectorRegistry : IConnectorRegistry
{
    public IReadOnlyList<ICrmConnector> All { get; } = new ICrmConnector[] { new MockHubSpotConnector(), new MockZohoConnector(), new MockSalesforceConnector(), new MockPipedriveConnector() };
    public ICrmConnector? Find(string id) => All.FirstOrDefault(c => c.Info.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
