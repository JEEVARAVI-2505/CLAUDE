using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

/// <summary>A rule-based suggestion for what to do next with a lead.</summary>
public sealed record NextAction(string Title, string Reason, int DueInDays);

/// <summary>Follow-up tasks, interaction logging and next-action suggestions.</summary>
public sealed class FollowUpService
{
    private readonly ILeadRepository _leads;
    private readonly IWorkRepository _work;
    private readonly ISystemRepository _sys;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public FollowUpService(ILeadRepository leads, IWorkRepository work, ISystemRepository sys, ICurrentUser user, IClock clock)
    { _leads = leads; _work = work; _sys = sys; _user = user; _clock = clock; }

    public int Schedule(int leadId, string title, DateTime dueUtc, string? notes = null, int? assignee = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Enter a task title.");
        if (_leads.GetLead(leadId) == null) throw new ArgumentException("Lead not found.");
        var id = _work.AddTask(new FollowUpTask { LeadId = leadId, Title = title.Trim(), Notes = notes, DueAt = dueUtc, AssignedUserId = assignee ?? _user.UserId });
        _work.AddActivity(new LeadActivity { LeadId = leadId, Type = ActivityType.Note, Subject = $"Follow-up scheduled: {title.Trim()}", Body = $"Due {dueUtc:u}", UserId = _user.UserId });
        return id;
    }

    public void Complete(int taskId, string? outcome = null)
    {
        var t = _work.GetTask(taskId) ?? throw new ArgumentException("Task not found.");
        t.Status = FollowUpStatus.Done; t.CompletedAt = _clock.UtcNow;
        if (!string.IsNullOrWhiteSpace(outcome)) t.Notes = string.IsNullOrWhiteSpace(t.Notes) ? outcome : t.Notes + "\nOutcome: " + outcome;
        _work.UpdateTask(t);
        _work.AddActivity(new LeadActivity { LeadId = t.LeadId, Type = ActivityType.Note, Subject = $"Follow-up completed: {t.Title}", Body = outcome, UserId = _user.UserId });
    }

    public void Snooze(int taskId, TimeSpan by)
    {
        var t = _work.GetTask(taskId) ?? throw new ArgumentException("Task not found.");
        t.DueAt = (t.DueAt < _clock.UtcNow ? _clock.UtcNow : t.DueAt) + by;
        _work.UpdateTask(t);
    }

    public void Cancel(int taskId)
    {
        var t = _work.GetTask(taskId) ?? throw new ArgumentException("Task not found.");
        t.Status = FollowUpStatus.Cancelled; _work.UpdateTask(t);
    }

    /// <summary>Records a call/e-mail/meeting/note. A first contact moves a New lead to Contacted.</summary>
    public void LogInteraction(int leadId, ActivityType type, string subject, string? body)
    {
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Enter a subject for the interaction.");
        _work.AddActivity(new LeadActivity { LeadId = leadId, Type = type, Subject = subject.Trim(), Body = body, UserId = _user.UserId });
        var lead = _leads.GetLead(leadId);
        if (lead != null && lead.Status == LeadStatus.New && type is ActivityType.Call or ActivityType.Email or ActivityType.Meeting or ActivityType.WhatsApp)
        { lead.Status = LeadStatus.Contacted; _leads.UpdateLead(lead); }
    }

    public IReadOnlyList<TaskRow> Open() => _work.GetTasks(new TaskQuery { Status = FollowUpStatus.Open }, _clock.UtcNow);
    public IReadOnlyList<TaskRow> Overdue() => _work.GetTasks(new TaskQuery { OnlyOverdue = true }, _clock.UtcNow);
    public IReadOnlyList<TaskRow> All(TaskQuery q) => _work.GetTasks(q, _clock.UtcNow);

    /// <summary>Deterministic suggestions based on status, score, contact data and recency.</summary>
    public IReadOnlyList<NextAction> Suggest(LeadRow lead, bool hasOpenTask)
    {
        var now = _clock.UtcNow; var list = new List<NextAction>();
        if (lead.Status is LeadStatus.Won or LeadStatus.Lost or LeadStatus.Unqualified) return list;
        if (string.IsNullOrWhiteSpace(lead.Phone) && string.IsNullOrWhiteSpace(lead.Email))
            list.Add(new("Find contact details", "No phone or e-mail on record – run enrichment or add them manually.", 1));
        if (lead.Status == LeadStatus.New && lead.Temperature >= LeadTemperature.Warm)
            list.Add(new("Make first contact", $"{lead.Temperature} relevance and not yet contacted.", lead.Temperature == LeadTemperature.Hot ? 0 : 2));
        else if (lead.Status == LeadStatus.New)
            list.Add(new("Review and qualify", "New lead with no contact yet.", 3));
        if (lead.Status == LeadStatus.Contacted && (lead.LastContactedAt == null || (now - lead.LastContactedAt.Value).TotalDays >= 7))
            list.Add(new("Send a follow-up", lead.LastContactedAt == null ? "Marked contacted but no interaction logged." : $"Last contact was {(now - lead.LastContactedAt.Value).TotalDays:0} days ago.", 1));
        if (lead.Status == LeadStatus.Qualified) list.Add(new("Send proposal or quote", "Lead is qualified.", 2));
        if (lead.Status == LeadStatus.Proposal) list.Add(new("Chase the proposal", "Proposal sent – confirm the decision timeline.", 3));
        if (!hasOpenTask && list.Count == 0) list.Add(new("Schedule a check-in", "No follow-up is scheduled.", 7));
        return list;
    }
}
