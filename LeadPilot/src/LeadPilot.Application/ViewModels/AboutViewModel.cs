using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;

namespace LeadPilot.Application.ViewModels;

/// <summary>Version, licence status and activation, privacy statement and third-party notices.</summary>
public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly ILicenseService _license;
    private readonly IDialogService _dlg;
    private readonly ICurrentUser _user;

    [ObservableProperty] private string _licenseKey = "";
    [ObservableProperty] private string _tier = "";
    [ObservableProperty] private string _holder = "";
    [ObservableProperty] private string _licenseMessage = "";
    [ObservableProperty] private string _limits = "";
    [ObservableProperty] private bool _isLicensed;

    public string Version { get; } = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version?.ToString(3) ?? "1.0.0";
    public string MachineId => _license.MachineId;
    public string Tiers => "Free trial: 14 days, up to 200 leads, 1 user.  Single-PC: 1 computer, 1 user.  Professional: up to 3 users, all features.  Business: up to 10 users, priority support.  Licences are validated offline; no internet connection or account is needed.";
    public string Privacy => "LeadPilot AI stores your data only on this computer, in a local database. It sends nothing to the vendor: no analytics, no telemetry, no lead uploads. " +
        "Network access happens only when you start a feature that needs it (search, website reading, optional AI/web-search with your own keys) – see API Integrations for the exact list. " +
        "You are responsible for using collected business contact data lawfully (e.g. anti-spam, telemarketing and data-protection rules in your country). Public listing of a phone number or e-mail does not mean consent to marketing.";
    public string Notices => "Open-source components: CommunityToolkit.Mvvm (MIT), Microsoft.Data.Sqlite (MIT), HtmlAgilityPack (MIT), ClosedXML (MIT), CsvHelper (MS-PL/Apache-2.0), DnsClient (Apache-2.0), libphonenumber-csharp (Apache-2.0), Microsoft.Extensions.* (MIT). " +
        "Map data © OpenStreetMap contributors, ODbL 1.0. The installer is not code-signed unless stated in the release notes; Windows SmartScreen may warn on first run.";

    public AboutViewModel(ILicenseService license, IDialogService dlg, ICurrentUser user) { _license = license; _dlg = dlg; _user = user; }

    public override Task OnNavigatedToAsync(object? parameter) { Refresh(); return Task.CompletedTask; }

    private void Refresh()
    {
        var l = _license.Current;
        Tier = l.Tier; Holder = l.Holder; LicenseMessage = l.Message; IsLicensed = l.IsValid && !l.IsTrial;
        Limits = $"Leads: {(l.MaxLeads == int.MaxValue ? "unlimited" : l.MaxLeads.ToString())}  ·  Users: {l.MaxUsers}  ·  " + (l.ExpiresUtc == null ? "No expiry" : $"Valid until {l.ExpiresUtc:dd MMM yyyy}");
    }

    [RelayCommand]
    private void Activate()
    {
        ErrorMessage = "";
        if (!_user.Can(Permission.ManageSettings)) { ErrorMessage = "Only an administrator can activate a licence."; return; }
        if (string.IsNullOrWhiteSpace(LicenseKey)) { ErrorMessage = "Paste your licence key first."; return; }
        var r = _license.Activate(LicenseKey);
        if (!r.IsValid) ErrorMessage = r.Message; else { StatusMessage = "Licence activated. " + r.Message; LicenseKey = ""; }
        Refresh();
    }

    [RelayCommand] private void CopyMachineId() => _dlg.CopyToClipboard(MachineId);
}
