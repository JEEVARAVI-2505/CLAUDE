using LeadPilot.Application.Abstractions;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

/// <summary>Retention purge, full data deletion and audit-logged destructive operations.</summary>
public sealed class DataGovernanceService
{
    private readonly ILeadRepository _leads;
    private readonly ISystemRepository _sys;
    private readonly ISettingsService _settings;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public DataGovernanceService(ILeadRepository leads, ISystemRepository sys, ISettingsService settings, ICurrentUser user, IClock clock)
    { _leads = leads; _sys = sys; _settings = settings; _user = user; _clock = clock; }

    /// <summary>Deletes closed (Lost/Unqualified/archived) leads older than the configured retention. Returns rows removed; 0 when retention is off.</summary>
    public int ApplyRetention()
    {
        var days = _settings.Current.RetentionDays;
        if (days <= 0) return 0;
        var n = _leads.PurgeClosedBefore(_clock.UtcNow.AddDays(-days));
        if (n > 0) _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = _user.UserId, Action = "retention.purge", Details = $"{n} closed leads older than {days} days removed" });
        return n;
    }

    /// <summary>Removes every lead, contact, task and campaign (the "right to erasure" for the whole database). Requires Admin.</summary>
    public void DeleteAllData()
    {
        if (!_user.Can(Permission.ManageSettings)) throw new UnauthorizedAccessException("Only an administrator can delete all data.");
        _sys.DeleteAllBusinessData();
        _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = _user.UserId, Action = "data.delete_all", Details = "All business data deleted by user request" });
    }

    /// <summary>Permanently deletes selected leads (audited).</summary>
    public void DeleteLeads(IReadOnlyCollection<int> ids)
    {
        if (!_user.Can(Permission.DeleteLeads)) throw new UnauthorizedAccessException("You do not have permission to delete leads.");
        _leads.Delete(ids);
        _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = _user.UserId, Action = "lead.delete", Details = $"{ids.Count} lead(s) deleted" });
    }
}
