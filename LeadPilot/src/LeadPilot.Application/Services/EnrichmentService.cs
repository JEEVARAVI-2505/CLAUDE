using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

/// <summary>What enrichment did for one lead.</summary>
public sealed class EnrichmentResult
{
    public int LeadId { get; init; }
    public bool Reached { get; set; }
    public List<string> Applied { get; } = new();
    public List<string> Suggestions { get; } = new();
    public List<string> Warnings { get; } = new();
    public string Summary => Applied.Count == 0 ? (Warnings.FirstOrDefault() ?? "Nothing new found.") : "Added: " + string.Join(", ", Applied);
}

/// <summary>
/// Fills gaps in a lead from its own official website. A value is applied only if the page belongs to the lead's website,
/// the company name on the page is similar enough, and the field is currently empty. Everything else is recorded as an
/// unapplied suggestion with its source. Nothing is invented; AI output is never applied automatically.
/// </summary>
public sealed class EnrichmentService
{
    public const double MinNameSimilarity = 0.6;
    private readonly ILeadRepository _leads;
    private readonly IWebsiteExtractor _extractor;
    private readonly IWebsiteFinder _finder;
    private readonly IAiClient _ai;
    private readonly ScoringService _scoring;
    private readonly ISettingsService _settings;
    private readonly IWorkRepository _work;
    private readonly IClock _clock;

    public EnrichmentService(ILeadRepository leads, IWebsiteExtractor extractor, IWebsiteFinder finder, IAiClient ai, ScoringService scoring, ISettingsService settings, IWorkRepository work, IClock clock)
    { _leads = leads; _extractor = extractor; _finder = finder; _ai = ai; _scoring = scoring; _settings = settings; _work = work; _clock = clock; }

    private static readonly string[] DirectoryDomains = { "facebook.com", "instagram.com", "linkedin.com", "justdial.com", "indiamart.com", "twitter.com", "x.com", "youtube.com", "wikipedia.org", "yellowpages.com", "sulekha.com", "tradeindia.com", "glassdoor.com", "zaubacorp.com" };

    public async Task<EnrichmentResult> EnrichAsync(int leadId, CancellationToken ct = default)
    {
        var res = new EnrichmentResult { LeadId = leadId };
        var d = _leads.GetDetail(leadId);
        if (d == null) { res.Warnings.Add("Lead not found."); return res; }
        var now = _clock.UtcNow; var region = _settings.Current.DefaultRegion;
        var biz = d.Business; var changed = false;

        void Evidence(string field, string value, string? url, double conf, bool applied, string? note) =>
            _leads.AddEvidence(new FieldEvidence { LeadId = leadId, FieldName = field, Value = value, SourceName = "Company website", SourceUrl = url, ExtractedAt = now, Confidence = conf, Applied = applied, Note = note });

        if (string.IsNullOrWhiteSpace(biz.Website))
        {
            if (!_finder.IsConfigured) { res.Warnings.Add("No website on record. Add one, or configure a web-search key in API Integrations so the app can look for the official site."); return res; }
            var found = (await _finder.FindAsync(biz.Name, biz.City, ct).ConfigureAwait(false))
                .Where(c => !DirectoryDomains.Any(x => Normalizer.SameSite(c.Url, x))).ToList();
            var best = found.Select(c => (c, sim: DomainNameSimilarity(biz.Name, c))).OrderByDescending(x => x.sim).FirstOrDefault();
            if (best.c == null || best.sim < 0.6) { res.Warnings.Add("No search result clearly matched the company name, so no website was applied."); return res; }
            biz.Website = Normalizer.NormalizeUrl(best.c.Url); biz.Domain = Normalizer.DomainOf(biz.Website);
            Evidence("Website", biz.Website!, best.c.Url, Math.Round(best.sim * 0.8, 2), true, $"Found via {best.c.SourceName}; domain/title similar to company name ({best.sim:P0}). Please confirm.");
            res.Applied.Add("Website"); changed = true;
        }

        var ex = await _extractor.ExtractAsync(biz.Website!, ct).ConfigureAwait(false);
        if (!ex.Success)
        {
            res.Warnings.Add(ex.BlockedByRobots ? "The site's robots.txt does not allow automated access, so nothing was read." : $"Could not read the website ({ex.Error ?? "unknown error"}).");
            if (!ex.BlockedByRobots) { d.Lead.WebsiteStatus = WebsiteStatus.Unavailable; _leads.UpdateLead(d.Lead); }
            if (changed) _leads.UpdateBusiness(biz);
            return res;
        }
        res.Reached = true;

        var sameSite = ex.FinalUrl == null || Normalizer.SameSite(biz.Website, ex.FinalUrl);
        if (!sameSite) { d.Lead.WebsiteStatus = WebsiteStatus.DomainMismatch; _leads.UpdateLead(d.Lead); res.Warnings.Add($"The website redirects to a different domain ({Normalizer.DomainOf(ex.FinalUrl)}); nothing was applied."); return res; }
        var nameSim = ex.CompanyName == null ? (double?)null : StringSimilarity.NameSimilarity(biz.Name, ex.CompanyName);
        if (nameSim is < MinNameSimilarity)
        {
            res.Warnings.Add($"The company name on the site (\"{ex.CompanyName}\") does not match \"{biz.Name}\" ({nameSim:P0}); data was recorded as suggestions only.");
            if (ex.Emails.Count > 0) { Evidence("Email", ex.Emails[0], ex.ValueSources.GetValueOrDefault(ex.Emails[0], ex.Url), 0.3, false, "Name mismatch – not applied."); res.Suggestions.Add("Email: " + ex.Emails[0]); }
            if (ex.Phones.Count > 0) { Evidence("Phone", ex.Phones[0], ex.ValueSources.GetValueOrDefault(ex.Phones[0], ex.Url), 0.3, false, "Name mismatch – not applied."); res.Suggestions.Add("Phone: " + ex.Phones[0]); }
            return res;
        }
        var conf = nameSim == null ? 0.6 : Math.Round(Math.Min(0.95, 0.5 + nameSim.Value * 0.4), 2);
        d.Lead.WebsiteStatus = Normalizer.SameSite(biz.Website, ex.FinalUrl ?? biz.Website) ? WebsiteStatus.Accessible : WebsiteStatus.Redirected;

        // Phone
        foreach (var p in ex.Phones)
        {
            var info = Normalizer.ParsePhone(p, region);
            if (!info.IsValid) continue;
            var src = ex.ValueSources.GetValueOrDefault(p, ex.Url);
            if (string.IsNullOrWhiteSpace(biz.Phone)) { biz.Phone = p; biz.NormalizedPhone = info.E164; d.Lead.PhoneStatus = PhoneStatus.PubliclyListed; Evidence("Phone", p, src, conf, true, "Publicly listed on the company's own website."); res.Applied.Add("Phone"); changed = true; }
            else if (info.E164 != biz.NormalizedPhone) { Evidence("Phone", p, src, conf - 0.1, false, "Alternate number; existing phone kept."); res.Suggestions.Add("Alternate phone: " + p); }
        }
        // Email – only addresses on the company's own domain are applied
        foreach (var e in ex.Emails)
        {
            var norm = Normalizer.NormalizeEmail(e); if (norm == null) continue;
            var src = ex.ValueSources.GetValueOrDefault(e, ex.Url);
            var own = Normalizer.SameSite(Normalizer.EmailDomain(norm), biz.Domain);
            if (string.IsNullOrWhiteSpace(biz.Email) && own) { biz.Email = e; biz.NormalizedEmail = norm; d.Lead.EmailStatus = EmailStatus.ValidFormat; Evidence("Email", e, src, conf, true, "Listed on the company's own website; format checked only."); res.Applied.Add("Email"); changed = true; }
            else { Evidence("Email", e, src, 0.4, false, own ? "Existing e-mail kept." : "Address is not on the company's domain – not applied."); res.Suggestions.Add("E-mail: " + e); }
        }
        if (string.IsNullOrWhiteSpace(biz.Address) && !string.IsNullOrWhiteSpace(ex.Address)) { biz.Address = ex.Address; Evidence("Address", ex.Address, ex.Url, conf, true, "From structured data on the website."); res.Applied.Add("Address"); changed = true; }
        if (string.IsNullOrWhiteSpace(biz.ContactPageUrl) && ex.ContactPageUrl != null) { biz.ContactPageUrl = ex.ContactPageUrl; Evidence("ContactPage", ex.ContactPageUrl, ex.ContactPageUrl, conf, true, null); res.Applied.Add("Contact page"); changed = true; }
        if (string.IsNullOrWhiteSpace(biz.Description) && !string.IsNullOrWhiteSpace(ex.Description)) { biz.Description = Trunc(ex.Description!, 500); Evidence("Description", biz.Description, ex.Url, conf, true, "Meta/structured description."); res.Applied.Add("Description"); changed = true; }
        if (string.IsNullOrWhiteSpace(biz.Services) && ex.Services.Count > 0) { biz.Services = Trunc(string.Join(", ", ex.Services), 500); Evidence("Services", biz.Services, ex.Url, conf - 0.1, true, "Headings/lists on the website."); res.Applied.Add("Services"); changed = true; }
        if (string.IsNullOrWhiteSpace(biz.Industry))
        {
            var guess = KeywordMatcher.GuessIndustry(ex.CompanyName, ex.Description, string.Join(' ', ex.Services), ex.AboutText);
            if (guess != null) { biz.Industry = guess; Evidence("Industry", guess, ex.Url, 0.6, true, "Keyword rules over the website text."); res.Applied.Add("Industry"); changed = true; }
        }

        if (_ai.IsAvailable && !string.IsNullOrWhiteSpace(ex.AboutText))
        {
            try
            {
                var s = await _ai.CompleteAsync("Summarise in one factual sentence what this company does, using ONLY the text provided. If unclear, reply UNKNOWN.", Trunc(ex.AboutText!, 2000), ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(s) && !s.Contains("UNKNOWN", StringComparison.OrdinalIgnoreCase))
                { Evidence("AI summary", Trunc(s.Trim(), 400), ex.Url, 0.4, false, "AI-generated from the website text; not applied automatically."); res.Suggestions.Add("AI summary (review before use)"); }
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException) { res.Warnings.Add("AI summary skipped: " + e.Message); }
        }

        if (changed) _leads.UpdateBusiness(biz);
        _leads.UpdateLead(d.Lead);
        _scoring.Rescore(leadId);
        _work.AddActivity(new LeadActivity { LeadId = leadId, Type = ActivityType.Enriched, Subject = res.Applied.Count > 0 ? "Enriched from company website" : "Enrichment found nothing new", Body = res.Summary + (res.Suggestions.Count > 0 ? "\nSuggestions: " + string.Join("; ", res.Suggestions) : "") });
        return res;
    }

    private static double DomainNameSimilarity(string name, WebsiteCandidate c)
    {
        var dom = (Normalizer.DomainOf(c.Url) ?? "").Split('.')[0].Replace("-", "");
        var core = Normalizer.CoreName(name).Replace(" ", "");
        var a = StringSimilarity.JaroWinkler(core, dom);
        var b = StringSimilarity.NameSimilarity(name, c.Title);
        return Math.Max(a, b);
    }

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];
}
