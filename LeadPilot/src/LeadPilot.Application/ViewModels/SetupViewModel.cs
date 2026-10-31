using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Services;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

/// <summary>First-run wizard: welcome, profile + administrator, sources and privacy choices.</summary>
public sealed partial class SetupViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly UserService _users;
    private readonly ILicenseService _license;
    private readonly INavigator _nav;

    private static readonly string[] RegionList = new[]
    {
        "India (IN)", "United States (US)", "United Kingdom (GB)", "United Arab Emirates (AE)", "Australia (AU)", "Canada (CA)", "Singapore (SG)",
        "Germany (DE)", "Saudi Arabia (SA)", "South Africa (ZA)", "New Zealand (NZ)", "Malaysia (MY)"
    };

    /// <summary>Selectable countries, e.g. "India (IN)".</summary>
    public IReadOnlyList<string> Regions => RegionList;
    internal static IReadOnlyList<string> AllRegions => RegionList;

    [ObservableProperty] private int _step;
    [ObservableProperty] private string _companyName = "";
    [ObservableProperty] private string _ownerName = "";
    [ObservableProperty] private string _contactEmail = "";
    [ObservableProperty] private string _region = RegionList[0];
    [ObservableProperty] private string _pin = "";
    [ObservableProperty] private string _pinConfirm = "";
    [ObservableProperty] private bool _useOpenStreetMap = true;
    [ObservableProperty] private bool _visitWebsites = true;

    public SetupViewModel(ISettingsService settings, UserService users, ILicenseService license, INavigator nav)
    { _settings = settings; _users = users; _license = license; _nav = nav; }

    public bool IsStep0 => Step == 0;
    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool CanGoBack => Step > 0;
    public bool IsLastStep => Step == 2;
    public string TrialText => _license.Current.Message;

    partial void OnStepChanged(int value)
    {
        foreach (var n in new[] { nameof(IsStep0), nameof(IsStep1), nameof(IsStep2), nameof(CanGoBack), nameof(IsLastStep) }) OnPropertyChanged(n);
    }

    [RelayCommand]
    private void Back() { ErrorMessage = ""; if (Step > 0) Step--; }

    [RelayCommand]
    private void Next()
    {
        ErrorMessage = "";
        if (Step == 1)
        {
            if (string.IsNullOrWhiteSpace(OwnerName)) { ErrorMessage = "Enter your name."; return; }
            if (!string.IsNullOrEmpty(Pin) && Pin.Length < 4) { ErrorMessage = "A PIN must be at least 4 characters (or leave it empty for no PIN)."; return; }
            if (Pin != PinConfirm) { ErrorMessage = "The two PIN entries do not match."; return; }
            if (!string.IsNullOrWhiteSpace(ContactEmail) && !Text.Normalizer.IsValidEmailFormat(ContactEmail)) { ErrorMessage = "The e-mail address does not look valid."; return; }
        }
        if (Step < 2) Step++;
    }

    [RelayCommand]
    private void Finish()
    {
        Next();
        if (HasError) return;
        var code = Region[(Region.LastIndexOf('(') + 1)..].TrimEnd(')');
        var s = _settings.Current;
        s.CompanyName = CompanyName.Trim(); s.OwnerName = OwnerName.Trim(); s.DefaultRegion = code;
        s.ContactEmail = string.IsNullOrWhiteSpace(ContactEmail) ? null : ContactEmail.Trim();
        s.VisitWebsitesDuringDiscovery = VisitWebsites;
        s.EnabledProviders = UseOpenStreetMap ? new List<string> { "osm-overpass" } : new List<string>();
        s.SetupCompleted = true;
        var admin = _users.Users().FirstOrDefault(u => u.Role == UserRole.Admin) ?? _users.Create(OwnerName.Trim(), s.ContactEmail, UserRole.Admin, string.IsNullOrEmpty(Pin) ? null : Pin);
        _settings.Save(s);
        _users.SignIn(admin, string.IsNullOrEmpty(Pin) ? null : Pin);
        _nav.Navigate(PageKey.Dashboard);
    }
}
