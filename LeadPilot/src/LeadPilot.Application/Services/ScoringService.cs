using System.Text.Json;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

/// <summary>Re-scores stored leads against their campaign (or the active) target profile.</summary>
public sealed class ScoringService
{
    private const string ActiveKey = "scoring.activeProfile";
    private readonly ILeadRepository _leads;
    private readonly IWorkRepository _work;
    private readonly ISystemRepository _sys;
    private readonly ISettingsService _settings;

    public ScoringService(ILeadRepository leads, IWorkRepository work, ISystemRepository sys, ISettingsService settings)
    { _leads = leads; _work = work; _sys = sys; _settings = settings; }

    /// <summary>The profile used for leads that have no campaign.</summary>
    public ScoringProfile GetActiveProfile()
    {
        var json = _sys.GetSetting(ActiveKey);
        if (string.IsNullOrWhiteSpace(json)) return new ScoringProfile();
        try { return JsonSerializer.Deserialize<ScoringProfile>(json) ?? new ScoringProfile(); } catch (JsonException) { return new ScoringProfile(); }
    }

    public void SetActiveProfile(ScoringProfile p) => _sys.SetSetting(ActiveKey, JsonSerializer.Serialize(p));

    /// <summary>Profile for a lead: its campaign's target, else the active profile.</summary>
    public ScoringProfile ProfileFor(Lead lead)
    {
        if (lead.CampaignId is { } id && _work.GetCampaign(id) is { } c)
            return new ScoringProfile { Product = c.Product, TargetCustomer = c.TargetCustomer, Location = c.Location };
        return GetActiveProfile();
    }

    /// <summary>Builds scorer inputs from a stored lead.</summary>
    public static LeadFacts FactsFor(LeadDetail d)
    {
        var primary = d.Contacts.FirstOrDefault(c => c.Id == d.Lead.PrimaryContactId);
        return new LeadFacts
        {
            Name = d.Business.Name, Industry = d.Business.Industry, Services = d.Business.Services, Description = d.Business.Description, Address = d.Business.Address,
            City = d.Business.City, Region = d.Business.Region, Country = d.Business.Country, CompanySize = d.Business.CompanySize,
            HasValidPhone = d.Business.NormalizedPhone != null || primary?.NormalizedPhone != null,
            HasValidEmail = d.Business.NormalizedEmail != null || primary?.NormalizedEmail != null,
            HasWebsite = !string.IsNullOrWhiteSpace(d.Business.Website), HasContactPerson = primary != null, IntentEvidence = d.Lead.IntentEvidence
        };
    }

    /// <summary>Re-scores one lead and stores the explanation. Returns null when the lead does not exist.</summary>
    public ScoreResult? Rescore(int leadId)
    {
        var d = _leads.GetDetail(leadId);
        if (d == null) return null;
        var res = LeadScorer.Score(ProfileFor(d.Lead), FactsFor(d), _settings.Current.Scoring);
        d.Lead.Score = res.Score; d.Lead.Temperature = res.Temperature; d.Lead.ScoreExplanation = res.Explain();
        _leads.UpdateLead(d.Lead);
        return res;
    }

    /// <summary>Re-scores every active lead (e.g. after the weights change). Returns the number scored.</summary>
    public int RescoreAll(IProgress<int>? progress = null)
    {
        var ids = _leads.Export(new LeadQuery { Sort = LeadSort.LeadId, Descending = false }).Select(r => r.Id).ToList();
        var n = 0;
        foreach (var id in ids) { if (Rescore(id) != null) n++; progress?.Report(n); }
        return n;
    }
}
