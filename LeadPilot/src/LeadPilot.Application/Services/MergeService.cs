using System.Text.Json;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Duplicates;
using LeadPilot.Application.Models;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

/// <summary>Finds duplicates, merges two leads (never destroying the originals) and undoes merges.</summary>
public sealed class DuplicateService
{
    private readonly ILeadRepository _leads;
    private readonly IDuplicateRepository _dups;
    private readonly ISystemRepository _sys;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public DuplicateService(ILeadRepository leads, IDuplicateRepository dups, ISystemRepository sys, ICurrentUser user, IClock clock)
    { _leads = leads; _dups = dups; _sys = sys; _user = user; _clock = clock; }

    /// <summary>Scans all active leads and stores new candidate pairs. Returns the number of new pairs.</summary>
    public int Scan() => _dups.UpsertMatches(DuplicateDetector.FindAll(_leads.GetMatchProfiles().ToList()));

    public IReadOnlyList<DuplicateView> Pending() => _dups.GetMatches(DuplicateDecision.Pending);
    public IReadOnlyList<MergeRecord> History() => _dups.GetMerges();

    /// <summary>Marks a pair as "not duplicates" so it is not suggested again.</summary>
    public void MarkNotDuplicate(int matchId)
    {
        _dups.SetDecision(matchId, DuplicateDecision.NotDuplicate, _user.User?.Name);
        _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = _user.UserId, Action = "duplicate.reject", Entity = "DuplicateMatch", EntityId = matchId.ToString() });
    }

    /// <summary>Merges <paramref name="mergedId"/> into <paramref name="survivorId"/>. Empty survivor fields are filled from the merged lead; nothing is overwritten.</summary>
    public int Merge(int survivorId, int mergedId)
    {
        if (survivorId == mergedId) throw new InvalidOperationException("Cannot merge a lead into itself.");
        var s = _leads.GetDetail(survivorId) ?? throw new InvalidOperationException("Survivor lead not found.");
        var m = _leads.GetDetail(mergedId) ?? throw new InvalidOperationException("Lead to merge not found.");
        if (s.Lead.IsArchived || m.Lead.IsArchived) throw new InvalidOperationException("Archived leads cannot be merged.");

        var after = s.Business.Clone();
        after.Website ??= Blank(m.Business.Website); after.Domain ??= Blank(m.Business.Domain);
        after.Phone ??= Blank(m.Business.Phone); after.NormalizedPhone ??= m.Business.NormalizedPhone;
        after.Email ??= Blank(m.Business.Email); after.NormalizedEmail ??= m.Business.NormalizedEmail;
        after.Address ??= Blank(m.Business.Address); after.City ??= Blank(m.Business.City); after.Region ??= Blank(m.Business.Region);
        after.Country ??= Blank(m.Business.Country); after.PostalCode ??= Blank(m.Business.PostalCode); after.Industry ??= Blank(m.Business.Industry);
        after.Services ??= Blank(m.Business.Services); after.Description ??= Blank(m.Business.Description); after.ContactPageUrl ??= Blank(m.Business.ContactPageUrl);
        after.CompanySize ??= Blank(m.Business.CompanySize); after.Latitude ??= m.Business.Latitude; after.Longitude ??= m.Business.Longitude;

        var plan = new MergePlan
        {
            SurvivorLeadId = survivorId, MergedLeadId = mergedId, SurvivorBusinessAfter = after,
            SurvivorPrimaryContactAfter = s.Lead.PrimaryContactId ?? m.Lead.PrimaryContactId,
            SurvivorBeforeJson = JsonSerializer.Serialize(new LeadSnapshot { Business = s.Business, Lead = s.Lead }),
            MergedBeforeJson = JsonSerializer.Serialize(new LeadSnapshot { Business = m.Business, Lead = m.Lead }),
            MergedBy = _user.User?.Name
        };
        var id = _dups.ApplyMerge(plan);
        _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = _user.UserId, Action = "lead.merge", Entity = "Lead", EntityId = survivorId.ToString(), Details = $"{m.Lead.LeadCode} merged into {s.Lead.LeadCode}" });
        return id;
    }

    /// <summary>Restores both leads to their state before the merge.</summary>
    public void Undo(int mergeId)
    {
        _dups.UndoMerge(mergeId, _user.User?.Name);
        _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = _user.UserId, Action = "lead.merge.undo", Entity = "MergeHistory", EntityId = mergeId.ToString() });
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
