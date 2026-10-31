using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Application.Text;

namespace LeadPilot.Discovery.Web;

/// <summary>
/// Reads publicly available business information from a company's own website: JSON-LD, meta tags, mailto:/tel: links and visible text.
/// Respects robots.txt (and Crawl-delay), rate-limits per host, refuses private addresses, caps page size and time, and never submits forms,
/// logs in or solves challenges. Anything found is "publicly listed", not "verified".
/// </summary>
public sealed partial class WebsiteExtractor : IWebsiteExtractor
{
    public const string BotToken = "LeadPilotBot";
    private const int MaxBytes = 2 * 1024 * 1024;
    private readonly HttpClient _http;
    private readonly RateLimiter _limiter = new();
    private readonly Func<string, CancellationToken, Task<bool>> _hostAllowed;
    private readonly Func<string> _region;
    private readonly Dictionary<string, (DateTime At, RobotsRules Rules)> _robots = new();
    private readonly TimeSpan _minInterval;

    /// <summary>Creates the extractor; <paramref name="hostAllowed"/> overrides the SSRF guard and <paramref name="minInterval"/> the per-host delay (tests).</summary>
    public WebsiteExtractor(HttpClient http, Func<string> defaultRegion, Func<string, CancellationToken, Task<bool>>? hostAllowed = null, TimeSpan? minInterval = null)
    {
        _http = http; _region = defaultRegion; _hostAllowed = hostAllowed ?? UrlGuard.IsPublicHostAsync; _minInterval = minInterval ?? TimeSpan.FromSeconds(1);
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(?:\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}")] private static partial Regex EmailRx();
    [GeneratedRegex(@"(?<![\w/])(?:\+\d{1,3}[\s.\-]?)?(?:\(?\d{2,5}\)?[\s.\-]?){2,4}\d{2,4}(?![\w])")] private static partial Regex PhoneRx();
    [GeneratedRegex(@"contact|reach\s*us|get\s*in\s*touch|enquir|inquir", RegexOptions.IgnoreCase)] private static partial Regex ContactRx();
    [GeneratedRegex(@"\babout\b|who\s*we\s*are|company", RegexOptions.IgnoreCase)] private static partial Regex AboutRx();

    public async Task<WebsiteExtraction> ExtractAsync(string url, CancellationToken ct)
    {
        var norm = Normalizer.NormalizeUrl(url);
        var res = new WebsiteExtraction { Url = norm ?? url, ExtractedAt = DateTime.UtcNow };
        if (norm == null) { res.Error = "Not a valid web address."; return res; }
        var start = new Uri(norm);
        if (!await _hostAllowed(start.IdnHost, ct).ConfigureAwait(false)) { res.Error = "The address is not a public website."; return res; }

        try
        {
            var home = await FetchAsync(start, ct).ConfigureAwait(false);
            if (home.Blocked) { res.BlockedByRobots = true; res.Error = "robots.txt disallows automated access."; return res; }
            res.StatusCode = home.Status;
            if (home.Html == null) { res.Error = home.Error ?? $"HTTP {home.Status}"; return res; }
            res.Success = true; res.FinalUrl = home.FinalUrl;
            var baseUri = new Uri(home.FinalUrl!);
            var homeDoc = Load(home.Html);
            Harvest(homeDoc, baseUri, res);
            res.SourceUrls.Add(home.FinalUrl!);

            var links = homeDoc.DocumentNode.SelectNodes("//a[@href]")?.Select(a => (Text: HtmlEntity.DeEntitize(a.InnerText).Trim(), Href: Resolve(baseUri, a.GetAttributeValue("href", "")))).Where(l => l.Href != null && SameHost(baseUri, l.Href)).ToList() ?? new();
            var contact = links.FirstOrDefault(l => ContactRx().IsMatch(l.Text) || ContactRx().IsMatch(l.Href!.AbsolutePath));
            if (contact.Href != null) res.ContactPageUrl = contact.Href.GetLeftPart(UriPartial.Path);
            var about = links.FirstOrDefault(l => AboutRx().IsMatch(l.Text) && l.Href!.AbsolutePath != contact.Href?.AbsolutePath);

            foreach (var (page, isAbout) in new[] { (contact.Href, false), (about.Href, true) })
            {
                if (page == null || ct.IsCancellationRequested) continue;
                var pg = await FetchAsync(page, ct).ConfigureAwait(false);
                if (pg.Html == null) continue;
                var d = Load(pg.Html);
                Harvest(d, new Uri(pg.FinalUrl!), res);
                res.SourceUrls.Add(pg.FinalUrl!);
                if (isAbout && res.AboutText == null) res.AboutText = Trunc(VisibleText(d), 1500);
            }
            res.AboutText ??= Trunc(res.Description ?? VisibleText(homeDoc), 1500);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException or InvalidDataException)
        {
            res.Success = false;
            res.Error = e is TaskCanceledException ? "The website took too long to respond." : e.Message;
        }
        return res;
    }

    private sealed record Fetched(string? Html, int Status, string? FinalUrl, bool Blocked, string? Error);

    private async Task<Fetched> FetchAsync(Uri uri, CancellationToken ct)
    {
        var rules = await GetRobotsAsync(uri, ct).ConfigureAwait(false);
        if (!rules.IsAllowed(uri.PathAndQuery)) return new(null, 0, null, true, null);
        await _limiter.WaitAsync(uri.Host, rules.CrawlDelay > _minInterval ? rules.CrawlDelay : _minInterval, ct).ConfigureAwait(false);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct); cts.CancelAfter(TimeSpan.FromSeconds(15));
        using var resp = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
        var final = resp.RequestMessage?.RequestUri ?? uri;
        if (!await _hostAllowed(final.IdnHost, ct).ConfigureAwait(false)) return new(null, (int)resp.StatusCode, final.ToString(), false, "Redirected to a non-public address.");
        if (!resp.IsSuccessStatusCode) return new(null, (int)resp.StatusCode, final.ToString(), false, $"HTTP {(int)resp.StatusCode}");
        var type = resp.Content.Headers.ContentType?.MediaType ?? "text/html";
        if (!type.Contains("html", StringComparison.OrdinalIgnoreCase) && !type.Contains("xml", StringComparison.OrdinalIgnoreCase)) return new(null, (int)resp.StatusCode, final.ToString(), false, "Not an HTML page.");
        await using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
        using var ms = new MemoryStream(); var buf = new byte[16384]; int n;
        while ((n = await stream.ReadAsync(buf, cts.Token).ConfigureAwait(false)) > 0) { ms.Write(buf, 0, n); if (ms.Length > MaxBytes) break; }
        var enc = System.Text.Encoding.UTF8;
        try { if (resp.Content.Headers.ContentType?.CharSet is { } cs) enc = System.Text.Encoding.GetEncoding(cs.Trim('"')); } catch (ArgumentException) { /* default UTF-8 */ }
        return new(enc.GetString(ms.ToArray()), (int)resp.StatusCode, final.ToString(), false, null);
    }

    private async Task<RobotsRules> GetRobotsAsync(Uri uri, CancellationToken ct)
    {
        var key = uri.GetLeftPart(UriPartial.Authority);
        if (_robots.TryGetValue(key, out var hit) && DateTime.UtcNow - hit.At < TimeSpan.FromHours(1)) return hit.Rules;
        RobotsRules rules;
        try
        {
            await _limiter.WaitAsync(uri.Host, _minInterval, ct).ConfigureAwait(false);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct); cts.CancelAfter(TimeSpan.FromSeconds(10));
            using var resp = await _http.GetAsync(new Uri(key + "/robots.txt"), cts.Token).ConfigureAwait(false);
            if ((int)resp.StatusCode >= 500) rules = RobotsRules.BlockAll;          // RFC 9309: server errors -> assume disallow
            else if (!resp.IsSuccessStatusCode) rules = RobotsRules.AllowAll;       // 4xx -> no restrictions published
            else
            {
                var text = await resp.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                rules = RobotsRules.Parse(text.Length > 512 * 1024 ? text[..(512 * 1024)] : text, BotToken);
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { rules = RobotsRules.BlockAll; }
        _robots[key] = (DateTime.UtcNow, rules);
        return rules;
    }

    private static HtmlDocument Load(string html) { var d = new HtmlDocument(); d.LoadHtml(html); return d; }

    private static Uri? Resolve(Uri baseUri, string href)
    {
        if (string.IsNullOrWhiteSpace(href) || href.StartsWith('#') || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) return null;
        return Uri.TryCreate(baseUri, href, out var u) && u.Scheme is "http" or "https" ? u : null;
    }

    private static bool SameHost(Uri a, Uri b) => Normalizer.SameSite(a.IdnHost, b.IdnHost);

    private void Harvest(HtmlDocument doc, Uri page, WebsiteExtraction res)
    {
        var src = page.ToString();
        foreach (var script in doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']") ?? new HtmlNodeCollection(null))
            try { using var j = JsonDocument.Parse(HtmlEntity.DeEntitize(script.InnerText)); ReadJsonLd(j.RootElement, res, src); }
            catch (JsonException) { /* malformed structured data is ignored */ }

        string? Meta(string name) => doc.DocumentNode.SelectSingleNode($"//meta[@property='{name}' or @name='{name}']")?.GetAttributeValue("content", null);
        res.CompanyName ??= Clean(Meta("og:site_name")) ?? Clean(Meta("application-name"));
        res.Description ??= Clean(Meta("og:description")) ?? Clean(Meta("description"));
        var title = Clean(doc.DocumentNode.SelectSingleNode("//title")?.InnerText);
        res.CompanyName ??= title == null ? null : Regex.Split(title, @"\s[|\-–—:·]\s").Select(p => p.Trim()).Where(p => p.Length > 1).OrderBy(p => p.Length).FirstOrDefault();

        foreach (var a in doc.DocumentNode.SelectNodes("//a[@href]") ?? new HtmlNodeCollection(null))
        {
            var href = a.GetAttributeValue("href", "").Trim();
            if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) AddEmail(WebUtility.UrlDecode(href[7..].Split('?')[0]), res, src);
            else if (href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) AddPhone(WebUtility.UrlDecode(href[4..]), res, src);
        }
        var text = VisibleText(doc);
        foreach (Match m in EmailRx().Matches(text)) AddEmail(m.Value, res, src);
        foreach (Match m in PhoneRx().Matches(text)) AddPhone(m.Value, res, src);

        res.Address ??= Clean(doc.DocumentNode.SelectSingleNode("//address")?.InnerText) is { Length: > 8 and < 250 } addr ? Regex.Replace(addr, @"\s+", " ") : null;

        foreach (var h in doc.DocumentNode.SelectNodes("//h2|//h3") ?? new HtmlNodeCollection(null))
        {
            if (!Regex.IsMatch(h.InnerText, @"services|products|what we (do|offer)|solutions", RegexOptions.IgnoreCase)) continue;
            var items = h.ParentNode.SelectNodes(".//li|.//h4")?.Select(n => Clean(n.InnerText)).Where(t => t != null && t.Length is > 2 and < 80).Take(12).ToList();
            if (items != null) foreach (var i in items) if (!res.Services.Contains(i!)) res.Services.Add(i!);
        }
    }

    private static string VisibleText(HtmlDocument doc)
    {
        foreach (var n in doc.DocumentNode.SelectNodes("//script|//style|//noscript|//svg")?.ToList() ?? new List<HtmlNode>()) n.Remove();
        return Regex.Replace(HtmlEntity.DeEntitize(doc.DocumentNode.InnerText), @"\s+", " ").Trim();
    }

    private void AddEmail(string? raw, WebsiteExtraction res, string src)
    {
        var e = raw?.Trim().Trim('.', ',', ';', ':', '<', '>', '(', ')');
        if (string.IsNullOrEmpty(e) || !Normalizer.IsValidEmailFormat(e)) return;
        var low = e.ToLowerInvariant();
        if (Regex.IsMatch(low, @"\.(png|jpe?g|gif|svg|webp|css|js)$") || low.Contains("example.", StringComparison.Ordinal) || low.Contains("sentry", StringComparison.Ordinal) || low.StartsWith("your", StringComparison.Ordinal) && low.Contains("@email")) return;
        if (res.Emails.Contains(low)) return;
        res.Emails.Add(low); res.ValueSources[low] = src;
    }

    private void AddPhone(string? raw, WebsiteExtraction res, string src)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        var digits = raw.Count(char.IsDigit);
        if (digits is < 8 or > 15) return;
        var info = Normalizer.ParsePhone(raw, _region());
        if (!info.IsValid || res.Phones.Any(p => Normalizer.NormalizePhone(p, _region()) == info.E164)) return;
        var shown = raw.Trim();
        res.Phones.Add(shown); res.ValueSources[shown] = src;
    }

    private void ReadJsonLd(JsonElement el, WebsiteExtraction res, string src)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Array: foreach (var x in el.EnumerateArray()) ReadJsonLd(x, res, src); return;
            case JsonValueKind.Object: break;
            default: return;
        }
        if (el.TryGetProperty("@graph", out var g)) ReadJsonLd(g, res, src);
        var types = el.TryGetProperty("@type", out var t) ? (t.ValueKind == JsonValueKind.Array ? t.EnumerateArray().Select(x => x.GetString() ?? "") : new[] { t.GetString() ?? "" }) : Array.Empty<string>();
        if (!types.Any(x => Regex.IsMatch(x, @"Organization|Business|Corporation|Store|Company|School|University|Agency|Contractor|Manufacturer", RegexOptions.IgnoreCase))) return;
        string? S(string n) => el.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? Clean(v.GetString()) : null;
        res.CompanyName ??= S("name") ?? S("legalName");
        res.Description ??= S("description");
        if (S("telephone") is { } tel) AddPhone(tel, res, src);
        if (S("email") is { } em) AddEmail(em.Replace("mailto:", ""), res, src);
        if (el.TryGetProperty("address", out var a))
        {
            if (a.ValueKind == JsonValueKind.String) res.Address ??= Clean(a.GetString());
            else if (a.ValueKind == JsonValueKind.Object)
            {
                string? P(string n) => a.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? Clean(v.GetString()) : null;
                var addr = string.Join(", ", new[] { P("streetAddress"), P("addressLocality"), P("addressRegion"), P("postalCode"), P("addressCountry") }.Where(x => !string.IsNullOrWhiteSpace(x)));
                if (addr.Length > 0) res.Address ??= addr;
            }
        }
        if (el.TryGetProperty("makesOffer", out var offers) && offers.ValueKind == JsonValueKind.Array)
            foreach (var o in offers.EnumerateArray().Take(12))
                if (o.TryGetProperty("itemOffered", out var item) && item.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String && Clean(n.GetString()) is { } name && !res.Services.Contains(name)) res.Services.Add(name);
    }

    private static string? Clean(string? s) { if (string.IsNullOrWhiteSpace(s)) return null; var t = Regex.Replace(HtmlEntity.DeEntitize(s), @"\s+", " ").Trim(); return t.Length == 0 ? null : t; }
    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];
}
