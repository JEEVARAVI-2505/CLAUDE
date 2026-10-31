using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

/// <summary>Outcome of verifying one lead. Statuses describe what was checked, never that a person owns a contact.</summary>
public sealed record VerificationResult(EmailStatus Email, PhoneStatus Phone, WebsiteStatus Website, IReadOnlyList<string> Notes);

/// <summary>Separate e-mail, phone and website checks. Network checks are optional and never SMTP-probe mailboxes.</summary>
public sealed class VerificationService
{
    private readonly ILeadRepository _leads;
    private readonly IWorkRepository _work;
    private readonly IDnsResolver _dns;
    private readonly IWebsiteProbe _probe;
    private readonly ISettingsService _settings;

    public VerificationService(ILeadRepository leads, IWorkRepository work, IDnsResolver dns, IWebsiteProbe probe, ISettingsService settings)
    { _leads = leads; _work = work; _dns = dns; _probe = probe; _settings = settings; }

    /// <summary>E-mail status from format and (optionally) DNS. A status set by an authorised provider is never downgraded.</summary>
    public async Task<(EmailStatus, string)> CheckEmailAsync(string? email, EmailStatus current, bool network, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return (EmailStatus.Unverified, "No e-mail on record.");
        if (current == EmailStatus.VerifiedByProvider) return (current, "Verified by an authorised provider (kept).");
        if (!Normalizer.IsValidEmailFormat(email)) return (EmailStatus.InvalidFormat, "E-mail format is invalid.");
        if (!network) return (EmailStatus.ValidFormat, "Format is valid; domain not checked.");
        var dns = await _dns.CheckAsync(Normalizer.EmailDomain(email)!, ct).ConfigureAwait(false);
        if (dns.Error != null) return (EmailStatus.ValidFormat, $"Format valid; DNS check unavailable ({dns.Error}).");
        if (!dns.DomainExists) return (EmailStatus.ValidFormat, "Format valid but the domain does not resolve.");
        return dns.HasMailServer ? (EmailStatus.MailServerFound, "Domain publishes a mail server. This does not prove the mailbox exists.") : (EmailStatus.DomainExists, "Domain exists but no mail server was found.");
    }

    /// <summary>Phone status from parsing against the default region.</summary>
    public (PhoneStatus, string) CheckPhone(string? phone, PhoneStatus current, bool publiclyListed)
    {
        if (string.IsNullOrWhiteSpace(phone)) return (PhoneStatus.Unverified, "No phone on record.");
        if (current == PhoneStatus.VerifiedByProvider) return (current, "Verified by an authorised provider (kept).");
        var info = Normalizer.ParsePhone(phone, _settings.Current.DefaultRegion);
        if (!info.IsValid) return (PhoneStatus.InvalidFormat, "Number is not valid for its country.");
        return (publiclyListed ? PhoneStatus.PubliclyListed : PhoneStatus.CountryIdentified, $"Valid number ({info.E164}, region {info.Region}). Ownership is not verified.");
    }

    /// <summary>Website status from an HTTP probe.</summary>
    public async Task<(WebsiteStatus, string)> CheckWebsiteAsync(string? website, CancellationToken ct)
    {
        var url = Normalizer.NormalizeUrl(website);
        if (url == null) return (WebsiteStatus.Unverified, "No website on record.");
        var r = await _probe.ProbeAsync(url, ct).ConfigureAwait(false);
        if (!r.Reachable) return (WebsiteStatus.Unavailable, $"Not reachable ({r.Error ?? "HTTP " + r.StatusCode}).");
        if (r.FinalUrl != null && !Normalizer.SameSite(url, r.FinalUrl)) return (WebsiteStatus.DomainMismatch, $"Redirects to a different domain: {Normalizer.DomainOf(r.FinalUrl)}.");
        return r.Redirected ? (WebsiteStatus.Redirected, "Reachable after redirect within the same site.") : (WebsiteStatus.Accessible, "Reachable.");
    }

    /// <summary>Verifies a stored lead and saves the resulting statuses.</summary>
    public async Task<VerificationResult?> VerifyLeadAsync(int leadId, bool network, CancellationToken ct = default)
    {
        var d = _leads.GetDetail(leadId);
        if (d == null) return null;
        var primary = d.Contacts.FirstOrDefault(c => c.Id == d.Lead.PrimaryContactId);
        var email = primary?.Email ?? d.Business.Email;
        var phone = primary?.Phone ?? d.Business.Phone;
        var publiclyListed = d.Evidence.Any(e => e.FieldName == "Phone" && e.Applied && e.Confidence < 0.9);
        var notes = new List<string>();
        var (es, en) = await CheckEmailAsync(email, d.Lead.EmailStatus, network, ct).ConfigureAwait(false); notes.Add("E-mail: " + en);
        var (ps, pn) = CheckPhone(phone, d.Lead.PhoneStatus, publiclyListed); notes.Add("Phone: " + pn);
        var ws = d.Lead.WebsiteStatus;
        if (network) { var (w, wn) = await CheckWebsiteAsync(d.Business.Website, ct).ConfigureAwait(false); ws = w; notes.Add("Website: " + wn); }
        d.Lead.EmailStatus = es; d.Lead.PhoneStatus = ps; d.Lead.WebsiteStatus = ws;
        _leads.UpdateLead(d.Lead);
        _work.AddActivity(new LeadActivity { LeadId = leadId, Type = ActivityType.Enriched, Subject = "Contact checks run", Body = string.Join('\n', notes) });
        return new VerificationResult(es, ps, ws, notes);
    }
}
