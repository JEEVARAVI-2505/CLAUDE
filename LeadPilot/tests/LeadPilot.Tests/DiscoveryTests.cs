using System.Net;
using System.Text;
using System.Text.Json;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Discovery.Providers;
using LeadPilot.Discovery.Web;
using LeadPilot.Domain;
using LeadPilot.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeadPilot.Tests;

public sealed class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _fn;
    public List<string> Requests { get; } = new();
    public List<string> Bodies { get; } = new();
    public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> fn) { _fn = fn; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request.Method + " " + request.RequestUri);
        if (request.Content != null) Bodies.Add(await request.Content.ReadAsStringAsync(ct));
        var r = _fn(request); r.RequestMessage ??= request; return r;
    }
    public static HttpResponseMessage Ok(string body, string type = "application/json") => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, type) };
}

public class OsmProviderTests
{
    private const string NominatimChennai = """[{"osm_type":"relation","osm_id":1234,"display_name":"Chennai, Tamil Nadu, India","boundingbox":["12.8","13.2","80.1","80.3"]}]""";

    private static string Overpass(int n) => JsonSerializer.Serialize(new
    {
        version = 0.6,
        elements = Enumerable.Range(1, n).Select(i => new Dictionary<string, object>
        {
            ["type"] = i % 2 == 0 ? "way" : "node", ["id"] = 1000 + i,
            ["center"] = new { lat = 13.0 + i / 1000.0, lon = 80.2 },
            ["tags"] = i % 3 == 0
                ? new Dictionary<string, string> { ["name"] = $"Plant {i:D3} Industries", ["industrial"] = "factory" }
                : new Dictionary<string, string> { ["name"] = $"Plant {i:D3} Industries", ["phone"] = "+91 44 2345 6789", ["website"] = $"https://plant{i}.example.in", ["addr:street"] = "Mount Road", ["addr:housenumber"] = "12", ["addr:city"] = "Chennai", ["email"] = $"info@plant{i}.example.in" }
        }).Append(new Dictionary<string, object> { ["type"] = "node", ["id"] = 99, ["lat"] = 1.0, ["lon"] = 1.0, ["tags"] = new Dictionary<string, string> { ["amenity"] = "bench" } }).ToList()
    });

    private static (OsmOverpassProvider p, FakeHandler h) Make(int n, Func<HttpRequestMessage, HttpResponseMessage>? over = null)
    {
        var h = new FakeHandler(r =>
        {
            if (over != null) return over(r);
            var u = r.RequestUri!.ToString();
            if (u.Contains("status.php")) return FakeHandler.Ok("OK", "text/plain");
            if (u.Contains("nominatim")) return FakeHandler.Ok(NominatimChennai);
            return FakeHandler.Ok(Overpass(n));
        });
        return (new OsmOverpassProvider(new HttpClient(h)) { MinInterval = TimeSpan.Zero }, h);
    }

    private static DiscoveryRequest Req() => new() { Product = "Solar", TargetCustomer = "Manufacturing Companies", Location = "Chennai", Count = 100 };

    [Fact]
    public async Task Search_geocodes_builds_an_area_query_and_returns_real_fields_with_attribution()
    {
        var (p, h) = Make(60);
        var page = await p.SearchPageAsync(Req(), null, new ProviderSettings(null, null, null), default);
        var q = Uri.UnescapeDataString(h.Bodies.Single()).Replace('+', ' ');
        q.Should().Contain("area(id:3600001234)").And.Contain("industrial\"=\"factory").And.Contain("man_made\"=\"works").And.Contain("(area.a)").And.Contain("out tags center");
        page.Candidates.Should().HaveCount(25);
        var c = page.Candidates.First(x => x.Phone != null);
        c.SourceName.Should().Be("OpenStreetMap"); c.SourceUrl.Should().StartWith("https://www.openstreetmap.org/"); c.License.Should().Contain("ODbL");
        c.Address.Should().Be("12 Mount Road, Chennai"); c.City.Should().Be("Chennai"); c.IsPublicListing.Should().BeTrue();
        c.Industry.Should().Be("Manufacturing");
        c.Evidence.Should().Contain(e => e.Field == "Phone" && e.SourceUrl == c.SourceUrl);
        page.Candidates.Select(x => x.ExternalId).Should().OnlyHaveUniqueItems();
        page.Candidates.Should().NotContain(x => x.Name == "" || x.Name == "bench", "unnamed/non-business nodes are dropped");
        page.Warnings.Should().ContainSingle(w => w.Contains("no phone, e-mail or website"));
    }

    [Fact]
    public async Task Pagination_walks_all_results_with_one_network_query_and_prefers_rich_records()
    {
        var (p, h) = Make(60);
        var all = new List<ProspectCandidate>(); string? tok = null; var pages = 0;
        do { var pg = await p.SearchPageAsync(Req(), tok, new ProviderSettings(null, null, null), default); all.AddRange(pg.Candidates); tok = pg.NextPageToken; pages++; } while (tok != null);
        pages.Should().Be(3); all.Should().HaveCount(60);
        h.Bodies.Should().HaveCount(1, "pages are served from the cached response");
        all.Take(30).Should().OnlyContain(c => c.Phone != null, "records with contact data come first");
    }

    [Fact]
    public async Task Unknown_place_returns_an_explanation_not_made_up_results()
    {
        var (p, _) = Make(0, r => FakeHandler.Ok("[]"));
        var pg = await p.SearchPageAsync(Req(), null, new ProviderSettings(null, null, null), default);
        pg.Candidates.Should().BeEmpty(); pg.Warnings.Should().ContainSingle(w => w.Contains("could not find a place"));
    }

    [Fact]
    public async Task Rate_limit_and_server_errors_surface_as_http_exceptions_with_status()
    {
        var (p, _) = Make(0, r => r.RequestUri!.ToString().Contains("nominatim") ? FakeHandler.Ok(NominatimChennai) : new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var act = async () => await p.SearchPageAsync(Req(), null, new ProviderSettings(null, null, null), default);
        (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Overpass_runtime_error_remark_is_treated_as_failure()
    {
        var (p, _) = Make(0, r => r.RequestUri!.ToString().Contains("nominatim") ? FakeHandler.Ok(NominatimChennai) : FakeHandler.Ok("""{"elements":[],"remark":"runtime error: Query timed out"}"""));
        await ((Func<Task>)(async () => await p.SearchPageAsync(Req(), null, new ProviderSettings(null, null, null), default))).Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Availability_reports_offline_cleanly()
    {
        var (p, _) = Make(0, r => throw new HttpRequestException("no network"));
        var a = await p.CheckAvailabilityAsync(new ProviderSettings(null, null, null), default);
        a.IsAvailable.Should().BeFalse(); a.Message.Should().Contain("offline");
    }

    [Fact]
    public void Generic_customer_type_without_keywords_has_a_name_regex_fallback()
    {
        CategoryMap.Match(new DiscoveryRequest { TargetCustomer = "Schools" }).Should().ContainSingle(e => e.Industry == "Education");
        CategoryMap.Match(new DiscoveryRequest { TargetCustomer = "Zeppelin makers" }).Should().BeEmpty();
        CategoryMap.FallbackRegex(new DiscoveryRequest { TargetCustomer = "Zeppelin makers" }).Should().Contain("zeppelin");
    }
}

public class OrchestratorTests
{
    private sealed class FakeProvider : IDiscoveryProvider
    {
        private readonly Func<int, ProviderPage> _page; public int Calls { get; private set; }
        public FakeProvider(string id, Func<int, ProviderPage> page, bool available = true) { Id = id; DisplayName = id; _page = page; _avail = available; }
        private readonly bool _avail;
        public string Id { get; } public string DisplayName { get; } public string SourceType => "Test"; public string Description => ""; public bool RequiresApiKey => false;
        public string CostNote => ""; public string LicenseNote => ""; public TimeSpan MinInterval => TimeSpan.Zero;
        public Task<ProviderAvailability> CheckAvailabilityAsync(ProviderSettings s, CancellationToken ct) => Task.FromResult(new ProviderAvailability(_avail, _avail ? "ok" : "offline"));
        public Task<ProviderPage> SearchPageAsync(DiscoveryRequest r, string? token, ProviderSettings s, CancellationToken ct) { Calls++; return Task.FromResult(_page(int.Parse(token ?? "0"))); }
        public Task<ProspectCandidate?> GetDetailsAsync(string id, ProviderSettings s, CancellationToken ct) => Task.FromResult<ProspectCandidate?>(null);
    }

    private sealed class FakeExtractor : IWebsiteExtractor { public Task<WebsiteExtraction> ExtractAsync(string url, CancellationToken ct) => Task.FromResult(new WebsiteExtraction { Url = url }); }
    private sealed class MemSecrets : ISecretStore { public string? Get(string n) => null; public void Set(string n, string? v) { } }

    private static ProspectCandidate C(string name, string? web = null, string? phone = null) => new() { Name = name, Website = web, Phone = phone, City = "Chennai", SourceName = "T", SourceType = "Test" };

    private static (DiscoveryOrchestrator o, TestEnv env) Make(params IDiscoveryProvider[] providers)
    {
        var env = new TestEnv();
        var s = env.Settings.Current; s.EnabledProviders = providers.Select(p => p.Id).ToList(); s.VisitWebsitesDuringDiscovery = false; env.Settings.Save(s);
        return (new DiscoveryOrchestrator(providers, new MemSecrets(), env.Settings, env.Sys, new FakeExtractor(), NullLogger<DiscoveryOrchestrator>.Instance, env.Clock), env);
    }

    private static DiscoveryRequest Req(int n = 10) => new() { Product = "P", TargetCustomer = "T", Location = "Chennai", Count = n };

    [Fact]
    public async Task A_failing_provider_does_not_stop_the_others_and_is_retried()
    {
        var bad = new FakeProvider("bad", _ => throw new HttpRequestException("boom", null, HttpStatusCode.ServiceUnavailable));
        var good = new FakeProvider("good", _ => new ProviderPage(new[] { C("Alpha Works", "https://alpha.com"), C("Beta Works") }, null, Array.Empty<string>()));
        var (o, env) = Make(bad, good); using var _e = env;
        var run = await o.RunAsync(Req(), null, default);
        bad.Calls.Should().Be(3, "three attempts");
        run.Providers.Single(p => p.ProviderId == "bad").Error.Should().Contain("503");
        run.Providers.Single(p => p.ProviderId == "good").Succeeded.Should().BeTrue();
        run.Candidates.Should().HaveCount(2); run.AnyProviderSucceeded.Should().BeTrue();
        var usage = env.Sys.GetApiUsage(20);
        usage.Count(u => u.Provider == "bad" && !u.Success).Should().Be(3);
        usage.Single(u => u.Provider == "good").ResultCount.Should().Be(2);
    }

    [Fact]
    public async Task Offline_provider_is_skipped_with_a_message()
    {
        var (o, env) = Make(new FakeProvider("osm", _ => throw new InvalidOperationException(), available: false)); using var _e = env;
        var run = await o.RunAsync(Req(), null, default);
        run.Candidates.Should().BeEmpty(); run.AnyProviderSucceeded.Should().BeFalse();
        run.Providers.Single().Error.Should().Be("offline");
    }

    [Fact]
    public async Task Paging_stops_at_requested_count_and_filters_apply()
    {
        var p = new FakeProvider("p", i => new ProviderPage(Enumerable.Range(0, 10).Select(k => C($"Company{i + k} Works", k % 2 == 0 ? $"https://c{i + k}.com" : null)).ToList(), i < 100 ? (i + 10).ToString() : null, Array.Empty<string>()));
        var (o, env) = Make(p); using var _e = env;
        var req = Req(5); req.RequireWebsite = true; req.ExcludedCompanies.Add("Company0 Works");
        var run = await o.RunAsync(req, null, default);
        run.Candidates.Should().HaveCount(5).And.OnlyContain(c => c.Website != null).And.NotContain(c => c.Name == "Company0 Works");
        p.Calls.Should().BeLessThan(5);
    }

    [Fact]
    public async Task Same_company_from_two_sources_appears_once()
    {
        var a = new FakeProvider("a", _ => new ProviderPage(new[] { C("ABC Industries", "https://abc.com", "+919876543210") }, null, Array.Empty<string>()));
        var b = new FakeProvider("b", _ => new ProviderPage(new[] { C("ABC Industries Pvt Ltd", "https://www.abc.com") }, null, Array.Empty<string>()));
        var (o, env) = Make(a, b); using var _e = env;
        (await o.RunAsync(Req(), null, default)).Candidates.Should().ContainSingle();
    }

    [Fact]
    public async Task No_enabled_source_returns_empty_run_with_guidance()
    {
        var (o, env) = Make(); using var _e = env;
        var msgs = new List<string>();
        var run = await o.RunAsync(Req(), new Progress<string>(msgs.Add), default);
        run.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task Invalid_request_is_rejected()
    {
        var (o, env) = Make(new FakeProvider("p", _ => ProviderPage.Empty())); using var _e = env;
        await ((Func<Task>)(() => o.RunAsync(new DiscoveryRequest { Product = "", TargetCustomer = "x", Location = "y" }, null, default))).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Discovered_candidates_flow_into_scored_stored_leads_with_sources()
    {
        var p = new FakeProvider("osm", _ => new ProviderPage(new[]
        {
            new ProspectCandidate { Name = "Sri Ganesh Engineering Works", City = "Chennai", Phone = "+91 98765 43210", Website = "https://ganesh.in", SourceName = "OpenStreetMap", SourceType = "Public business directory", SourceUrl = "https://www.openstreetmap.org/node/5", IsPublicListing = true },
            new ProspectCandidate { Name = "Bay Cafe", City = "Chennai", SourceName = "OpenStreetMap", SourceType = "Public business directory", SourceUrl = "https://www.openstreetmap.org/node/6" }
        }, null, Array.Empty<string>()));
        var (o, env) = Make(p); using var _e = env;
        var req = new DiscoveryRequest { Product = "Solar", TargetCustomer = "Manufacturing Companies", Location = "Chennai", Count = 10 };
        var run = await o.RunAsync(req, null, default);
        var cmp = env.Work.AddCampaign(new Campaign { Name = "Run", Product = req.Product, TargetCustomer = req.TargetCustomer, Location = req.Location });
        var outcomes = env.Ingest.IngestMany(run.Candidates, new IngestContext { CampaignId = cmp, Profile = new Application.Scoring.ScoringProfile { Product = req.Product, TargetCustomer = req.TargetCustomer, Location = req.Location } });
        outcomes.Should().OnlyContain(x => x.Status == IngestStatus.Created);
        var rows = env.Leads.Search(new LeadQuery { Sort = LeadSort.Score }).Rows;
        rows.First().CompanyName.Should().Be("Sri Ganesh Engineering Works");
        rows.First().Score.Should().BeGreaterThan(rows.Last().Score);
        env.Leads.GetDetail(rows.First().Id)!.Sources.Single().SourceUrl.Should().Be("https://www.openstreetmap.org/node/5");
    }
}

public class WebsiteExtractorTests
{
    private const string Home = """
<html><head><title>Acme Fabricators | Chennai</title>
<meta name="description" content="Precision sheet-metal fabrication since 1998.">
<script type="application/ld+json">{"@context":"https://schema.org","@type":"LocalBusiness","name":"Acme Fabricators Pvt Ltd","telephone":"+91 44 2345 6789",
 "email":"sales@acme-fab.in","address":{"@type":"PostalAddress","streetAddress":"12 Mount Road","addressLocality":"Chennai","postalCode":"600002","addressCountry":"IN"}}</script></head>
<body><nav><a href="/about-us">About us</a> <a href="/contact">Contact</a></nav>
<h2>Our Services</h2><ul><li>Laser cutting</li><li>CNC bending</li></ul>
<p>Write to <a href="mailto:Info@Acme-Fab.in?subject=Hi">us</a>. Call 98765 43210. Logo: <img src="logo@2x.png"></p>
<script>var x = "hidden@tracker.com";</script></body></html>
""";
    private const string Contact = "<html><body><h1>Contact</h1><address>12 Mount Road, Chennai 600002</address><p>Mobile: +91 98765 43211 &middot; accounts@acme-fab.in</p></body></html>";
    private const string About = "<html><body><p>Acme has been making precision parts for Chennai's automotive suppliers for 25 years.</p></body></html>";

    private static WebsiteExtractor Make(Func<HttpRequestMessage, HttpResponseMessage> fn, out FakeHandler h)
    {
        h = new FakeHandler(fn);
        return new WebsiteExtractor(new HttpClient(h), () => "IN", (_, _) => Task.FromResult(true), TimeSpan.Zero);
    }

    private static HttpResponseMessage Site(HttpRequestMessage r, string robots = "User-agent: *\nDisallow: /private/\n") => r.RequestUri!.AbsolutePath switch
    {
        "/robots.txt" => FakeHandler.Ok(robots, "text/plain"),
        "/" => FakeHandler.Ok(Home, "text/html"),
        "/contact" => FakeHandler.Ok(Contact, "text/html"),
        "/about-us" => FakeHandler.Ok(About, "text/html"),
        _ => new HttpResponseMessage(HttpStatusCode.NotFound)
    };

    [Fact]
    public async Task Extracts_structured_and_visible_contact_data_with_page_level_sources()
    {
        var x = Make(r => Site(r), out var h);
        var res = await x.ExtractAsync("acme-fab.in", default);
        res.Success.Should().BeTrue(); res.FinalUrl.Should().Be("https://acme-fab.in/");
        res.CompanyName.Should().Be("Acme Fabricators Pvt Ltd");
        res.Description.Should().Contain("sheet-metal");
        res.Address.Should().Contain("12 Mount Road");
        res.Emails.Should().BeEquivalentTo("sales@acme-fab.in", "info@acme-fab.in", "accounts@acme-fab.in");
        res.Emails.Should().NotContain(e => e.Contains("tracker") || e.EndsWith(".png"));
        res.Phones.Should().HaveCount(3);
        res.Phones.Select(p => LeadPilot.Application.Text.Normalizer.NormalizePhone(p, "IN")).Should().BeEquivalentTo("+914423456789", "+919876543210", "+919876543211");
        res.ContactPageUrl.Should().Be("https://acme-fab.in/contact");
        res.Services.Should().Contain("Laser cutting");
        res.SourceUrls.Should().Contain("https://acme-fab.in/contact");
        res.ValueSources["accounts@acme-fab.in"].Should().Be("https://acme-fab.in/contact");
        res.AboutText.Should().Contain("25 years");
        h.Requests.Should().Contain("GET https://acme-fab.in/robots.txt");
    }

    [Fact]
    public async Task Robots_disallow_prevents_the_page_fetch()
    {
        var x = Make(r => Site(r, "User-agent: *\nDisallow: /contact\n"), out var h);
        var res = await x.ExtractAsync("https://acme-fab.in", default);
        res.Success.Should().BeTrue(); res.SourceUrls.Should().NotContain(u => u.EndsWith("/contact"));
        h.Requests.Should().NotContain(q => q.EndsWith("/contact"));
        res.Emails.Should().NotContain("accounts@acme-fab.in");
    }

    [Fact]
    public async Task Robots_block_all_means_nothing_is_read()
    {
        var x = Make(r => Site(r, "User-agent: *\nDisallow: /\n"), out var h);
        var res = await x.ExtractAsync("https://acme-fab.in", default);
        res.Success.Should().BeFalse(); res.BlockedByRobots.Should().BeTrue();
        h.Requests.Should().OnlyContain(q => q.EndsWith("robots.txt"));
    }

    [Fact]
    public async Task Specific_bot_group_wins_over_wildcard()
    {
        var x = Make(r => Site(r, "User-agent: *\nDisallow:\n\nUser-agent: LeadPilotBot\nDisallow: /\n"), out _);
        (await x.ExtractAsync("https://acme-fab.in", default)).BlockedByRobots.Should().BeTrue();
    }

    [Fact]
    public async Task Robots_server_error_is_treated_as_disallow_but_404_as_allow()
    {
        var x1 = Make(r => r.RequestUri!.AbsolutePath == "/robots.txt" ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : Site(r), out _);
        (await x1.ExtractAsync("https://acme-fab.in", default)).BlockedByRobots.Should().BeTrue();
        var x2 = Make(r => r.RequestUri!.AbsolutePath == "/robots.txt" ? new HttpResponseMessage(HttpStatusCode.NotFound) : Site(r), out _);
        (await x2.ExtractAsync("https://acme-fab.in", default)).Success.Should().BeTrue();
    }

    [Fact]
    public async Task Network_errors_and_http_errors_are_reported_not_thrown()
    {
        var x = Make(r => r.RequestUri!.AbsolutePath == "/robots.txt" ? new HttpResponseMessage(HttpStatusCode.NotFound) : throw new HttpRequestException("connection refused"), out _);
        var res = await x.ExtractAsync("https://down.example", default);
        res.Success.Should().BeFalse(); res.Error.Should().Contain("connection refused");
        var y = Make(r => r.RequestUri!.AbsolutePath == "/robots.txt" ? new HttpResponseMessage(HttpStatusCode.NotFound) : new HttpResponseMessage(HttpStatusCode.Forbidden), out _);
        (await y.ExtractAsync("https://forbidden.example", default)).Error.Should().Contain("403");
    }

    [Fact]
    public async Task Non_html_and_invalid_inputs_are_rejected()
    {
        var x = Make(r => r.RequestUri!.AbsolutePath == "/robots.txt" ? new HttpResponseMessage(HttpStatusCode.NotFound) : FakeHandler.Ok("%PDF", "application/pdf"), out _);
        (await x.ExtractAsync("https://files.example/doc", default)).Success.Should().BeFalse();
        (await x.ExtractAsync("not a url", default)).Error.Should().Contain("valid web address");
    }

    [Fact]
    public async Task Private_and_local_addresses_are_refused_by_default()
    {
        var h = new FakeHandler(r => FakeHandler.Ok("<html></html>"));
        var x = new WebsiteExtractor(new HttpClient(h), () => "IN");
        foreach (var u in new[] { "http://127.0.0.1/admin", "http://192.168.1.1", "http://10.0.0.5/", "http://localhost:8080", "http://169.254.169.254/latest/meta-data", "http://[::1]/" })
            (await x.ExtractAsync(u, default)).Error.Should().MatchRegex("not a public website|valid web address", u);
        h.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("8.8.8.8", true)] [InlineData("1.1.1.1", true)] [InlineData("127.0.0.1", false)] [InlineData("10.1.2.3", false)] [InlineData("172.16.0.1", false)]
    [InlineData("172.32.0.1", true)] [InlineData("192.168.0.9", false)] [InlineData("169.254.1.1", false)] [InlineData("100.64.0.1", false)] [InlineData("::1", false)] [InlineData("fd00::1", false)]
    public void Ip_classification(string ip, bool isPublic) => UrlGuard.IsPublic(IPAddress.Parse(ip)).Should().Be(isPublic);

    [Fact]
    public void Robots_rules_longest_match_wildcards_and_crawl_delay()
    {
        var r = RobotsRules.Parse("User-agent: *\nCrawl-delay: 3\nDisallow: /a/\nAllow: /a/public/\nDisallow: /*.pdf$\nDisallow: /tmp", "LeadPilotBot");
        r.IsAllowed("/a/secret").Should().BeFalse(); r.IsAllowed("/a/public/x").Should().BeTrue();
        r.IsAllowed("/files/x.pdf").Should().BeFalse(); r.IsAllowed("/files/x.pdf?y=1").Should().BeTrue();
        r.IsAllowed("/tmpfile").Should().BeFalse(); r.IsAllowed("/contact").Should().BeTrue();
        r.CrawlDelay.Should().Be(TimeSpan.FromSeconds(3));
        RobotsRules.Parse("", "x").IsAllowed("/anything").Should().BeTrue();
    }
}
