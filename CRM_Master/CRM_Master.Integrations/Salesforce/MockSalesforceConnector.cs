using CRM_Master.Integrations.Interfaces;

namespace CRM_Master.Integrations.Salesforce;

/// <summary>Development-only mock for Salesforce. Does not connect to Salesforce.</summary>
public sealed class MockSalesforceConnector : MockConnectorBase
{
    public MockSalesforceConnector() : base("salesforce", "Salesforce") { }
}
