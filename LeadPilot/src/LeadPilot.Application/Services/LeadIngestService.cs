using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Duplicates;
using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

/// <summary>Per-run options for ingest.</summary>
public sealed class IngestContext
{
    public int? CampaignId { get; set; }
    public ScoringProfile Profile { get; set; } = new();
    public int? UserId { get; set; }
    /// <summary>When true (default) a High-confidence duplicate is not inserted; its source is attached to the existing lead instead.</summary>
    public bool SkipHighConfidenceDuplicates { get; set; } = true;
    public int? AssignedUserId { get; set; }
}

public enum IngestStatus { Created, DuplicateLinked, Skipped, Invalid }

/// <summary>What happened to one candidate.</summary>
public sealed record IngestOutcome(IngestStatus Status, int? LeadId, string Message, IReadOnlyList<DuplicateResult> Matches);

/// <summary>Turns candidates (from providers, files or manual entry) into stored, scored, de-duplicated leads.</summary>
public sealed class LeadIngestService
{
    private readonly ILeadRepository _leads;
    private readonly IDuplicateRepository _dups;
    private readonly ISettingsService _settings;
    private readonly ILicenseService _license;
    private readonly IClock _clock;

    public LeadIngestService(ILeadRepository leads, IDuplicateRepository dups, ISettingsService settings, ILicenseService license, IClock clock)
    { _leads = leads; _dups = dups; _settings = settings; _license = license; _clock = clock; }

    /// <summary>Ingests many candidates, building the duplicate index once.</summary>
    public IReadOnlyList<IngestOutcome> IngestMany(IEnumerable<ProspectCandidate> candidates, IngestContext ctx, IProgress<int>? progress = null)
    {
        var index = new DuplicateIndex(_leads.GetMatchProfiles());
        var results = new List<IngestOutcome>();
        foreach (var c in candidates) { results.Add(Ingest(c, ctx, index)); progress?.Report(results.Count); }
        return results;
    }

    /// <summary>Ingests one candidate.</summary>
    public IngestOutcome Ingest(ProspectCandidate c, IngestContext ctx, DuplicateIndex? index = null)
    {
        if (string.IsNullOrWhiteSpace(c.Name)) return new(IngestStatus.Invalid, null, "Company name is missing.", Array.Empty<DuplicateResult>());
        var lic = _license.Current;
        if (!lic.IsValid) return new(IngestStatus.Skipped, null, lic.Message, Array.Empty<DuplicateResult>());
        if (_leads.Count() >= lic.MaxLeads) return new(IngestStatus.Skipped, null, $"Licence limit of {lic.MaxLeads} leads reached. Upgrade your licence to add more.", Array.Empty<DuplicateResult>());

        var region = _settings.Current.DefaultRegion;
        var ins = Build(c, ctx, region);
        var probe = MatchProfile.From(0, "", ins.Business.Name, ins.Business.Website, new[] { ins.Business.NormalizedPhone, ins.Contact?.NormalizedPhone },
            new[] { ins.Business.NormalizedEmail, ins.Contact?.NormalizedEmail }, ins.Business.Address, ins.Business.City, region);
        index ??= new DuplicateIndex(_leads.GetMatchProfiles());
        var matches = index.Find(probe);

        if (ctx.SkipHighConfidenceDuplicates && matches.FirstOrDefault(m => m.Confidence == DuplicateConfidence.High) is { } high)
        {
            var existing = OtherId(high);
            AttachSource(existing, ins);
            return new(IngestStatus.DuplicateLinked, existing, $"Already in your database ({high.Reasons.FirstOrDefault()}); source recorded on the existing lead.", matches);
        }

        var id = _leads.Insert(ins);
        probe = MatchProfile.From(id, ins.Lead.LeadCode, ins.Business.Name, ins.Business.Website, new[] { ins.Business.NormalizedPhone, ins.Contact?.NormalizedPhone },
            new[] { ins.Business.NormalizedEmail, ins.Contact?.NormalizedEmail }, ins.Business.Address, ins.Business.City, region);
        if (matches.Count > 0)
            _dups.UpsertMatches(matches.Select(m => new DuplicateResult(Math.Min(id, OtherId(m)), Math.Max(id, OtherId(m)), m.Score, m.Confidence, m.Reasons)));
        index.Add(probe);
        return new(IngestStatus.Created, id, matches.Count > 0 ? $"Created; {matches.Count} possible duplicate(s) flagged for review." : "Created.", matches);
    }

    private static int OtherId(DuplicateResult m) => m.LeadAId == 0 ? m.LeadBId : m.LeadAId;

    private void AttachSource(int leadId, LeadInsert ins)
    {
        if (ins.Source != null) { ins.Source.LeadId = leadId; _leads.AddSource(ins.Source); }
        foreach (var e in ins.Evidence) { e.LeadId = leadId; e.Applied = false; e.Note = "Seen again in a later import/discovery; not applied automatically."; _leads.AddEvidence(e); }
    }

    /// <summary>Builds the rows to insert (normalised, scored, with evidence). Public for testing.</summary>
    public LeadInsert Build(ProspectCandidate c, IngestContext ctx, string region)
    {
        var now = _clock.UtcNow;
        var website = Normalizer.NormalizeUrl(c.Website);
        var phone = Normalizer.ParsePhone(c.Phone, region);
        var email = Normalizer.NormalizeEmail(c.Email);
        var industry = string.IsNullOrWhiteSpace(c.Industry) ? KeywordMatcher.GuessIndustry(c.Name, c.Services, c.Description) : c.Industry!.Trim();

        var biz = new Business
        {
            Name = c.Name.Trim(), Website = website, Domain = Normalizer.DomainOf(website),
            Phone = Clean(c.Phone), NormalizedPhone = phone.E164, Email = Clean(c.Email), NormalizedEmail = email,
            Address = Clean(c.Address), City = Clean(c.City), Region = Clean(c.Region), Country = Clean(c.Country), PostalCode = Clean(c.PostalCode),
            Industry = industry, Services = Clean(c.Services), Description = Clean(c.Description), CompanySize = Clean(c.CompanySize),
            Latitude = c.Latitude, Longitude = c.Longitude
        };
        Contact? contact = null;
        if (!string.IsNullOrWhiteSpace(c.ContactName))
        {
            var cp = Normalizer.ParsePhone(c.ContactPhone, region);
            contact = new Contact { FullName = c.ContactName.Trim(), JobTitle = Clean(c.ContactTitle), Phone = Clean(c.ContactPhone), NormalizedPhone = cp.E164, Email = Clean(c.ContactEmail), NormalizedEmail = Normalizer.NormalizeEmail(c.ContactEmail) };
        }

        var anyPhone = phone.IsValid || (contact?.NormalizedPhone != null);
        var anyEmail = email != null || contact?.NormalizedEmail != null;
        var facts = new LeadFacts
        {
            Name = biz.Name, Industry = biz.Industry, Services = biz.Services, Description = biz.Description, Address = biz.Address, City = biz.City, Region = biz.Region, Country = biz.Country,
            CompanySize = biz.CompanySize, HasValidPhone = anyPhone, HasValidEmail = anyEmail, HasWebsite = website != null, HasContactPerson = contact != null, IntentEvidence = c.IntentEvidence
        };
        var score = LeadScorer.Score(ctx.Profile, facts, _settings.Current.Scoring);

        var lead = new Lead
        {
            CampaignId = ctx.CampaignId, AssignedUserId = ctx.AssignedUserId, Score = score.Score, Temperature = score.Temperature, ScoreExplanation = score.Explain(),
            Status = ParseStatus(c.Status), SourceName = string.IsNullOrWhiteSpace(c.SourceName) ? "Manual" : c.SourceName,
            IntentEvidence = Clean(c.IntentEvidence),
            EmailStatus = !string.IsNullOrWhiteSpace(c.Email) ? (email != null ? EmailStatus.ValidFormat : EmailStatus.InvalidFormat) : EmailStatus.Unverified,
            PhoneStatus = !string.IsNullOrWhiteSpace(c.Phone) ? (phone.IsValid ? (c.IsPublicListing ? PhoneStatus.PubliclyListed : PhoneStatus.CountryIdentified) : PhoneStatus.InvalidFormat) : PhoneStatus.Unverified,
            WebsiteStatus = WebsiteStatus.Unverified
        };

        var ev = new List<FieldEvidence>();
        void Add(string field, string? value, double conf)
        {
            if (!string.IsNullOrWhiteSpace(value))
                ev.Add(new FieldEvidence { FieldName = field, Value = value!, SourceName = lead.SourceName, SourceUrl = c.SourceUrl, ExtractedAt = c.RetrievedAt, Confidence = conf, Applied = true });
        }
        var baseConf = c.IsPublicListing ? 0.7 : 0.9;
        Add("Phone", biz.Phone, baseConf); Add("Email", biz.Email, baseConf); Add("Website", biz.Website, baseConf); Add("Address", biz.Address, baseConf);
        foreach (var e in c.Evidence)
            ev.Add(new FieldEvidence { FieldName = e.Field, Value = e.Value, SourceName = lead.SourceName, SourceUrl = e.SourceUrl ?? c.SourceUrl, ExtractedAt = c.RetrievedAt, Confidence = e.Confidence, Applied = true, Note = e.Note });

        return new LeadInsert
        {
            Business = biz, Contact = contact, Lead = lead,
            Source = new LeadSourceRecord { SourceName = lead.SourceName, SourceType = string.IsNullOrWhiteSpace(c.SourceType) ? "Manual" : c.SourceType, SourceUrl = c.SourceUrl, ExternalId = c.ExternalId, License = c.License, RetrievedAt = c.RetrievedAt },
            Evidence = ev,
            Activity = new LeadActivity { Type = string.Equals(c.SourceType, "File import", StringComparison.Ordinal) ? ActivityType.Imported : ActivityType.Created, Subject = $"Lead created from {lead.SourceName}", UserId = ctx.UserId }
        };
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static LeadStatus ParseStatus(string? s) =>
        Enum.TryParse<LeadStatus>(s?.Replace(" ", ""), true, out var st) ? st : LeadStatus.New;
}
