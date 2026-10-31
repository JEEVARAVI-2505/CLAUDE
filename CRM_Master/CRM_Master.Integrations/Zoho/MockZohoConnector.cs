using CRM_Master.Integrations.Interfaces;

namespace CRM_Master.Integrations.Zoho;

/// <summary>Development-only mock for Zoho CRM. Does not connect to Zoho CRM.</summary>
public sealed class MockZohoConnector : MockConnectorBase
{
    public MockZohoConnector() : base("zoho", "Zoho CRM") { }
}
