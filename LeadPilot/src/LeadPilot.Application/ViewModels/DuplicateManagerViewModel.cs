using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

public sealed record DuplicateItem(DuplicateView View, string Summary, string ConfidenceText, string Reasons);
public sealed record CompareRow(string Field, string ValueA, string ValueB, bool Differs);
public sealed record MergeHistoryRow(int Id, string When, string Survivor, string Merged, string State, bool CanUndo);

/// <summary>Finds likely duplicates, shows both records side by side and merges only when the user approves. Merges can be undone.</summary>
public sealed partial class DuplicateManagerViewModel : ViewModelBase
{
    private readonly DuplicateService _dups;
    private readonly ILeadRepository _leads;
    private readonly ICurrentUser _user;
    private readonly IDialogService _dlg;
    private readonly INavigator _nav;

    [ObservableProperty] private DuplicateItem? _selected;
    [ObservableProperty] private string _codeA = "";
    [ObservableProperty] private string _codeB = "";
    [ObservableProperty] private string _guidance = "";

    public ObservableCollection<DuplicateItem> Pending { get; } = new();
    public ObservableCollection<CompareRow> Comparison { get; } = new();
    public ObservableCollection<MergeHistoryRow> History { get; } = new();
    public bool HasPending => Pending.Count > 0;
    public bool HasSelection => Selected != null;

    public DuplicateManagerViewModel(DuplicateService dups, ILeadRepository leads, ICurrentUser user, IDialogService dlg, INavigator nav)
    { _dups = dups; _leads = leads; _user = user; _dlg = dlg; _nav = nav; }

    public override Task OnNavigatedToAsync(object? parameter) => RefreshAsync();
    private Task RefreshAsync() => RefreshCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task Refresh() => RunAsync(async () =>
    {
        var (pending, history) = await Task.Run(() => (_dups.Pending(), _dups.History()));
        Pending.Clear();
        foreach (var v in pending)
            Pending.Add(new DuplicateItem(v, $"{v.A.LeadCode} {v.A.CompanyName}  ↔  {v.B.LeadCode} {v.B.CompanyName}", $"{v.Match.Confidence} · {v.Match.Score:P0}", v.Match.Reasons));
        History.Clear();
        foreach (var h in history) History.Add(new MergeHistoryRow(h.Id, h.MergedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"), $"lead #{h.SurvivorLeadId}", $"lead #{h.MergedLeadId}", h.UndoneAt == null ? "Merged" : "Undone", h.UndoneAt == null));
        Selected = Pending.FirstOrDefault();
        OnPropertyChanged(nameof(HasPending));
    });

    [RelayCommand]
    private Task Scan() => RunAsync(async () =>
    {
        var added = await Task.Run(() => _dups.Scan());
        StatusMessage = added == 0 ? "Scan finished – no new duplicate pairs found." : $"Scan finished – {added} new pair(s) to review.";
        var pending = await Task.Run(() => _dups.Pending());
        Pending.Clear();
        foreach (var v in pending) Pending.Add(new DuplicateItem(v, $"{v.A.LeadCode} {v.A.CompanyName}  ↔  {v.B.LeadCode} {v.B.CompanyName}", $"{v.Match.Confidence} · {v.Match.Score:P0}", v.Match.Reasons));
        Selected = Pending.FirstOrDefault(); OnPropertyChanged(nameof(HasPending));
    }, "Scanning…");

    partial void OnSelectedChanged(DuplicateItem? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        Comparison.Clear();
        if (value == null) { CodeA = CodeB = Guidance = ""; return; }
        var a = _leads.GetDetail(value.View.A.Id); var b = _leads.GetDetail(value.View.B.Id);
        if (a == null || b == null) return;
        CodeA = a.Lead.LeadCode; CodeB = b.Lead.LeadCode;
        Guidance = value.View.Match.Confidence switch
        {
            DuplicateConfidence.High => "High confidence: a phone number, e-mail or website matches. Merging is usually right – but check below.",
            DuplicateConfidence.Medium => "Medium confidence: the names are the same or very similar but no contact detail matches. Two branches or different companies can share a name – only merge if you are sure.",
            _ => "Low confidence: only weak similarities. If in doubt, choose \"Not duplicates\"."
        };
        void Row(string f, string? x, string? y) => Comparison.Add(new CompareRow(f, x ?? "", y ?? "", !string.Equals((x ?? "").Trim(), (y ?? "").Trim(), StringComparison.OrdinalIgnoreCase)));
        var ca = a.Contacts.FirstOrDefault(c => c.Id == a.Lead.PrimaryContactId); var cb = b.Contacts.FirstOrDefault(c => c.Id == b.Lead.PrimaryContactId);
        Row("Company", a.Business.Name, b.Business.Name); Row("Website", a.Business.Website, b.Business.Website); Row("Phone", a.Business.Phone, b.Business.Phone); Row("E-mail", a.Business.Email, b.Business.Email);
        Row("Address", a.Business.Address, b.Business.Address); Row("City", a.Business.City, b.Business.City); Row("Industry", a.Business.Industry, b.Business.Industry);
        Row("Contact person", ca?.FullName, cb?.FullName); Row("Source", a.Lead.SourceName, b.Lead.SourceName); Row("Status", a.Lead.Status.ToString(), b.Lead.Status.ToString());
        Row("Score", a.Lead.Score.ToString(), b.Lead.Score.ToString()); Row("Created", a.Lead.CreatedAt.ToLocalTime().ToString("dd MMM yyyy"), b.Lead.CreatedAt.ToLocalTime().ToString("dd MMM yyyy"));
        Row("Activities", a.Activities.Count.ToString(), b.Activities.Count.ToString());
    }

    private Task MergeAsync(bool keepA) => RunAsync(async () =>
    {
        if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to merge leads.");
        if (Selected == null) return;
        var v = Selected.View;
        var (keep, drop) = keepA ? (v.A, v.B) : (v.B, v.A);
        var warn = v.Match.Confidence == DuplicateConfidence.High ? "" : "\n\nThis match is NOT high confidence.";
        if (!_dlg.Confirm("Merge leads", $"Keep {keep.LeadCode} ({keep.CompanyName}) and merge {drop.LeadCode} into it?\nEmpty fields are filled from {drop.LeadCode}; nothing is overwritten and you can undo the merge later.{warn}")) return;
        await Task.Run(() => _dups.Merge(keep.Id, drop.Id));
        StatusMessage = $"{drop.LeadCode} merged into {keep.LeadCode}. You can undo this in the history below.";
        await RefreshInner();
    });

    private async Task RefreshInner()
    {
        var (pending, history) = await Task.Run(() => (_dups.Pending(), _dups.History()));
        Pending.Clear(); foreach (var v in pending) Pending.Add(new DuplicateItem(v, $"{v.A.LeadCode} {v.A.CompanyName}  ↔  {v.B.LeadCode} {v.B.CompanyName}", $"{v.Match.Confidence} · {v.Match.Score:P0}", v.Match.Reasons));
        History.Clear(); foreach (var h in history) History.Add(new MergeHistoryRow(h.Id, h.MergedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"), $"lead #{h.SurvivorLeadId}", $"lead #{h.MergedLeadId}", h.UndoneAt == null ? "Merged" : "Undone", h.UndoneAt == null));
        Selected = Pending.FirstOrDefault(); OnPropertyChanged(nameof(HasPending));
    }

    [RelayCommand] private Task KeepA() => MergeAsync(true);
    [RelayCommand] private Task KeepB() => MergeAsync(false);

    [RelayCommand]
    private Task NotDuplicates() => RunAsync(async () =>
    {
        if (Selected == null) return;
        await Task.Run(() => _dups.MarkNotDuplicate(Selected.View.Match.Id));
        await RefreshInner();
    });

    [RelayCommand]
    private Task Undo(MergeHistoryRow? row) => RunAsync(async () =>
    {
        if (row == null || !row.CanUndo) return;
        if (!_dlg.Confirm("Undo merge", "Restore both leads to how they were before the merge?")) return;
        await Task.Run(() => _dups.Undo(row.Id));
        StatusMessage = "Merge undone – both leads are back, and the pair is pending again.";
        await RefreshInner();
    });

    [RelayCommand] private void OpenA() { if (Selected != null) _nav.Navigate(PageKey.LeadDetails, Selected.View.A.Id); }
    [RelayCommand] private void OpenB() { if (Selected != null) _nav.Navigate(PageKey.LeadDetails, Selected.View.B.Id); }
}
