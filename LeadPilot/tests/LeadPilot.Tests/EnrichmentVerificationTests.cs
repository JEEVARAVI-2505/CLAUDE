using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Domain;
using LeadPilot.Tests.Support;

namespace LeadPilot.Tests;

public class EnrichmentVerificationTests
{
    private sealed class Extractor : IWebsiteExtractor
    {
        public WebsiteExtraction Result { get; set; } = new();
        public string? LastUrl { get; private set; }
        public Task<WebsiteExtraction> ExtractAsync(string url, CancellationToken ct) { LastUrl = url; return Task.FromResult(Result); }
    }
    private sealed class Finder : IWebsiteFinder
    {
        public bool IsConfigured { get; set; }
        public List<WebsiteCandidate> Results { get; } = new();
        public Task<IReadOnlyList<WebsiteCandidate>> FindAsync(string n, string? l, CancellationToken ct) => Task.FromResult<IReadOnlyList<WebsiteCandidate>>(Results);
    }
    private sealed class Ai : IAiClient
    {
        public bool IsAvailable => true; public string Name => "fake";
        public Task<string?> CompleteAsync(string s, string p, CancellationToken ct) => Task.FromResult<string?>("A precision sheet-metal fabricator in Chennai.");
    }
    private sealed class NoAi : IAiClient { public bool IsAvailable => false; public string Name => "off"; public Task<string?> CompleteAsync(string s, string p, CancellationToken ct) => Task.FromResult<string?>(null); }

    private static WebsiteExtraction Good() => new()
    {
        Url = "https://acme-fab.in", FinalUrl = "https://www.acme-fab.in/", Success = true, CompanyName = "Acme Fabricators Pvt Ltd", Description = "Precision sheet-metal fabrication.",
        Address = "12 Mount Road, Chennai", ContactPageUrl = "https://acme-fab.in/contact", AboutText = "We fabricate precision parts."
    };

    private static (EnrichmentService svc, TestEnv env, Extractor ex, Finder finder) Make(IAiClient? ai = null)
    {
        var env = new TestEnv(); var ex = new Extractor(); var f = new Finder();
        return (new EnrichmentService(env.Leads, ex, f, ai ?? new NoAi(), env.Scoring, env.Settings, env.Work, env.Clock), env, ex, f);
    }

    [Fact]
    public async Task Fills_only_missing_fields_from_the_company_site_with_evidence()
    {
        var (svc, env, ex, _) = Make(); using var _e = env;
        var id = env.Add("Acme Fabricators", c => { c.Website = "https://acme-fab.in"; c.Phone = "+91 98765 43210"; });
        ex.Result = Good();
        ex.Result.Emails.AddRange(new[] { "sales@acme-fab.in", "owner@gmail.com" });
        ex.Result.Phones.AddRange(new[] { "044 2345 6789", "12345" });
        ex.Result.ValueSources["sales@acme-fab.in"] = "https://acme-fab.in/contact";

        var r = await svc.EnrichAsync(id);
        r.Reached.Should().BeTrue();
        r.Applied.Should().Contain(new[] { "Email", "Address", "Description", "Contact page" });
        r.Applied.Should().NotContain("Phone", "a phone already existed and must never be overwritten");
        var d = env.Leads.GetDetail(id)!;
        d.Business.Phone.Should().Be("+91 98765 43210");
        d.Business.Email.Should().Be("sales@acme-fab.in");
        d.Business.Address.Should().Be("12 Mount Road, Chennai");
        d.Lead.EmailStatus.Should().Be(EmailStatus.ValidFormat, "found on a page is not 'verified'");
        d.Lead.WebsiteStatus.Should().Be(WebsiteStatus.Accessible);
        var emailEv = d.Evidence.Single(e => e.FieldName == "Email" && e.Applied);
        emailEv.SourceUrl.Should().Be("https://acme-fab.in/contact"); emailEv.ExtractedAt.Should().Be(env.Clock.UtcNow);
        d.Evidence.Should().Contain(e => e.FieldName == "Email" && e.Value == "owner@gmail.com" && !e.Applied, "third-party mailbox is a suggestion only");
        d.Evidence.Should().Contain(e => e.FieldName == "Phone" && !e.Applied && e.Value == "044 2345 6789");
        d.Evidence.Should().NotContain(e => e.Value == "12345");
        d.Activities.Should().Contain(a => a.Type == ActivityType.Enriched);
    }

    [Fact]
    public async Task Name_mismatch_applies_nothing_and_flags_the_suggestions()
    {
        var (svc, env, ex, _) = Make(); using var _e = env;
        var id = env.Add("Acme Fabricators", c => c.Website = "https://acme-fab.in");
        ex.Result = Good(); ex.Result.CompanyName = "Zenith Bakery"; ex.Result.Emails.Add("hello@acme-fab.in");
        var r = await svc.EnrichAsync(id);
        r.Applied.Should().BeEmpty(); r.Warnings.Should().ContainSingle(w => w.Contains("does not match"));
        var d = env.Leads.GetDetail(id)!;
        d.Business.Email.Should().BeNull();
        d.Evidence.Should().Contain(e => e.Value == "hello@acme-fab.in" && !e.Applied && e.Confidence < 0.5);
    }

    [Fact]
    public async Task Redirect_to_a_different_domain_applies_nothing()
    {
        var (svc, env, ex, _) = Make(); using var _e = env;
        var id = env.Add("Acme Fabricators", c => c.Website = "https://acme-fab.in");
        ex.Result = Good(); ex.Result.FinalUrl = "https://parked-domains.example/acme"; ex.Result.Emails.Add("x@parked-domains.example");
        var r = await svc.EnrichAsync(id);
        r.Applied.Should().BeEmpty();
        env.Leads.GetLead(id)!.WebsiteStatus.Should().Be(WebsiteStatus.DomainMismatch);
    }

    [Fact]
    public async Task Robots_block_and_unreachable_sites_are_reported_honestly()
    {
        var (svc, env, ex, _) = Make(); using var _e = env;
        var id = env.Add("Acme Fabricators", c => c.Website = "https://acme-fab.in");
        ex.Result = new WebsiteExtraction { Url = "https://acme-fab.in", BlockedByRobots = true, Error = "robots" };
        (await svc.EnrichAsync(id)).Warnings.Should().ContainSingle(w => w.Contains("robots.txt"));
        env.Leads.GetLead(id)!.WebsiteStatus.Should().Be(WebsiteStatus.Unverified);
        ex.Result = new WebsiteExtraction { Url = "https://acme-fab.in", Error = "timed out" };
        (await svc.EnrichAsync(id)).Warnings.Should().ContainSingle(w => w.Contains("timed out"));
        env.Leads.GetLead(id)!.WebsiteStatus.Should().Be(WebsiteStatus.Unavailable);
    }

    [Fact]
    public async Task Without_a_website_and_without_search_key_nothing_is_invented()
    {
        var (svc, env, ex, _) = Make(); using var _e = env;
        var id = env.Add("Acme Fabricators");
        var r = await svc.EnrichAsync(id);
        r.Applied.Should().BeEmpty(); r.Warnings.Single().Should().Contain("No website on record");
        ex.LastUrl.Should().BeNull();
    }

    [Fact]
    public async Task Website_finder_result_is_applied_only_when_it_clearly_matches_and_is_not_a_directory()
    {
        var (svc, env, ex, finder) = Make(); using var _e = env;
        finder.IsConfigured = true;
        finder.Results.AddRange(new[] { new WebsiteCandidate("https://www.justdial.com/Chennai/Acme-Fabricators", "Acme Fabricators - Justdial", "Brave"), new WebsiteCandidate("https://acme-fab.in", "Acme Fabricators – Home", "Brave") });
        var id = env.Add("Acme Fabricators");
        ex.Result = Good();
        var r = await svc.EnrichAsync(id);
        r.Applied.Should().Contain("Website");
        env.Leads.GetDetail(id)!.Business.Website.Should().Be("https://acme-fab.in");
        ex.LastUrl.Should().Be("https://acme-fab.in");

        finder.Results.Clear(); finder.Results.Add(new WebsiteCandidate("https://totally-unrelated.com", "Cheap Flights", "Brave"));
        var id2 = env.Add("Quantum Zebra Works");
        (await svc.EnrichAsync(id2)).Warnings.Single().Should().Contain("No search result clearly matched");
        env.Leads.GetDetail(id2)!.Business.Website.Should().BeNull();
    }

    [Fact]
    public async Task Ai_summary_is_stored_as_an_unapplied_suggestion_and_never_overwrites()
    {
        var (svc, env, ex, _) = Make(new Ai()); using var _e = env;
        var id = env.Add("Acme Fabricators", c => c.Website = "https://acme-fab.in");
        ex.Result = Good();
        var r = await svc.EnrichAsync(id);
        r.Suggestions.Should().Contain(s => s.StartsWith("AI summary"));
        var d = env.Leads.GetDetail(id)!;
        d.Evidence.Single(e => e.FieldName == "AI summary").Applied.Should().BeFalse();
        d.Business.Description.Should().Be("Precision sheet-metal fabrication.");
    }

    // ---- verification ----
    private sealed class Dns : IDnsResolver { public DnsCheck R = new(true, true, null); public Task<DnsCheck> CheckAsync(string d, CancellationToken ct) => Task.FromResult(R); }
    private sealed class Probe : IWebsiteProbe { public WebsiteProbeResult R = new(true, 200, "https://acme-fab.in/", false, null); public Task<WebsiteProbeResult> ProbeAsync(string u, CancellationToken ct) => Task.FromResult(R); }

    private static (VerificationService v, TestEnv env, Dns dns, Probe probe) MakeV()
    {
        var env = new TestEnv(); var dns = new Dns(); var probe = new Probe();
        return (new VerificationService(env.Leads, env.Work, dns, probe, env.Settings), env, dns, probe);
    }

    [Fact]
    public async Task Email_status_ladder_never_claims_the_mailbox_exists()
    {
        var (v, env, dns, _) = MakeV(); using var _e = env;
        (await v.CheckEmailAsync("bad", EmailStatus.Unverified, true, default)).Item1.Should().Be(EmailStatus.InvalidFormat);
        (await v.CheckEmailAsync("a@b.com", EmailStatus.Unverified, false, default)).Item1.Should().Be(EmailStatus.ValidFormat);
        (await v.CheckEmailAsync("a@b.com", EmailStatus.Unverified, true, default)).Should().Match<(EmailStatus, string)>(x => x.Item1 == EmailStatus.MailServerFound && x.Item2.Contains("does not prove"));
        dns.R = new DnsCheck(true, false, null);
        (await v.CheckEmailAsync("a@b.com", EmailStatus.Unverified, true, default)).Item1.Should().Be(EmailStatus.DomainExists);
        dns.R = new DnsCheck(false, false, null);
        (await v.CheckEmailAsync("a@b.com", EmailStatus.Unverified, true, default)).Item1.Should().Be(EmailStatus.ValidFormat);
        dns.R = new DnsCheck(false, false, "timeout");
        (await v.CheckEmailAsync("a@b.com", EmailStatus.Unverified, true, default)).Item2.Should().Contain("unavailable");
        (await v.CheckEmailAsync("a@b.com", EmailStatus.VerifiedByProvider, true, default)).Item1.Should().Be(EmailStatus.VerifiedByProvider, "provider verification is never downgraded");
        (await v.CheckEmailAsync("", EmailStatus.Unverified, true, default)).Item1.Should().Be(EmailStatus.Unverified);
    }

    [Fact]
    public void Phone_status_makes_no_ownership_claim()
    {
        var (v, env, _, _) = MakeV(); using var _e = env;
        v.CheckPhone("12345", PhoneStatus.Unverified, false).Item1.Should().Be(PhoneStatus.InvalidFormat);
        var (s, msg) = v.CheckPhone("+91 98765 43210", PhoneStatus.Unverified, false);
        s.Should().Be(PhoneStatus.CountryIdentified); msg.Should().Contain("Ownership is not verified");
        v.CheckPhone("+91 98765 43210", PhoneStatus.Unverified, true).Item1.Should().Be(PhoneStatus.PubliclyListed);
        v.CheckPhone("+91 98765 43210", PhoneStatus.VerifiedByProvider, false).Item1.Should().Be(PhoneStatus.VerifiedByProvider);
    }

    [Fact]
    public async Task Website_status_covers_accessible_redirected_mismatch_unavailable()
    {
        var (v, env, _, probe) = MakeV(); using var _e = env;
        (await v.CheckWebsiteAsync("acme-fab.in", default)).Item1.Should().Be(WebsiteStatus.Accessible);
        probe.R = new(true, 200, "https://www.acme-fab.in/home", true, null);
        (await v.CheckWebsiteAsync("acme-fab.in", default)).Item1.Should().Be(WebsiteStatus.Redirected);
        probe.R = new(true, 200, "https://other-company.com/", true, null);
        (await v.CheckWebsiteAsync("acme-fab.in", default)).Item1.Should().Be(WebsiteStatus.DomainMismatch);
        probe.R = new(false, 503, null, false, null);
        (await v.CheckWebsiteAsync("acme-fab.in", default)).Item1.Should().Be(WebsiteStatus.Unavailable);
        (await v.CheckWebsiteAsync(null, default)).Item1.Should().Be(WebsiteStatus.Unverified);
    }

    [Fact]
    public async Task Verify_lead_persists_statuses_and_timeline()
    {
        var (v, env, _, _) = MakeV(); using var _e = env;
        var id = env.Add("Acme", c => { c.Email = "info@acme-fab.in"; c.Phone = "+91 98765 43210"; c.Website = "acme-fab.in"; });
        var res = await v.VerifyLeadAsync(id, true);
        res!.Email.Should().Be(EmailStatus.MailServerFound);
        var lead = env.Leads.GetLead(id)!;
        (lead.EmailStatus, lead.PhoneStatus, lead.WebsiteStatus).Should().Be((EmailStatus.MailServerFound, PhoneStatus.CountryIdentified, WebsiteStatus.Accessible));
        env.Leads.GetDetail(id)!.Activities.Should().Contain(a => a.Subject == "Contact checks run");
    }
}
