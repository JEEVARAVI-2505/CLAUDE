using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

public sealed record FollowUpRow(int TaskId, int LeadId, string LeadCode, string Company, string Title, string Due, string Status, string AssignedTo, bool IsOverdue, bool IsOpen);
public sealed record NextActionRow(int LeadId, string LeadCode, string Company, string Action, string Reason);

/// <summary>Tasks, overdue alerts, and rule-based next-action suggestions.</summary>
public sealed partial class FollowUpViewModel : ViewModelBase
{
    private readonly FollowUpService _svc;
    private readonly ILeadRepository _leads;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly INavigator _nav;

    [ObservableProperty] private string _filter = "Open";
    [ObservableProperty] private int _overdueCount;
    [ObservableProperty] private string _newLeadCode = "";
    [ObservableProperty] private string _newTitle = "";
    [ObservableProperty] private DateTime _newDue = DateTime.Today.AddDays(1);

    public IReadOnlyList<string> Filters { get; } = new[] { "Open", "Overdue", "Done", "All" };
    public ObservableCollection<FollowUpRow> Tasks { get; } = new();
    public ObservableCollection<NextActionRow> Suggestions { get; } = new();
    public bool IsEmpty => Tasks.Count == 0;

    public FollowUpViewModel(FollowUpService svc, ILeadRepository leads, IClock clock, ICurrentUser user, INavigator nav)
    { _svc = svc; _leads = leads; _clock = clock; _user = user; _nav = nav; }

    public override Task OnNavigatedToAsync(object? parameter) => RefreshCommand.ExecuteAsync(null);
    partial void OnFilterChanged(string value) => _ = RefreshCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task Refresh() => RunAsync(async () =>
    {
        var now = _clock.UtcNow;
        var q = Filter switch { "Overdue" => new TaskQuery { OnlyOverdue = true }, "Done" => new TaskQuery { Status = FollowUpStatus.Done }, "All" => new TaskQuery(), _ => new TaskQuery { Status = FollowUpStatus.Open } };
        var (tasks, overdue, open, rows) = await Task.Run(() => (_svc.All(q), _svc.Overdue(), _svc.Open(), _leads.Search(new LeadQuery { Sort = LeadSort.Score, PageSize = 100 }).Rows));
        Tasks.Clear();
        foreach (var t in tasks) Tasks.Add(new FollowUpRow(t.Task.Id, t.Task.LeadId, t.LeadCode, t.CompanyName, t.Task.Title, t.Task.DueAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"), t.Task.Status.ToString(), t.AssignedTo ?? "", t.IsOverdue(now), t.Task.Status == FollowUpStatus.Open));
        OverdueCount = overdue.Count;
        var withTask = open.Select(o => o.Task.LeadId).ToHashSet();
        Suggestions.Clear();
        foreach (var r in rows.Where(r => !withTask.Contains(r.Id)))
            foreach (var s in _svc.Suggest(r, false).Take(1)) { Suggestions.Add(new NextActionRow(r.Id, r.LeadCode, r.CompanyName, s.Title, s.Reason)); break; }
        while (Suggestions.Count > 15) Suggestions.RemoveAt(Suggestions.Count - 1);
        OnPropertyChanged(nameof(IsEmpty));
    });

    private async Task Act(Action a, string status)
    {
        await RunAsync(async () =>
        {
            if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to edit follow-ups.");
            await Task.Run(a); StatusMessage = status;
        });
        if (!HasError) await RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand] private Task Complete(FollowUpRow? r) => r == null ? Task.CompletedTask : Act(() => _svc.Complete(r.TaskId), "Marked done.");
    [RelayCommand] private Task Snooze(FollowUpRow? r) => r == null ? Task.CompletedTask : Act(() => _svc.Snooze(r.TaskId, TimeSpan.FromDays(1)), "Moved to tomorrow.");
    [RelayCommand] private Task Cancel(FollowUpRow? r) => r == null ? Task.CompletedTask : Act(() => _svc.Cancel(r.TaskId), "Cancelled.");
    [RelayCommand] private void OpenLead(FollowUpRow? r) { if (r != null) _nav.Navigate(PageKey.LeadDetails, r.LeadId); }
    [RelayCommand] private void OpenSuggestion(NextActionRow? r) { if (r != null) _nav.Navigate(PageKey.LeadDetails, r.LeadId); }

    [RelayCommand]
    private Task Schedule(NextActionRow? r) => r == null ? Task.CompletedTask : Act(() => _svc.Schedule(r.LeadId, r.Action, DateTime.UtcNow.AddDays(1), r.Reason), $"Follow-up scheduled for {r.LeadCode}.");

    [RelayCommand]
    private async Task AddTask()
    {
        await RunAsync(async () =>
        {
        if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to edit follow-ups.");
        var code = NewLeadCode.Trim().ToUpperInvariant();
        var row = (await Task.Run(() => _leads.Search(new LeadQuery { Text = code, PageSize = 5 }).Rows)).FirstOrDefault(r => r.LeadCode == code) ?? throw new InvalidOperationException($"No lead with ID {code}. Use the format LD-000001.");
        var due = DateTime.SpecifyKind(NewDue.Date.AddHours(9), DateTimeKind.Local).ToUniversalTime();
        await Task.Run(() => _svc.Schedule(row.Id, NewTitle, due));
            NewTitle = ""; NewLeadCode = ""; StatusMessage = "Follow-up scheduled.";
        });
        if (!HasError) await RefreshCommand.ExecuteAsync(null);
    }
}
