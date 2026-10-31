using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Services;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

public sealed record UserRow(int Id, string Name, string Role, string Pin, string Email);
public sealed record AuditRow(string When, string User, string Action, string Details);

/// <summary>Profile, privacy, retention, users with roles, audit log and destructive data operations.</summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly ISystemRepository _sys;
    private readonly UserService _users;
    private readonly DataGovernanceService _gov;
    private readonly ICurrentUser _user;
    private readonly IDialogService _dlg;
    private readonly IAppFolders _folders;

    [ObservableProperty] private string _companyName = "";
    [ObservableProperty] private string _ownerName = "";
    [ObservableProperty] private string _contactEmail = "";
    [ObservableProperty] private string _region = SetupViewModel.Regions[0];
    [ObservableProperty] private bool _visitWebsites = true;
    [ObservableProperty] private bool _isDark;
    [ObservableProperty] private int _retentionDays;
    [ObservableProperty] private string _newUserName = "";
    [ObservableProperty] private string _newUserRole = nameof(UserRole.Sales);
    [ObservableProperty] private string _newUserPin = "";

    public IReadOnlyList<string> Regions => SetupViewModel.Regions;
    public IReadOnlyList<string> Roles { get; } = Enum.GetNames<UserRole>();
    public ObservableCollection<UserRow> Users { get; } = new();
    public ObservableCollection<AuditRow> Audit { get; } = new();
    public string DataFolder => _folders.Root;

    public SettingsViewModel(ISettingsService settings, ISystemRepository sys, UserService users, DataGovernanceService gov, ICurrentUser user, IDialogService dlg, IAppFolders folders)
    { _settings = settings; _sys = sys; _users = users; _gov = gov; _user = user; _dlg = dlg; _folders = folders; }

    public override Task OnNavigatedToAsync(object? parameter) => RunAsync(async () =>
    {
        var s = _settings.Current;
        CompanyName = s.CompanyName; OwnerName = s.OwnerName; ContactEmail = s.ContactEmail ?? ""; VisitWebsites = s.VisitWebsitesDuringDiscovery; IsDark = s.Theme == "Dark"; RetentionDays = s.RetentionDays;
        Region = SetupViewModel.Regions.FirstOrDefault(r => r.EndsWith($"({s.DefaultRegion})")) ?? SetupViewModel.Regions[0];
        await LoadListsAsync();
    });

    private async Task LoadListsAsync()
    {
        var (users, audit) = await Task.Run(() => (_sys.GetUsers(), _sys.GetAudit(100)));
        Users.Clear(); foreach (var u in users.Where(x => x.IsActive)) Users.Add(new UserRow(u.Id, u.Name, u.Role.ToString(), u.PinHash == null ? "none" : "set", u.Email ?? ""));
        var names = users.ToDictionary(u => u.Id, u => u.Name);
        Audit.Clear(); foreach (var a in audit) Audit.Add(new AuditRow(a.At.ToLocalTime().ToString("dd MMM yyyy HH:mm"), a.UserId != null && names.TryGetValue(a.UserId.Value, out var n) ? n : "", a.Action, a.Details ?? a.EntityId ?? ""));
    }

    private void Require() { if (!_user.Can(Permission.ManageSettings)) throw new UnauthorizedAccessException("Only an administrator can change settings."); }

    [RelayCommand]
    private Task Save() => RunAsync(() =>
    {
        Require();
        if (!string.IsNullOrWhiteSpace(ContactEmail) && !Text.Normalizer.IsValidEmailFormat(ContactEmail)) { ErrorMessage = "The e-mail address does not look valid."; return Task.CompletedTask; }
        if (RetentionDays < 0 || RetentionDays > 3650) { ErrorMessage = "Retention must be between 0 (keep forever) and 3650 days."; return Task.CompletedTask; }
        var s = _settings.Current;
        s.CompanyName = CompanyName.Trim(); s.OwnerName = OwnerName.Trim(); s.ContactEmail = string.IsNullOrWhiteSpace(ContactEmail) ? null : ContactEmail.Trim();
        s.DefaultRegion = Region[(Region.LastIndexOf('(') + 1)..].TrimEnd(')'); s.VisitWebsitesDuringDiscovery = VisitWebsites; s.RetentionDays = RetentionDays;
        s.Theme = IsDark ? "Dark" : "Light"; _dlg.SetTheme(s.Theme);
        _settings.Save(s); StatusMessage = "Settings saved.";
        return Task.CompletedTask;
    });

    [RelayCommand]
    private Task AddUser() => RunAsync(async () =>
    {
        Require();
        if (string.IsNullOrWhiteSpace(NewUserName)) { ErrorMessage = "Enter the user's name."; return; }
        _users.Create(NewUserName, null, Enum.Parse<UserRole>(NewUserRole), string.IsNullOrEmpty(NewUserPin) ? null : NewUserPin);
        NewUserName = ""; NewUserPin = ""; StatusMessage = "User added.";
        await LoadListsAsync();
    });

    [RelayCommand]
    private Task RemoveUser(UserRow? row) => RunAsync(async () =>
    {
        Require();
        if (row == null) return;
        if (row.Id == _user.UserId) { ErrorMessage = "You cannot remove the user you are signed in as."; return; }
        var u = _sys.GetUser(row.Id); if (u == null) return;
        if (u.Role == UserRole.Admin && Users.Count(x => x.Role == nameof(UserRole.Admin)) <= 1) { ErrorMessage = "There must be at least one administrator."; return; }
        u.IsActive = false; _sys.UpdateUser(u);
        await LoadListsAsync();
    });

    [RelayCommand]
    private Task ApplyRetention() => RunAsync(async () =>
    {
        Require();
        if (RetentionDays <= 0) { ErrorMessage = "Set a number of days above 0 first (and save)."; return; }
        if (!_dlg.Confirm("Apply retention now", $"Permanently delete Lost, Unqualified and archived leads not updated in the last {RetentionDays} days?")) return;
        var s = _settings.Current; s.RetentionDays = RetentionDays; _settings.Save(s);
        var n = await Task.Run(() => _gov.ApplyRetention());
        StatusMessage = $"{n} lead(s) removed."; await LoadListsAsync();
    });

    [RelayCommand]
    private Task DeleteAll() => RunAsync(async () =>
    {
        Require();
        var typed = _dlg.PromptText("Delete ALL data", "This permanently deletes every lead, contact, task, campaign and import record on this computer (users and settings are kept). Make a backup first if unsure.\n\nType DELETE to confirm:");
        if (typed?.Trim() != "DELETE") return;
        await Task.Run(() => _gov.DeleteAllData());
        StatusMessage = "All lead data was deleted."; await LoadListsAsync();
    });

    [RelayCommand] private void OpenDataFolder() => _dlg.OpenUrl(_folders.Root);
}
