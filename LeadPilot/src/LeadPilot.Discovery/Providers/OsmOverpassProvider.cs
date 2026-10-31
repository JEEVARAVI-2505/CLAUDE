using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;

namespace LeadPilot.Discovery.Providers;

/// <summary>
/// Free, key-less business discovery from OpenStreetMap: Nominatim resolves the location, Overpass returns named businesses with their
/// published contact tags. Data is © OpenStreetMap contributors (ODbL 1.0); attribution is stored with every lead. Only information that
/// volunteers have mapped is returned, so phone/e-mail/website are often missing – this is reported, never filled in.
/// Respect the public servers' usage policies; for heavy use point <c>Endpoint</c> at your own Overpass instance.
/// </summary>
public sealed class OsmOverpassProvider : IDiscoveryProvider
{
    public const string DefaultOverpass = "https://overpass-api.de/api/interpreter";
    public const string DefaultNominatim = "https://nominatim.openstreetmap.org/search";
    private readonly HttpClient _http;
    private readonly string _nominatim;
    private readonly Dictionary<string, (DateTime At, List<ProspectCandidate> Items, List<string> Warnings)> _cache = new();

    public OsmOverpassProvider(HttpClient http, string? nominatimUrl = null) { _http = http; _nominatim = nominatimUrl ?? DefaultNominatim; }

    public string Id => "osm-overpass";
    public string DisplayName => "OpenStreetMap (free, no key)";
    public string SourceType => "Public business directory";
    public string Description => "Businesses mapped by the OpenStreetMap community: name, address and – where volunteers entered them – phone, website and e-mail.";
    public bool RequiresApiKey => false;
    public string CostNote => "Free. Public servers allow light, polite use only (this app waits between requests). Coverage varies a lot by city and category.";
    public string LicenseNote => "© OpenStreetMap contributors, ODbL 1.0. You may store and use the data if you keep this attribution.";
    public TimeSpan MinInterval { get; set; } = TimeSpan.FromSeconds(1.2);

    public async Task<ProviderAvailability> CheckAvailabilityAsync(ProviderSettings settings, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct); cts.CancelAfter(TimeSpan.FromSeconds(8));
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(_nominatim), "status.php"));
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            return resp.IsSuccessStatusCode ? new(true, "Reachable") : new(false, $"OpenStreetMap geocoder answered HTTP {(int)resp.StatusCode}.");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { return new(false, "Cannot reach OpenStreetMap services (are you offline?)."); }
    }

    public async Task<ProviderPage> SearchPageAsync(DiscoveryRequest request, string? pageToken, ProviderSettings settings, CancellationToken ct)
    {
        var key = $"{request.Location}|{request.TargetCustomer}|{request.BusinessCategory}|{request.Industry}|{request.Count}".ToLowerInvariant();
        if (!_cache.TryGetValue(key, out var hit) || DateTime.UtcNow - hit.At > TimeSpan.FromMinutes(10))
        {
            var (items, warnings) = await FetchAllAsync(request, settings, ct).ConfigureAwait(false);
            hit = (DateTime.UtcNow, items, warnings);
            _cache[key] = hit;
        }
        var offset = int.TryParse(pageToken, NumberStyles.None, CultureInfo.InvariantCulture, out var o) ? o : 0;
        const int pageSize = 25;
        var page = hit.Items.Skip(offset).Take(pageSize).ToList();
        var next = offset + pageSize < hit.Items.Count ? (offset + pageSize).ToString(CultureInfo.InvariantCulture) : null;
        return new ProviderPage(page, next, offset == 0 ? hit.Warnings : Array.Empty<string>());
    }

    public Task<ProspectCandidate?> GetDetailsAsync(string externalId, ProviderSettings settings, CancellationToken ct) => GetDetailsCoreAsync(externalId, settings, ct);

    private async Task<ProspectCandidate?> GetDetailsCoreAsync(string externalId, ProviderSettings settings, CancellationToken ct)
    {
        var parts = externalId.Split('/');
        if (parts.Length != 2 || parts[0] is not ("node" or "way" or "relation") || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var id)) return null;
        var json = await PostOverpassAsync($"[out:json][timeout:25];{parts[0]}({id});out tags center;", settings, ct).ConfigureAwait(false);
        return ParseElements(json, null, new List<string>()).FirstOrDefault();
    }

    private async Task<(List<ProspectCandidate>, List<string>)> FetchAllAsync(DiscoveryRequest r, ProviderSettings s, CancellationToken ct)
    {
        var warnings = new List<string>();
        var place = await GeocodeAsync(r.Location, s, ct).ConfigureAwait(false);
        if (place == null) return (new(), new() { $"OpenStreetMap could not find a place called \"{r.Location}\". Try a city name, e.g. \"Chennai, India\"." });

        var matches = CategoryMap.Match(r);
        var tagFilters = matches.SelectMany(m => m.Tags).Distinct().ToList();
        var regex = matches.Count > 0 ? string.Join('|', matches.Select(m => m.NameRegex)) : CategoryMap.FallbackRegex(r);
        if (tagFilters.Count == 0 && string.IsNullOrWhiteSpace(regex)) return (new(), new() { "The customer type was too generic to translate into OpenStreetMap categories. Try e.g. \"manufacturing companies\", \"schools\" or \"construction contractors\"." });

        var scope = place.AreaId != null ? "(area.a)" : $"({place.South.ToString(CultureInfo.InvariantCulture)},{place.West.ToString(CultureInfo.InvariantCulture)},{place.North.ToString(CultureInfo.InvariantCulture)},{place.East.ToString(CultureInfo.InvariantCulture)})";
        var sb = new StringBuilder("[out:json][timeout:60];\n");
        if (place.AreaId != null) sb.Append("area(id:").Append(place.AreaId.Value.ToString(CultureInfo.InvariantCulture)).Append(")->.a;\n");
        sb.Append("(\n");
        foreach (var t in tagFilters)
        {
            var kv = t.Split('=');
            sb.Append("  nwr[\"").Append(kv[0]).Append(kv[1] == "*" ? "\"" : "\"=\"" + kv[1] + "\"").Append("][\"name\"]").Append(scope).Append(";\n");
        }
        if (!string.IsNullOrWhiteSpace(regex))
            sb.Append("  nwr[\"name\"~\"").Append(regex.Replace("\"", "")).Append("\",i]").Append("[\"phone\"]").Append(scope).Append(";\n")
              .Append("  nwr[\"name\"~\"").Append(regex.Replace("\"", "")).Append("\",i]").Append("[\"website\"]").Append(scope).Append(";\n")
              .Append("  nwr[\"name\"~\"").Append(regex.Replace("\"", "")).Append("\",i]").Append("[\"contact:phone\"]").Append(scope).Append(";\n");
        var cap = Math.Clamp(r.Count * 6, 100, 2000);
        sb.Append(");\nout tags center ").Append(cap.ToString(CultureInfo.InvariantCulture)).Append(';');

        var json = await PostOverpassAsync(sb.ToString(), s, ct).ConfigureAwait(false);
        var items = ParseElements(json, place.Name, warnings, matches.FirstOrDefault()?.Industry);
        items = items.OrderByDescending(Richness).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
        if (items.Count == 0) warnings.Add($"OpenStreetMap has no matching businesses in {place.Name} for this customer type. Try a broader type or another source.");
        else
        {
            var noContact = items.Count(i => string.IsNullOrWhiteSpace(i.Phone) && string.IsNullOrWhiteSpace(i.Email) && string.IsNullOrWhiteSpace(i.Website));
            if (noContact > 0) warnings.Add($"{noContact} of {items.Count} results have no phone, e-mail or website in OpenStreetMap (left empty, not guessed).");
        }
        return (items, warnings);
    }

    private static int Richness(ProspectCandidate c) => (string.IsNullOrWhiteSpace(c.Phone) ? 0 : 2) + (string.IsNullOrWhiteSpace(c.Website) ? 0 : 2) + (string.IsNullOrWhiteSpace(c.Email) ? 0 : 1);

    private sealed record Place(string Name, long? AreaId, double South, double North, double West, double East);

    private async Task<Place?> GeocodeAsync(string location, ProviderSettings s, CancellationToken ct)
    {
        var url = $"{_nominatim}?format=jsonv2&limit=1&addressdetails=0&q={Uri.EscapeDataString(location)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        Ensure(resp);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) return null;
        var p = doc.RootElement[0];
        var type = p.TryGetProperty("osm_type", out var t) ? t.GetString() : null;
        var id = p.TryGetProperty("osm_id", out var i) && i.TryGetInt64(out var l) ? l : 0;
        long? area = type switch { "relation" => 3_600_000_000L + id, "way" => 2_400_000_000L + id, _ => null };
        var bb = p.TryGetProperty("boundingbox", out var b) && b.GetArrayLength() == 4 ? b.EnumerateArray().Select(x => double.Parse(x.GetString()!, CultureInfo.InvariantCulture)).ToArray() : new[] { 0.0, 0, 0, 0 };
        var name = p.TryGetProperty("display_name", out var dn) ? dn.GetString()! : location;
        var shortName = name.Split(',')[0].Trim();
        return new Place(shortName, area, bb[0], bb[1], bb[2], bb[3]);
    }

    private async Task<string> PostOverpassAsync(string ql, ProviderSettings s, CancellationToken ct)
    {
        var endpoint = string.IsNullOrWhiteSpace(s.Endpoint) ? DefaultOverpass : s.Endpoint!;
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("data", ql) }) };
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        Ensure(resp);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (body.Contains("\"remark\"", StringComparison.Ordinal) && body.Contains("runtime error", StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException("Overpass reported a runtime error (query too heavy or server busy).", null, HttpStatusCode.GatewayTimeout);
        return body;
    }

    private static void Ensure(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;
        throw new HttpRequestException($"HTTP {(int)resp.StatusCode}", null, resp.StatusCode);
    }

    /// <summary>Converts an Overpass JSON response into candidates. Public for fixture tests.</summary>
    public static List<ProspectCandidate> ParseElements(string json, string? defaultCity, List<string> warnings, string? industryHint = null)
    {
        var list = new List<ProspectCandidate>(); var seen = new HashSet<string>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("elements", out var els)) return list;
        var now = DateTime.UtcNow;
        foreach (var e in els.EnumerateArray())
        {
            if (!e.TryGetProperty("tags", out var tags)) continue;
            string? T(params string[] keys) { foreach (var k in keys) if (tags.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())) return v.GetString()!.Trim(); return null; }
            var name = T("name", "name:en", "brand"); if (name == null) continue;
            var type = e.GetProperty("type").GetString()!; var id = e.GetProperty("id").GetInt64();
            var ext = $"{type}/{id}"; if (!seen.Add(ext)) continue;
            double? lat = null, lon = null;
            if (e.TryGetProperty("lat", out var la) && e.TryGetProperty("lon", out var lo)) { lat = la.GetDouble(); lon = lo.GetDouble(); }
            else if (e.TryGetProperty("center", out var ce)) { lat = ce.GetProperty("lat").GetDouble(); lon = ce.GetProperty("lon").GetDouble(); }
            var street = string.Join(' ', new[] { T("addr:housenumber"), T("addr:street") }.Where(x => x != null));
            var addr = string.Join(", ", new[] { street, T("addr:suburb"), T("addr:city"), T("addr:postcode") }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var phone = T("phone", "contact:phone", "contact:mobile", "mobile")?.Split(';')[0].Trim();
            var email = T("email", "contact:email")?.Split(';')[0].Trim();
            var web = T("website", "contact:website", "url");
            var url = $"https://www.openstreetmap.org/{type}/{id}";
            var c = new ProspectCandidate
            {
                Name = name, Phone = phone, Email = email, Website = web, Address = string.IsNullOrWhiteSpace(addr) ? null : addr,
                City = T("addr:city") ?? defaultCity, Region = T("addr:state"), Country = T("addr:country"), PostalCode = T("addr:postcode"),
                Latitude = lat, Longitude = lon, Description = T("description"), Services = T("product", "craft", "industrial"),
                Industry = industryHint, ContactName = null,
                SourceName = "OpenStreetMap", SourceType = "Public business directory", SourceUrl = url, ExternalId = ext,
                License = "ODbL 1.0 © OpenStreetMap contributors", RetrievedAt = now, IsPublicListing = true
            };
            foreach (var (field, value) in new[] { ("Phone", phone), ("Email", email), ("Website", web) })
                if (!string.IsNullOrWhiteSpace(value)) c.Evidence.Add(new CandidateEvidence { Field = field, Value = value!, SourceUrl = url, Confidence = 0.7, Note = "Mapped by an OpenStreetMap contributor; not verified" });
            list.Add(c);
        }
        return list;
    }
}
