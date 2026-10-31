using System.Collections.ObjectModel;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;

namespace LeadPilot.Application.ViewModels;

public sealed record BackupFileRow(string Name, string Size, string Created, string Path);

/// <summary>Create, list and restore backups (optionally encrypted) – also the way to take all your data with you.</summary>
public sealed partial class BackupViewModel : ViewModelBase
{
    private readonly IBackupService _backup;
    private readonly IAppFolders _folders;
    private readonly ISettingsService _settings;
    private readonly ICurrentUser _user;
    private readonly IDialogService _dlg;
    private readonly INavigator _nav;

    [ObservableProperty] private string _folder = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _passwordConfirm = "";
    [ObservableProperty] private BackupFileRow? _selected;

    public ObservableCollection<BackupFileRow> Backups { get; } = new();

    public BackupViewModel(IBackupService backup, IAppFolders folders, ISettingsService settings, ICurrentUser user, IDialogService dlg, INavigator nav)
    { _backup = backup; _folders = folders; _settings = settings; _user = user; _dlg = dlg; _nav = nav; }

    public override Task OnNavigatedToAsync(object? parameter)
    {
        if (string.IsNullOrEmpty(Folder)) Folder = _folders.Backups;
        return ListAsync();
    }

    private Task ListAsync() => RunAsync(async () =>
    {
        var rows = await Task.Run(() => Directory.Exists(Folder)
            ? new DirectoryInfo(Folder).GetFiles("*.lpbackup").OrderByDescending(f => f.LastWriteTimeUtc).Select(f => new BackupFileRow(f.Name, $"{f.Length / 1024.0 / 1024.0:0.##} MB", f.LastWriteTime.ToString("dd MMM yyyy HH:mm"), f.FullName)).ToList()
            : new List<BackupFileRow>());
        Backups.Clear(); foreach (var r in rows) Backups.Add(r);
    });

    private void Require() { if (!_user.Can(Permission.ManageBackups)) throw new UnauthorizedAccessException("Only an administrator can back up or restore."); }

    [RelayCommand]
    private void ChooseFolder() { var f = _dlg.PickFolder("Choose backup folder"); if (f != null) { Folder = f; _ = ListAsync(); } }

    [RelayCommand]
    private async Task CreateBackup()
    {
        await RunAsync(async () =>
        {
            Require();
            if (!string.IsNullOrEmpty(Password) && Password != PasswordConfirm) { ErrorMessage = "The two passwords do not match."; return; }
            if (!string.IsNullOrEmpty(Password) && Password.Length < 8) { ErrorMessage = "Use at least 8 characters for the backup password."; return; }
            var path = await Task.Run(() => _backup.CreateBackup(Folder, string.IsNullOrEmpty(Password) ? null : Password));
            StatusMessage = (string.IsNullOrEmpty(Password) ? "Backup created (not encrypted): " : "Encrypted backup created – keep the password safe, it cannot be recovered: ") + path;
            Password = PasswordConfirm = "";
        }, "Creating backup…");
        if (!HasError) await ListAsync();
    }

    [RelayCommand] private void ChooseAndRestore() { var f = _dlg.PickOpenFile("Choose a backup to restore", "LeadPilot backup (*.lpbackup)|*.lpbackup|All files (*.*)|*.*"); if (f != null) _ = RestoreFile(f); }
    [RelayCommand] private Task RestoreSelected() => Selected == null ? Task.CompletedTask : RestoreFile(Selected.Path);

    private Task RestoreFile(string path) => RunAsync(async () =>
    {
        Require();
        if (!_dlg.Confirm("Restore backup", "Replace ALL current data with this backup? A safety copy of the current data is saved first. The app will return to the Dashboard.")) return;
        string? pw = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try { var p = pw; await Task.Run(() => _backup.Restore(path, p)); break; }
            catch (CryptographicException ex) when (attempt < 2)
            {
                pw = _dlg.PromptPassword("Backup password", ex.Message.Contains("Wrong") ? "Wrong password – try again:" : "This backup is encrypted. Enter its password:", false);
                if (pw == null) return;
            }
        }
        _settings.Reload();
        _dlg.Info("Restore complete", "Your data was restored.");
        _nav.Navigate(PageKey.Dashboard);
    }, "Restoring…");

    [RelayCommand] private void OpenFolder() { Directory.CreateDirectory(Folder); _dlg.OpenUrl(Folder); }
}
