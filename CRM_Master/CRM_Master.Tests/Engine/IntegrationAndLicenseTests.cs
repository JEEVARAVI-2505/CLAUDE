using CRM_Master.Core.Interfaces;
using CRM_Master.Engine.Licensing;
using CRM_Master.Integrations;
using CRM_Master.Integrations.Interfaces;
using CRM_Master.Integrations.Security;
using CRM_Master.Tests.Support;

namespace CRM_Master.Tests.Engine;

public class IntegrationTests
{
    [Fact]
    public void Registry_lists_the_four_vendors_all_honestly_labelled_as_development_mocks()
    {
        var reg = new ConnectorRegistry();
        reg.All.Select(c => c.Info.Id).Should().BeEquivalentTo("hubspot", "zoho", "salesforce", "pipedrive");
        reg.All.Should().OnlyContain(c => c.Info.IsMock && c.Info.Maturity == ConnectorMaturity.DevelopmentMock);
        reg.All.Should().OnlyContain(c => c.Info.Description.Contains("does NOT connect"));
        reg.Find("HubSpot").Should().NotBeNull(); reg.Find("nope").Should().BeNull();
    }

    [Fact]
    public async Task Mock_connector_works_without_network_and_says_it_is_a_mock()
    {
        var c = new ConnectorRegistry().Find("zoho")!;
        (await c.TestConnectionAsync()).Message.Should().Contain("development mock").And.Contain("no connection to Zoho CRM");
        var pull = await c.PullContactsAsync(null, null);
        pull.Contacts.Should().HaveCount(3).And.OnlyContain(x => x.Email!.EndsWith("@example.invalid"));
        (await c.PullContactsAsync(new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc), null)).Contacts.Should().HaveCount(1);
        (await c.PushContactsAsync(pull.Contacts)).Created.Should().Be(3);
    }
}

public class CredentialStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "crm-master-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    [Theory]
    [InlineData("../evil")]
    [InlineData("a/b")]
    [InlineData("")]
    [InlineData("name with space")]
    public async Task Path_traversal_and_bad_keys_are_rejected(string key)
    {
        var s = new DpapiCredentialStore(_dir);
        await ((Func<Task>)(() => s.SaveAsync(key, "secret"))).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Secrets_are_never_written_in_plain_text()
    {
        var s = new DpapiCredentialStore(_dir);
        if (!s.IsAvailable)
        {
            // Linux/macOS: refuse rather than fall back to plain text
            await ((Func<Task>)(() => s.SaveAsync("hubspot.token", "TOP-SECRET-TOKEN"))).Should().ThrowAsync<PlatformNotSupportedException>();
            Directory.Exists(_dir).Should().BeFalse();
            (await s.LoadAsync("hubspot.token")).Should().BeNull();
            return;
        }
        await s.SaveAsync("hubspot.token", "TOP-SECRET-TOKEN");
        var raw = await File.ReadAllBytesAsync(Path.Combine(_dir, "hubspot.token.bin"));
        System.Text.Encoding.UTF8.GetString(raw).Should().NotContain("TOP-SECRET");
        (await s.LoadAsync("hubspot.token")).Should().Be("TOP-SECRET-TOKEN");
        await s.DeleteAsync("hubspot.token");
        (await s.LoadAsync("hubspot.token")).Should().BeNull();
    }
}

public class LicenseTests
{
    private static async Task<(EvaluationLicenseProvider p, TestDb db)> Make(DateTime? firstRun)
    {
        var db = await TestDb.CreateAsync();
        var s = db.Settings.Current.Clone(); s.FirstRunUtc = firstRun; await db.Settings.SaveAsync(s);
        return (new EvaluationLicenseProvider(db.Settings), db);
    }

    [Fact]
    public async Task Evaluation_counts_down_from_first_launch_and_never_claims_to_be_licensed()
    {
        var now = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var (p, db) = await Make(now.AddDays(-10)); using var _ = db;
        var st = p.GetStatus(now);
        (st.Mode, st.DaysRemaining, st.IsExpired).Should().Be((LicenseMode.Evaluation, 20, false));
        st.Message.Should().Contain("20 day(s)");
    }

    [Fact]
    public async Task Expired_evaluation_is_informational_and_keeps_data_accessible()
    {
        var now = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var (p, db) = await Make(now.AddDays(-45)); using var _ = db;
        var st = p.GetStatus(now);
        (st.IsExpired, st.DaysRemaining).Should().Be((true, 0));
        st.Message.Should().Contain("remains fully accessible");
    }

    [Fact]
    public async Task Before_first_run_the_full_period_is_available()
    {
        var (p, db) = await Make(null); using var _ = db;
        p.GetStatus(DateTime.UtcNow).DaysRemaining.Should().Be(30);
    }
}
