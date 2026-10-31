using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

public sealed record EvidenceRow(string Field, string Value, string Source, string Url, string When, string Confidence, string Applied, string Note);
public sealed record TimelineRow(string When, string Type, string Subject, string Body);
public sealed record TaskItem(int Id, string Title, string Due, string Status, bool IsOpen, bool IsOverdue);
public sealed record LeadSourceItem(string Name, string Type, string Url, string When, string License);

/// <summary>Everything about one lead: edit, verify, enrich, rescore, log interactions, schedule follow-ups and draft outreach.</summary>
public sealed partial class LeadDetailsViewModel : ViewModelBase
{
    private readonly ILeadRepository _leads;
    private readonly ISystemRepository _sys;
    private readonly ScoringService _scoring;
    private readonly VerificationService _verify;
    private readonly EnrichmentService _enrich;
    private readonly FollowUpService _follow;
    private readonly OutreachDrafter _drafter;
    private readonly ISettingsService _settings;
    private readonly ICurrentUser _user;
    private readonly INavigator _nav;
    private readonly IDialogService _dlg;
    private readonly IClock _clock;
    private LeadDetail? _detail;

    [ObservableProperty] private string _leadCode = "";
    [ObservableProperty] private string _companyName = "";
    [ObservableProperty] private string _website = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _city = "";
    [ObservableProperty] private string _industry = "";
    [ObservableProperty] private string _services = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _contactName = "";
    [ObservableProperty] private string _contactTitle = "";
    [ObservableProperty] private string _contactPhone = "";
    [ObservableProperty] private string _contactEmail = "";
    [ObservableProperty] private string _intentEvidence = "";
    [ObservableProperty] private string _status = nameof(LeadStatus.New);
    [ObservableProperty] private SalesUser? _assignedUser;
    [ObservableProperty] private int _score;
    [ObservableProperty] private string _temperature = "";
    [ObservableProperty] private string _scoreExplanation = "";
    [ObservableProperty] private string _emailCheck = "";
    [ObservableProperty] private string _phoneCheck = "";
    [ObservableProperty] private string _websiteCheck = "";
    [ObservableProperty] private string _contactPageUrl = "";
    [ObservableProperty] private string _offer = "";
    // interaction + follow-up forms
    [ObservableProperty] private string _interactionType = nameof(ActivityType.Call);
    [ObservableProperty] private string _interactionSubject = "";
    [ObservableProperty] private string _interactionNotes = "";
    [ObservableProperty] private string _taskTitle = "";
    [ObservableProperty] private DateTime _taskDue = DateTime.Today.AddDays(2);
    [ObservableProperty] private string _draftSubject = "";
    [ObservableProperty] private string _draftBody = "";
    [ObservableProperty] private string _draftChannel = nameof(OutreachChannel.Email);

    public ObservableCollection<TimelineRow> Timeline { get; } = new();
    public ObservableCollection<EvidenceRow> Evidence { get; } = new();
    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<LeadSourceItem> Sources { get; } = new();
    public ObservableCollection<SalesUser> Users { get; } = new();
    public ObservableCollection<string> Suggestions { get; } = new();
    public IReadOnlyList<string> StatusChoices { get; } = Enum.GetNames<LeadStatus>();
    public IReadOnlyList<string> InteractionTypes { get; } = new[] { nameof(ActivityType.Call), nameof(ActivityType.Email), nameof(ActivityType.WhatsApp), nameof(ActivityType.Meeting), nameof(ActivityType.Note) };
    public IReadOnlyList<string> Channels { get; } = Enum.GetNames<OutreachChannel>();
    public int LeadId => _detail?.Lead.Id ?? 0;
    public bool HasLead => _detail != null;
    public bool HasDraft => !string.IsNullOrEmpty(DraftBody);

    public LeadDetailsViewModel(ILeadRepository leads, ISystemRepository sys, ScoringService scoring, VerificationService verify, EnrichmentService enrich, FollowUpService follow, OutreachDrafter drafter, ISettingsService settings, ICurrentUser user, INavigator nav, IDialogService dlg, IClock clock)
    { _leads = leads; _sys = sys; _scoring = scoring; _verify = verify; _enrich = enrich; _follow = follow; _drafter = drafter; _settings = settings; _user = user; _nav = nav; _dlg = dlg; _clock = clock; }

    public override Task OnNavigatedToAsync(object? parameter) => parameter is int id ? LoadAsync(id) : Task.CompletedTask;

    public Task LoadAsync(int leadId) => RunAsync(async () =>
    {
        var d = await Task.Run(() => _leads.GetDetail(leadId)) ?? throw new InvalidOperationException("That lead no longer exists.");
        var users = await Task.Run(() => _sys.GetUsers().Where(u => u.IsActive).ToList());
        Users.Clear(); foreach (var u in users) Users.Add(u);
        Apply(d);
    });

    private void Apply(LeadDetail d)
    {
        _detail = d;
        var b = d.Business; var l = d.Lead; var c = d.Contacts.FirstOrDefault(x => x.Id == l.PrimaryContactId);
        LeadCode = l.LeadCode; CompanyName = b.Name; Website = b.Website ?? ""; Phone = b.Phone ?? ""; Email = b.Email ?? ""; Address = b.Address ?? ""; City = b.City ?? "";
        Industry = b.Industry ?? ""; Services = b.Services ?? ""; Description = b.Description ?? ""; ContactPageUrl = b.ContactPageUrl ?? "";
        ContactName = c?.FullName ?? ""; ContactTitle = c?.JobTitle ?? ""; ContactPhone = c?.Phone ?? ""; ContactEmail = c?.Email ?? "";
        IntentEvidence = l.IntentEvidence ?? ""; Status = l.Status.ToString(); AssignedUser = Users.FirstOrDefault(u => u.Id == l.AssignedUserId);
        Score = l.Score; Temperature = l.Temperature.ToString(); ScoreExplanation = l.ScoreExplanation ?? "Not scored yet.";
        EmailCheck = Describe(l.EmailStatus); PhoneCheck = Describe(l.PhoneStatus); WebsiteCheck = Describe(l.WebsiteStatus);
        Offer = string.IsNullOrWhiteSpace(Offer) ? _scoring.ProfileFor(l).Product : Offer;
        Timeline.Clear(); foreach (var a in d.Activities) Timeline.Add(new TimelineRow(Local(a.CreatedAt), a.Type.ToString(), a.Subject, a.Body ?? ""));
        Evidence.Clear(); foreach (var e in d.Evidence) Evidence.Add(new EvidenceRow(e.FieldName, e.Value, e.SourceName, e.SourceUrl ?? "", Local(e.ExtractedAt), $"{e.Confidence:P0}", e.Applied ? "Applied" : "Suggestion only", e.Note ?? ""));
        Sources.Clear(); foreach (var s in d.Sources) Sources.Add(new LeadSourceItem(s.SourceName, s.SourceType, s.SourceUrl ?? "", Local(s.RetrievedAt), s.License ?? ""));
        Tasks.Clear(); foreach (var t in d.Tasks) Tasks.Add(new TaskItem(t.Id, t.Title, Local(t.DueAt), t.Status.ToString(), t.Status == FollowUpStatus.Open, t.Status == FollowUpStatus.Open && t.DueAt < _clock.UtcNow));
        Suggestions.Clear();
        var row = _leads.Export(new LeadQuery { Text = l.LeadCode, IncludeArchived = true }).FirstOrDefault(r => r.Id == l.Id);
        if (row != null) foreach (var s in _follow.Suggest(row, d.Tasks.Any(t => t.Status == FollowUpStatus.Open))) Suggestions.Add($"{s.Title} – {s.Reason}");
        OnPropertyChanged(nameof(LeadId)); OnPropertyChanged(nameof(HasLead));
    }

    private static string Local(DateTime utc) => utc.ToLocalTime().ToString("dd MMM yyyy HH:mm");

    private static string Describe(EmailStatus s) => s switch
    {
        EmailStatus.Unverified => "Not checked", EmailStatus.InvalidFormat => "Invalid format", EmailStatus.ValidFormat => "Valid format (mailbox not checked)", EmailStatus.DomainExists => "Domain exists",
        EmailStatus.MailServerFound => "Domain has a mail server (mailbox not confirmed)", _ => "Verified by provider"
    };
    private static string Describe(PhoneStatus s) => s switch
    {
        PhoneStatus.Unverified => "Not checked", PhoneStatus.InvalidFormat => "Invalid number", PhoneStatus.Normalized => "Normalised", PhoneStatus.CountryIdentified => "Valid number, country identified (owner not verified)",
        PhoneStatus.PubliclyListed => "Publicly listed (owner not verified)", _ => "Verified by provider"
    };
    private static string Describe(WebsiteStatus s) => s switch
    {
        WebsiteStatus.Unverified => "Not checked", WebsiteStatus.Accessible => "Accessible", WebsiteStatus.Unavailable => "Unavailable", WebsiteStatus.Redirected => "Redirects within the same site", _ => "Redirects to a different domain"
    };

    private void RequireEdit() { if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to edit leads."); }

    [RelayCommand]
    private Task Save() => RunAsync(async () =>
    {
        RequireEdit();
        if (_detail == null) return;
        if (string.IsNullOrWhiteSpace(CompanyName)) { ErrorMessage = "Company name is required."; return; }
        var region = _settings.Current.DefaultRegion;
        var d = _detail; var b = d.Business; var l = d.Lead;
        var phoneChanged = (b.Phone ?? "") != Phone.Trim(); var emailChanged = (b.Email ?? "") != Email.Trim();
        b.Name = CompanyName.Trim(); b.Website = Normalizer.NormalizeUrl(Website) ?? N(Website); b.Domain = Normalizer.DomainOf(b.Website);
        b.Phone = N(Phone); b.NormalizedPhone = Normalizer.NormalizePhone(Phone, region); b.Email = N(Email); b.NormalizedEmail = Normalizer.NormalizeEmail(Email);
        b.Address = N(Address); b.City = N(City); b.Industry = N(Industry); b.Services = N(Services); b.Description = N(Description);
        l.Status = Enum.Parse<LeadStatus>(Status); l.AssignedUserId = AssignedUser?.Id; l.IntentEvidence = N(IntentEvidence);
        if (phoneChanged && l.PhoneStatus != PhoneStatus.VerifiedByProvider) l.PhoneStatus = string.IsNullOrWhiteSpace(Phone) ? PhoneStatus.Unverified : b.NormalizedPhone != null ? PhoneStatus.CountryIdentified : PhoneStatus.InvalidFormat;
        if (emailChanged && l.EmailStatus != EmailStatus.VerifiedByProvider) l.EmailStatus = string.IsNullOrWhiteSpace(Email) ? EmailStatus.Unverified : b.NormalizedEmail != null ? EmailStatus.ValidFormat : EmailStatus.InvalidFormat;

        await Task.Run(() =>
        {
            _leads.UpdateBusiness(b);
            if (!string.IsNullOrWhiteSpace(ContactName))
            {
                var c = d.Contacts.FirstOrDefault(x => x.Id == l.PrimaryContactId);
                if (c == null) { c = new Contact { BusinessId = b.Id, FullName = ContactName.Trim(), IsPrimary = true }; Fill(c, region); c.Id = _leads.AddContact(c); l.PrimaryContactId = c.Id; }
                else { c.FullName = ContactName.Trim(); Fill(c, region); _leads.UpdateContact(c); }
            }
            _leads.UpdateLead(l);
            _scoring.Rescore(l.Id);
        });
        _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = _user.UserId, Action = "lead.update", Entity = "Lead", EntityId = l.LeadCode });
        StatusMessage = "Saved.";
        await LoadAsyncSafe(l.Id);
    });

    private void Fill(Contact c, string region)
    {
        c.JobTitle = N(ContactTitle); c.Phone = N(ContactPhone); c.NormalizedPhone = Normalizer.NormalizePhone(ContactPhone, region); c.Email = N(ContactEmail); c.NormalizedEmail = Normalizer.NormalizeEmail(ContactEmail);
    }

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    [RelayCommand]
    private Task Rescore() => RunAsync(async () =>
    {
        if (_detail == null) return;
        await Task.Run(() => _scoring.Rescore(_detail.Lead.Id));
        await LoadAsyncSafe(_detail.Lead.Id); StatusMessage = "Score recalculated.";
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken ct)
    {
        if (_detail == null) return;
        var id = _detail.Lead.Id;
        await RunAsync(async () =>
        {
            RequireEdit();
            var r = await _verify.VerifyLeadAsync(id, true, ct);
            await LoadAsyncSafe(id);
            StatusMessage = r == null ? "" : string.Join("  ", r.Notes);
        }, "Checking contact details…");
    }

    private async Task LoadAsyncSafe(int id) { var d = await Task.Run(() => _leads.GetDetail(id)); if (d != null) Apply(d); }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EnrichAsync(CancellationToken ct)
    {
        if (_detail == null) return;
        var id = _detail.Lead.Id; EnrichmentResult? res = null;
        await RunAsync(async () => { RequireEdit(); res = await _enrich.EnrichAsync(id, ct); }, "Reading the company website…");
        await LoadAsyncSafe(id);
        if (res != null) StatusMessage = res.Summary + (res.Warnings.Count > 0 && res.Applied.Count > 0 ? "  Note: " + res.Warnings[0] : "");
    }

    [RelayCommand]
    private Task LogInteraction() => RunAsync(async () =>
    {
        RequireEdit();
        if (_detail == null) return;
        await Task.Run(() => _follow.LogInteraction(_detail.Lead.Id, Enum.Parse<ActivityType>(InteractionType), InteractionSubject, N(InteractionNotes)));
        InteractionSubject = ""; InteractionNotes = "";
        await ReloadAfterAction();
    });

    [RelayCommand]
    private Task ScheduleFollowUp() => RunAsync(async () =>
    {
        RequireEdit();
        if (_detail == null) return;
        var due = DateTime.SpecifyKind(TaskDue.Date.AddHours(9), DateTimeKind.Local).ToUniversalTime();
        await Task.Run(() => _follow.Schedule(_detail.Lead.Id, TaskTitle, due));
        TaskTitle = ""; StatusMessage = "Follow-up scheduled.";
        await ReloadAfterAction();
    });

    [RelayCommand]
    private Task CompleteTask(TaskItem? t) => RunAsync(async () =>
    {
        if (t == null) return;
        await Task.Run(() => _follow.Complete(t.Id));
        await ReloadAfterAction();
    });

    private Task ReloadAfterAction() => _detail == null ? Task.CompletedTask : LoadAsyncSafe(_detail.Lead.Id);

    [RelayCommand]
    private Task Draft() => RunAsync(async () =>
    {
        if (_detail == null) return;
        var ch = Enum.Parse<OutreachChannel>(DraftChannel);
        var me = new SenderProfile { SenderName = _settings.Current.OwnerName, SenderCompany = _settings.Current.CompanyName, Offer = Offer };
        var d = await _drafter.DraftAsync(_detail, me, ch, true);
        DraftSubject = d.Subject; DraftBody = d.Body;
        OnPropertyChanged(nameof(HasDraft));
        StatusMessage = (d.UsedAi ? "Draft polished by your AI model. " : "") + "Nothing has been sent. Review it, then open it in your own mail app or WhatsApp.";
        await ReloadAfterAction();
    });

    [RelayCommand]
    private void OpenInMailApp()
    {
        var to = N(ContactEmail) ?? N(Email);
        if (to == null) { ErrorMessage = "This lead has no e-mail address."; return; }
        _dlg.OpenUrl(OutreachDrafter.MailTo(to, DraftSubject, DraftBody));
    }

    [RelayCommand]
    private void OpenInWhatsApp()
    {
        var number = Normalizer.NormalizePhone(N(ContactPhone) ?? N(Phone), _settings.Current.DefaultRegion);
        var link = OutreachDrafter.WhatsAppLink(number, DraftBody);
        if (link == null) { ErrorMessage = "This lead has no valid phone number."; return; }
        _dlg.OpenUrl(link);
    }

    [RelayCommand] private void CopyDraft() => _dlg.CopyToClipboard(string.IsNullOrEmpty(DraftSubject) ? DraftBody : DraftSubject + "\n\n" + DraftBody);
    [RelayCommand] private void OpenWebsite() { if (Normalizer.NormalizeUrl(Website) is { } u) _dlg.OpenUrl(u); }
    [RelayCommand] private void OpenUrl(string? url) { if (!string.IsNullOrWhiteSpace(url) && Normalizer.NormalizeUrl(url) is { } u) _dlg.OpenUrl(u); }
    [RelayCommand] private void Back() => _nav.Navigate(PageKey.LeadDatabase);
}
