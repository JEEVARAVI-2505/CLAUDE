using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CRM_Master.App.ViewModels;

/// <summary>One sidebar entry.</summary>
public sealed partial class NavItem : ObservableObject
{
    public NavItem(Module module, string title, string iconKey)
    {
        Module = module; Title = title;
        Icon = (Geometry)Application.Current.FindResource("Icon." + iconKey);
    }
    public Module Module { get; }
    public string Title { get; }
    public Geometry Icon { get; }
    [ObservableProperty] private bool _isSelected;
}

/// <summary>Application shell: sidebar navigation, top bar status, notifications.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly IDbContextFactory<CrmDbContext> _factory;
    private readonly ThemeService _theme;
    private readonly ILicenseProvider _license;
    private readonly Dictionary<Module, ViewModelBase> _cache = new();

    public MainViewModel(IServiceProvider services, IDbContextFactory<CrmDbContext> factory, INotificationCenter notifications, IBusyService busy,
        INavigationService navigation, ISettingsService settings, ThemeService theme, ILicenseProvider license)
    {
        _services = services; _factory = factory; _theme = theme; _license = license;
        Notifications = notifications; Busy = busy; Settings = settings;
        NavItems = new ObservableCollection<NavItem>
        {
            new(Module.Dashboard, "Dashboard", "Dashboard"), new(Module.Import, "Import Data", "Import"), new(Module.Cleaning, "Data Cleaning", "Clean"),
            new(Module.Duplicates, "Duplicate Detection", "Duplicates"), new(Module.Contacts, "Contact Management", "Contacts"), new(Module.BulkUpdate, "Bulk Update", "Update"),
            new(Module.Validation, "Data Validation", "Validate"), new(Module.Automation, "Automation Rules", "Automation"), new(Module.Reports, "Reports & Analytics", "Reports"),
            new(Module.Activity, "Activity Logs", "Logs"), new(Module.Settings, "Settings", "Settings"), new(Module.About, "About", "About"),
        };
        navigation.NavigationRequested += m => _ = NavigateAsync(m);
        busy.PropertyChanged += (_, _) => { OnPropertyChanged(nameof(IsProcessing)); OnPropertyChanged(nameof(ProcessingText)); };
        UpdateLicense();
    }

    public ObservableCollection<NavItem> NavItems { get; }
    public INotificationCenter Notifications { get; }
    public IBusyService Busy { get; }
    public ISettingsService Settings { get; }

    [ObservableProperty] private ViewModelBase? _current;
    [ObservableProperty] private string _currentTitle = "Dashboard";
    [ObservableProperty] private bool _isSidebarCollapsed;
    [ObservableProperty] private bool _isDatabaseConnected = true;
    [ObservableProperty] private bool _isNotificationsOpen;
    [ObservableProperty] private string? _licenseBanner;
    [ObservableProperty] private bool _isLicenseExpired;

    public bool IsProcessing => Busy.IsBusy;
    public string ProcessingText => Busy.IsBusy ? Busy.StatusText : "Idle";
    public string DisplayName => Settings.Current.DisplayName;
    public string DatabaseStatusText => IsDatabaseConnected ? "Database connected" : "Database unavailable";
    public string ThemeToggleText => _theme.Current == ThemeService.Dark ? "Switch to light theme" : "Switch to dark theme";
    public string AppVersion => "v" + AppInfo.Version;

    partial void OnIsDatabaseConnectedChanged(bool value) => OnPropertyChanged(nameof(DatabaseStatusText));

    public void UpdateLicense()
    {
        var st = _license.GetStatus(DateTime.UtcNow);
        LicenseBanner = st.Mode == LicenseMode.Evaluation ? st.Message : null;
        IsLicenseExpired = st.IsExpired;
        OnPropertyChanged(nameof(DisplayName)); OnPropertyChanged(nameof(ThemeToggleText));
    }

    private ViewModelBase Resolve(Module m)
    {
        if (_cache.TryGetValue(m, out var vm)) return vm;
        vm = m switch
        {
            Module.Dashboard => _services.GetRequiredService<DashboardViewModel>(),
            Module.Import => _services.GetRequiredService<ImportViewModel>(),
            Module.Cleaning => _services.GetRequiredService<CleaningViewModel>(),
            Module.Duplicates => _services.GetRequiredService<DuplicatesViewModel>(),
            Module.Contacts => _services.GetRequiredService<ContactsViewModel>(),
            Module.BulkUpdate => _services.GetRequiredService<BulkUpdateViewModel>(),
            Module.Validation => _services.GetRequiredService<ValidationViewModel>(),
            Module.Automation => _services.GetRequiredService<AutomationViewModel>(),
            Module.Reports => _services.GetRequiredService<ReportsViewModel>(),
            Module.Activity => _services.GetRequiredService<ActivityLogViewModel>(),
            Module.Settings => _services.GetRequiredService<SettingsViewModel>(),
            _ => _services.GetRequiredService<AboutViewModel>(),
        };
        _cache[m] = vm; return vm;
    }

    [RelayCommand]
    public async Task NavigateAsync(object? target)
    {
        var module = target switch { Module m => m, NavItem n => n.Module, _ => Module.Dashboard };
        foreach (var n in NavItems) n.IsSelected = n.Module == module;
        CurrentTitle = NavItems.First(n => n.Module == module).Title;
        var vm = Resolve(module);
        Current = vm;
        await vm.ActivateAsync();
        await RefreshDatabaseStatusAsync();
        UpdateLicense();
    }

    public async Task RefreshDatabaseStatusAsync()
    {
        try { await using var ctx = await _factory.CreateDbContextAsync(); IsDatabaseConnected = await ctx.Database.CanConnectAsync(); }
        catch (Exception) { IsDatabaseConnected = false; }
    }

    [RelayCommand] private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;
    [RelayCommand] private void ToggleNotifications() { IsNotificationsOpen = !IsNotificationsOpen; if (IsNotificationsOpen) Notifications.MarkAllRead(); }
    [RelayCommand] private void ClearNotifications() => Notifications.Clear();
    [RelayCommand] private void ToggleTheme()
    {
        _theme.Toggle();
        var s = Settings.Current.Clone(); s.Theme = _theme.Current;
        _ = Settings.SaveAsync(s);
        OnPropertyChanged(nameof(ThemeToggleText));
    }
    [RelayCommand] private void ExitApp() => Application.Current.Shutdown();
}
