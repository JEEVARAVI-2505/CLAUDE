using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CRM_Master.App.ViewModels;

/// <summary>Product information, licence status and third-party notices. Developer / support details come from branding.json.</summary>
public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly ILicenseProvider _license;
    public AboutViewModel(UiServices ui, ILicenseProvider license) : base(ui) => _license = license;

    public string ProductName => AppInfo.ProductName;
    public string Tagline => AppInfo.Tagline;
    public string Version => AppInfo.Version;
    public string Developer => AppInfo.Developer;
    public string Support => AppInfo.Support;
    public string LicenceText => AppInfo.Licence;
    public string Notices => AppInfo.ThirdPartyNotices;
    public string Runtime => $"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} on {System.Runtime.InteropServices.RuntimeInformation.OSDescription}";
    public string DataFolder => DatabasePaths.AppDataFolder;
    public string LogsFolder => DatabasePaths.LogsFolder;
    [ObservableProperty] private string _licenseStatus = "";
    [ObservableProperty] private bool _licenseExpired;

    public override Task ActivateAsync()
    {
        var st = _license.GetStatus(DateTime.UtcNow);
        LicenseStatus = st.Message; LicenseExpired = st.IsExpired;
        return Task.CompletedTask;
    }

    [RelayCommand] private void OpenData() { Directory.CreateDirectory(DataFolder); Ui.Dialogs.OpenInShell(DataFolder); }
    [RelayCommand] private void OpenLogs() { Directory.CreateDirectory(LogsFolder); Ui.Dialogs.OpenInShell(LogsFolder); }
}
