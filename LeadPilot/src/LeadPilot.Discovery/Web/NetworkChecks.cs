using System.Net.Http.Json;
using System.Text.Json;
using DnsClient;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Text;

namespace LeadPilot.Discovery.Web;

/// <summary>Checks that a website answers (GET, headers only). Refuses private addresses.</summary>
public sealed class HttpWebsiteProbe : IWebsiteProbe
{
    private readonly HttpClient _http;
    private readonly Func<string, CancellationToken, Task<bool>> _hostAllowed;
    public HttpWebsiteProbe(HttpClient http, Func<string, CancellationToken, Task<bool>>? hostAllowed = null) { _http = http; _hostAllowed = hostAllowed ?? UrlGuard.IsPublicHostAsync; }

    public async Task<WebsiteProbeResult> ProbeAsync(string url, CancellationToken ct)
    {
        var n = Normalizer.NormalizeUrl(url);
        if (n == null) return new(false, null, null, false, "invalid address");
        var uri = new Uri(n);
        if (!await _hostAllowed(uri.IdnHost, ct).ConfigureAwait(false)) return new(false, null, null, false, "not a public address");
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct); cts.CancelAfter(TimeSpan.FromSeconds(12));
            using var resp = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            var final = resp.RequestMessage?.RequestUri ?? uri;
            var redirected = !string.Equals(final.AbsoluteUri.TrimEnd('/'), uri.AbsoluteUri.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
            return new((int)resp.StatusCode < 400, (int)resp.StatusCode, final.ToString(), redirected, null);
        }
        catch (HttpRequestException e) { return new(false, null, null, false, e.Message); }
        catch (TaskCanceledException) { return new(false, null, null, false, "timed out"); }
    }
}

/// <summary>DNS A/AAAA and MX lookups via DnsClient. Never opens SMTP connections.</summary>
public sealed class DnsMailResolver : IDnsResolver
{
    private readonly ILookupClient _client;
    public DnsMailResolver(ILookupClient? client = null) { _client = client ?? new LookupClient(new LookupClientOptions { Timeout = TimeSpan.FromSeconds(5), Retries = 1, UseCache = true }); }

    public async Task<DnsCheck> CheckAsync(string domain, CancellationToken ct)
    {
        try
        {
            var mx = await _client.QueryAsync(domain, QueryType.MX, cancellationToken: ct).ConfigureAwait(false);
            var hasMx = mx.Answers.MxRecords().Any(r => !string.IsNullOrWhiteSpace(r.Exchange.Value) && r.Exchange.Value != ".");
            if (hasMx) return new(true, true, null);
            var a = await _client.QueryAsync(domain, QueryType.A, cancellationToken: ct).ConfigureAwait(false);
            var exists = a.Answers.ARecords().Any() || mx.HasError == false && mx.Header.ResponseCode == DnsHeaderResponseCode.NoError;
            return new(exists, false, null);
        }
        catch (DnsResponseException e) when (e.Code == DnsResponseCode.NotExistentDomain) { return new(false, false, null); }
        catch (Exception e) when (e is DnsResponseException or OperationCanceledException or System.Net.Sockets.SocketException) { return new(false, false, e.Message); }
    }
}

/// <summary>Finds official website candidates through the Brave Search API using the customer's own key (free tier available).</summary>
public sealed class BraveWebsiteFinder : IWebsiteFinder
{
    public const string SecretName = Application.Abstractions.SecretNames.BraveSearch;
    private readonly HttpClient _http;
    private readonly ISecretStore _secrets;
    public BraveWebsiteFinder(HttpClient http, ISecretStore secrets) { _http = http; _secrets = secrets; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_secrets.Get(SecretName));

    public async Task<IReadOnlyList<WebsiteCandidate>> FindAsync(string companyName, string? location, CancellationToken ct)
    {
        var key = _secrets.Get(SecretName);
        if (string.IsNullOrWhiteSpace(key)) return Array.Empty<WebsiteCandidate>();
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.search.brave.com/res/v1/web/search?count=8&q={Uri.EscapeDataString($"{companyName} {location} official website")}");
        req.Headers.Add("X-Subscription-Token", key);
        req.Headers.Add("Accept", "application/json");
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"HTTP {(int)resp.StatusCode}", null, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        if (!doc.RootElement.TryGetProperty("web", out var web) || !web.TryGetProperty("results", out var results)) return Array.Empty<WebsiteCandidate>();
        return results.EnumerateArray().Select(r => new WebsiteCandidate(r.GetProperty("url").GetString() ?? "", r.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "", "Brave Search"))
            .Where(c => Normalizer.NormalizeUrl(c.Url) != null).ToList();
    }
}

/// <summary>Creates the shared HttpClient: honest User-Agent, bounded redirects, gzip.</summary>
public static class HttpClients
{
    public static HttpClient Create(string? contactEmail = null, HttpMessageHandler? handler = null)
    {
        var h = handler ?? new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 5, AutomaticDecompression = System.Net.DecompressionMethods.All };
        var c = new HttpClient(h, handler == null) { Timeout = TimeSpan.FromSeconds(60) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd($"{WebsiteExtractor.BotToken}/1.0 (+https://leadpilot.ai/bot{(string.IsNullOrWhiteSpace(contactEmail) ? "" : "; " + contactEmail)})");
        c.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/json;q=0.9,*/*;q=0.5");
        return c;
    }
}
