using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Services;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.Services;
using CRM_Master.Integrations.Interfaces;

namespace CRM_Master.App.ViewModels;

public sealed record ConnectorRow(string Name, string Maturity, string Description);

/// <summary>Application settings, backup and restore, database location and integration status.</summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ThemeService _themeService;
    private readonly IBackupService _backup;
    private readonly IContactRepository _repo;
    private readonly IConnectorRegistry _connectors;
    private readonly ICredentialStore _credentials;

    public SettingsViewModel(UiServices ui, ThemeService theme, IBackupService backup, IContactRepository repo, IConnectorRegistry connectors, ICredentialStore credentials) : base(ui)
    {
        _themeService = theme; _backup = backup; _repo = repo; _connectors = connectors; _credentials = credentials;
        _selectedPolicy = PolicyOptions[0];
        foreach (var c in connectors.All) Connectors.Add(new ConnectorRow(c.Info.DisplayName, c.Info.Maturity switch { ConnectorMaturity.DevelopmentMock => "Development mock only", ConnectorMaturity.Planned => "Planned", ConnectorMaturity.Beta => "Beta", _ => "Production" }, c.Info.Description));
    }

    public IReadOnlyList<string> ThemeOptions { get; } = new[] { ThemeService.Light, ThemeService.Dark };
    public IReadOnlyList<PolicyOption> PolicyOptions { get; } = new[]
    {
        new PolicyOption("Skip duplicates", ImportDuplicatePolicy.Skip, ""), new PolicyOption("Import and flag for review", ImportDuplicatePolicy.ImportAndFlag, ""),
        new PolicyOption("Propose an update to the existing contact", ImportDuplicatePolicy.ProposeUpdate, "")
    };
    public ObservableCollection<BackupRecord> Backups { get; } = new();
    public ObservableCollection<ConnectorRow> Connectors { get; } = new();

    [ObservableProperty] private string _theme = ThemeService.Light;
    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private string _countryCode = "91";
    [ObservableProperty] private int _pageSize = 100;
    [ObservableProperty] private double _highThreshold = 0.9;
    [ObservableProperty] private double _possibleThreshold = 0.8;
    [ObservableProperty] private double _lowThreshold = 0.7;
    [ObservableProperty] private bool _reqName = true;
    [ObservableProperty] private bool _reqEmail = true;
    [ObservableProperty] private bool _reqPhone;
    [ObservableProperty] private bool _reqCompany;
    [ObservableProperty] private bool _validateWebsite = true;
    [ObservableProperty] private bool _validateCountry = true;
    [ObservableProperty] private bool _cleanOnImport = true;
    [ObservableProperty] private PolicyOption _selectedPolicy;
    [ObservableProperty] private bool _automationEnabled = true;
    [ObservableProperty] private int _logRetentionDays = 365;
    [ObservableProperty] private bool _autoBackup = true;
    [ObservableProperty] private int _backupsToKeep = 10;
    [ObservableProperty] private string _backupFolder = "";
    [ObservableProperty] private string _exportFolder = "";
    [ObservableProperty] private BackupRecord? _selectedBackup;
    [ObservableProperty] private string? _savedMessage;

    public string DatabaseFile => DatabasePaths.DatabaseFile;
    public string DataFolder => DatabasePaths.AppDataFolder;
    public string CredentialStatus => _credentials.IsAvailable ? "Windows DPAPI protected storage is available. Credentials, when connectors are added, are encrypted for your Windows account." : "Protected credential storage needs Windows. Credentials are never stored in plain text.";
    public bool HasSelectedBackup => SelectedBackup is not null;
    partial void OnSelectedBackupChanged(BackupRecord? value) => OnPropertyChanged(nameof(HasSelectedBackup));

    public override async Task ActivateAsync()
    {
        var s = Ui.Settings.Current;
        Theme = s.Theme; DisplayName = s.DisplayName; CountryCode = s.DefaultCountryCode; PageSize = s.PageSize;
        HighThreshold = s.DuplicateHighThreshold; PossibleThreshold = s.DuplicatePossibleThreshold; LowThreshold = s.DuplicateLowThreshold;
        ReqName = s.RequiredFields.Contains(nameof(ContactField.FullName)); ReqEmail = s.RequiredFields.Contains(nameof(ContactField.Email));
        ReqPhone = s.RequiredFields.Contains(nameof(ContactField.Phone)); ReqCompany = s.RequiredFields.Contains(nameof(ContactField.Company));
        ValidateWebsite = s.ValidateWebsite; ValidateCountry = s.ValidateCountry; CleanOnImport = s.CleanOnImport;
        SelectedPolicy = PolicyOptions.First(p => p.Policy == s.ImportDuplicatePolicy);
        AutomationEnabled = s.AutomationEnabled; LogRetentionDays = s.LogRetentionDays; AutoBackup = s.AutoBackupBeforeBulk; BackupsToKeep = s.BackupsToKeep;
        BackupFolder = s.BackupFolder; ExportFolder = s.ExportFolder; SavedMessage = null;
        await LoadBackupsAsync();
    }

    [RelayCommand]
    private async Task Save()
    {
        var s = Ui.Settings.Current.Clone();
        var oldRequired = string.Join(",", s.RequiredFields.OrderBy(x => x)); var oldCountry = s.DefaultCountryCode;
        s.Theme = Theme; s.DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? "Local User" : DisplayName.Trim(); s.DefaultCountryCode = CountryCode.Trim().TrimStart('+'); s.PageSize = PageSize;
        s.DuplicateHighThreshold = Math.Round(HighThreshold, 2); s.DuplicatePossibleThreshold = Math.Round(PossibleThreshold, 2); s.DuplicateLowThreshold = Math.Round(LowThreshold, 2);
        s.RequiredFields = new[] { ReqName ? nameof(ContactField.FullName) : null, ReqEmail ? nameof(ContactField.Email) : null, ReqPhone ? nameof(ContactField.Phone) : null, ReqCompany ? nameof(ContactField.Company) : null }.Where(x => x is not null).Select(x => x!).ToList();
        s.ValidateWebsite = ValidateWebsite; s.ValidateCountry = ValidateCountry; s.CleanOnImport = CleanOnImport; s.ImportDuplicatePolicy = SelectedPolicy.Policy;
        s.AutomationEnabled = AutomationEnabled; s.LogRetentionDays = LogRetentionDays; s.AutoBackupBeforeBulk = AutoBackup; s.BackupsToKeep = BackupsToKeep;
        s.BackupFolder = BackupFolder.Trim(); s.ExportFolder = ExportFolder.Trim();

        if (!await RunAsync("Saving settings", async () =>
        {
            if (!string.IsNullOrWhiteSpace(s.BackupFolder)) Directory.CreateDirectory(s.BackupFolder);
            if (!string.IsNullOrWhiteSpace(s.ExportFolder)) Directory.CreateDirectory(s.ExportFolder);
            await Ui.Settings.SaveAsync(s);
        }, AuditOperation.SettingsChanged)) return;

        _themeService.Apply(s.Theme);
        SavedMessage = "Settings saved.";
        Ui.Notifications.Add(NotificationLevel.Success, "Settings saved", "Your preferences were updated.");
        var requiredChanged = oldRequired != string.Join(",", s.RequiredFields.OrderBy(x => x)) || oldCountry != s.DefaultCountryCode;
        if (requiredChanged && Ui.Dialogs.Confirm("Update existing contacts?", "The required fields or phone country code changed. Re-evaluate the stored valid / invalid / incomplete flags of all contacts now so the dashboard and filters match the new rules?", "Re-evaluate"))
            await RunAsync("Re-evaluating contacts", async () => { var n = await _repo.ReevaluateQualityAsync(); Ui.Notifications.Add(NotificationLevel.Info, "Contacts re-evaluated", $"{n:N0} contact(s) changed status."); });
    }

    [RelayCommand] private void BrowseBackupFolder() { if (Ui.Dialogs.PickFolder("Choose the backup folder") is { } p) BackupFolder = p; }
    [RelayCommand] private void BrowseExportFolder() { if (Ui.Dialogs.PickFolder("Choose the default export folder") is { } p) ExportFolder = p; }

    private async Task LoadBackupsAsync()
    {
        var list = await _backup.ListAsync();
        Backups.Clear(); foreach (var b in list) Backups.Add(b);
    }

    [RelayCommand]
    private async Task BackupNow()
    {
        BackupRecord? rec = null;
        if (await RunAsync("Creating backup", async () => rec = await _backup.CreateAsync(BackupKind.Manual, "Manual backup")) && rec is not null)
            Ui.Notifications.Add(NotificationLevel.Success, "Backup created", $"{rec.ContactCount:N0} contact(s), {rec.SizeBytes / 1024.0:N0} KB: {Path.GetFileName(rec.FilePath)}");
        await LoadBackupsAsync();
    }

    [RelayCommand]
    private async Task RestoreSelected()
    {
        if (SelectedBackup is null) return;
        await RestoreAsync(SelectedBackup.FilePath, $"the backup from {SelectedBackup.CreatedAt.ToLocalTime():dd MMM yyyy HH:mm} ({SelectedBackup.ContactCount:N0} contacts)");
    }

    [RelayCommand]
    private async Task RestoreFromFile()
    {
        var path = Ui.Dialogs.PickOpenFile("CRM MASTER backup (*.db)|*.db|All files (*.*)|*.*", "Choose a backup file to restore");
        if (path is not null) await RestoreAsync(path, $"the file {Path.GetFileName(path)}");
    }

    private async Task RestoreAsync(string file, string description)
    {
        if (!Ui.Dialogs.Confirm("Restore backup?", $"This replaces ALL current data with {description}.\n\nA safety backup of your current data is created first, so you can go back. Continue?", "Restore", danger: true)) return;
        if (!await RunAsync("Restoring backup", () => _backup.RestoreAsync(file), AuditOperation.Restore)) return;
        await Ui.Settings.LoadAsync();
        Ui.Notifications.Add(NotificationLevel.Success, "Backup restored", "Your data was restored. A safety copy of the previous data was kept.");
        Ui.Dialogs.Info("Backup restored", "Your data has been restored. A safety copy of the data you had before is listed under Backups.");
        await ActivateAsync();
        Ui.Navigation.Navigate(Module.Dashboard);
    }

    [RelayCommand] private void OpenBackupFolder() { var f = string.IsNullOrWhiteSpace(BackupFolder) ? DatabasePaths.DefaultBackupFolder : BackupFolder; Directory.CreateDirectory(f); Ui.Dialogs.OpenInShell(f); }
    [RelayCommand] private void OpenDataFolder() { Directory.CreateDirectory(DataFolder); Ui.Dialogs.OpenInShell(DataFolder); }

    [RelayCommand]
    private async Task ChangeDatabaseLocation()
    {
        var folder = Ui.Dialogs.PickFolder("Choose the new folder for the CRM MASTER database");
        if (folder is null) return;
        var target = Path.Combine(folder, "crm_master.db");
        if (File.Exists(target) && !Ui.Dialogs.Confirm("A database already exists there", $"{target} already exists and will be used as-is the next time CRM MASTER starts (nothing is copied over it). Continue?", "Use it")) return;
        await RunAsync("Moving database", async () =>
        {
            if (!File.Exists(target)) await _backup.CreateAtAsync(target, "Copy to new database location");
            DatabasePaths.SetDatabaseFolder(folder);
        });
        Ui.Dialogs.Info("Restart required", $"CRM MASTER will use the database in:\n{folder}\n\nafter you close and reopen the application. The previous database file was left where it was; delete it yourself when you no longer need it.");
    }
}
