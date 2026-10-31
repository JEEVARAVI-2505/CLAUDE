using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Services;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

/// <summary>Application shell: sidebar navigation, current page, user switcher and licence banner.</summary>
public sealed partial class MainViewModel : ObservableObject, INavigator
{
    private readonly Func<PageKey, ViewModelBase> _factory;
    private readonly ISettingsService _settings;
    private readonly ILicenseService _license;
    private readonly UserService _users;
    private readonly IDialogService _dialogs;
    private readonly FollowUpService _followUps;
    private readonly Stack<PageKey> _history = new();

    [ObservableProperty] private ViewModelBase? _currentPage;
    [ObservableProperty] private NavItem? _selectedNav;
    [ObservableProperty] private bool _isShellVisible;
    [ObservableProperty] private string _licenseBanner = "";
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _userRole = "";
    [ObservableProperty] private bool _isDark;
    [ObservableProperty] private string _overdueBanner = "";

    public ObservableCollection<NavItem> NavItems { get; } = new();

    public MainViewModel(Func<PageKey, ViewModelBase> factory, ISettingsService settings, ILicenseService license, UserService users, IDialogService dialogs, FollowUpService followUps)
    {
        _factory = factory; _settings = settings; _license = license; _users = users; _dialogs = dialogs; _followUps = followUps;
        _users.Changed += (_, _) => RefreshChrome();
        IsDark = settings.Current.Theme == "Dark";
    }

    private static readonly NavItem[] AllItems =
    {
        new(PageKey.Dashboard, "Dashboard", "", "Overview"),
        new(PageKey.LeadFinder, "AI Lead Finder", "", "Find"),
        new(PageKey.Import, "Import Centre", "", "Find"),
        new(PageKey.LeadDatabase, "Lead Database", "", "Manage"),
        new(PageKey.Duplicates, "Duplicate Manager", "", "Manage"),
        new(PageKey.Enrichment, "AI Enrichment", "", "Manage"),
        new(PageKey.Qualification, "Lead Qualification", "", "Manage"),
        new(PageKey.FollowUps, "Follow-up Manager", "", "Sell"),
        new(PageKey.Campaigns, "Campaign Manager", "", "Sell"),
        new(PageKey.Assistant, "AI Assistant", "", "Sell"),
        new(PageKey.Analytics, "Analytics", "", "Insights"),
        new(PageKey.Export, "Export Centre", "", "Insights"),
        new(PageKey.Integrations, "API Integrations", "", "System"),
        new(PageKey.Backup, "Backup & Restore", "", "System"),
        new(PageKey.Settings, "Settings", "", "System"),
        new(PageKey.About, "About & Licensing", "", "System")
    };

    /// <summary>Called once at startup. Shows Setup until the first-run wizard is completed.</summary>
    public async Task StartAsync()
    {
        if (!_settings.Current.SetupCompleted || _users.User == null) { await ShowAsync(PageKey.Setup, null); return; }
        RefreshChrome();
        await ShowAsync(PageKey.Dashboard, null);
    }

    public void Navigate(PageKey key, object? parameter = null) => _ = ShowAsync(key, parameter);

    private async Task ShowAsync(PageKey key, object? parameter)
    {
        if (key != PageKey.Setup && (!_settings.Current.SetupCompleted || _users.User == null)) key = PageKey.Setup;
        IsShellVisible = key != PageKey.Setup;
        if (key != PageKey.Setup) RefreshChrome();
        if (key != PageKey.Setup) { var n = await Task.Run(() => _followUps.Overdue().Count); OverdueBanner = n == 0 ? "" : $"{n} follow-up(s) overdue"; }
        var page = _factory(key);
        CurrentPage = page;
        SelectedNav = NavItems.FirstOrDefault(n => n.Key == key);
        _history.Push(key);
        await page.OnNavigatedToAsync(parameter);
    }

    partial void OnSelectedNavChanged(NavItem? value)
    {
        if (value != null && (CurrentPage == null || PageKeyOf(CurrentPage) != value.Key)) Navigate(value.Key);
    }

    private static PageKey? PageKeyOf(ViewModelBase vm) => vm switch
    {
        DashboardViewModel => (PageKey?)PageKey.Dashboard, LeadFinderViewModel => PageKey.LeadFinder, LeadDatabaseViewModel => PageKey.LeadDatabase, LeadDetailsViewModel => PageKey.LeadDetails,
        DuplicateManagerViewModel => PageKey.Duplicates, EnrichmentViewModel => PageKey.Enrichment, QualificationViewModel => PageKey.Qualification, FollowUpViewModel => PageKey.FollowUps,
        CampaignViewModel => PageKey.Campaigns, AnalyticsViewModel => PageKey.Analytics, AssistantViewModel => PageKey.Assistant, ImportViewModel => PageKey.Import,
        ExportViewModel => PageKey.Export, IntegrationsViewModel => PageKey.Integrations, SettingsViewModel => PageKey.Settings, BackupViewModel => PageKey.Backup,
        AboutViewModel => PageKey.About, SetupViewModel => PageKey.Setup, _ => null
    };

    private void RefreshChrome()
    {
        var lic = _license.Current;
        LicenseBanner = lic.IsValid ? (lic.IsTrial ? lic.Message : "") : lic.Message;
        UserName = _users.User?.Name ?? ""; UserRole = _users.User?.Role.ToString() ?? "";
        var visible = AllItems.Where(i => i.Key switch
        {
            PageKey.Settings => _users.Can(Permission.ManageSettings),
            PageKey.Backup => _users.Can(Permission.ManageBackups),
            PageKey.Integrations => _users.Can(Permission.ManageIntegrations),
            PageKey.Export => _users.Can(Permission.Export),
            _ => true
        }).ToList();
        if (NavItems.Select(n => n.Key).SequenceEqual(visible.Select(v => v.Key))) return;
        NavItems.Clear(); foreach (var v in visible) NavItems.Add(v);
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDark = !IsDark;
        var s = _settings.Current; s.Theme = IsDark ? "Dark" : "Light"; _settings.Save(s);
        _dialogs.SetTheme(s.Theme);
    }

    [RelayCommand]
    private async Task SwitchUser()
    {
        var candidates = _users.Users().Where(u => u.Id != _users.UserId).ToList();
        if (candidates.Count == 0) { _dialogs.Info("Switch user", "There is only one user. Add more users in Settings."); return; }
        var names = string.Join(", ", candidates.Select(c => c.Name));
        var typed = _dialogs.PromptText("Switch user", $"Type the name of the user to sign in as ({names}):");
        var u = candidates.FirstOrDefault(c => string.Equals(c.Name, typed?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (u == null) return;
        string? pin = null;
        if (u.PinHash != null) { pin = _dialogs.PromptPassword("PIN required", $"Enter the PIN for {u.Name}:", false); if (pin == null) return; }
        if (!_users.SignIn(u, pin)) { _dialogs.Error("Sign-in failed", "The PIN is not correct."); return; }
        await ShowAsync(PageKey.Dashboard, null);
    }
}
