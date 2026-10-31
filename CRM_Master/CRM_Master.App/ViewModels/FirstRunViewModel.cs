using CommunityToolkit.Mvvm.ComponentModel;
using CRM_Master.App.Services;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.Services;
using CRM_Master.Engine.Import;

namespace CRM_Master.App.ViewModels;

/// <summary>First-launch setup: profile, phone country code, theme and the optional sample dataset.</summary>
public sealed partial class FirstRunViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ImportService _import;
    private readonly ThemeService _theme;

    public FirstRunViewModel(ISettingsService settings, ImportService import, ThemeService theme)
    { _settings = settings; _import = import; _theme = theme; _displayName = Environment.UserName; }

    [ObservableProperty] private string _displayName;
    [ObservableProperty] private string _countryCode = "91";
    [ObservableProperty] private bool _useDarkTheme;
    [ObservableProperty] private bool _loadSample = true;
    [ObservableProperty] private string? _error;

    partial void OnUseDarkThemeChanged(bool value) => _theme.Apply(value ? ThemeService.Dark : ThemeService.Light);

    public string DataFolder => DatabasePaths.DatabaseFolder;

    public bool Validate()
    {
        var s = _settings.Current.Clone(); s.DefaultCountryCode = (CountryCode ?? "").Trim().TrimStart('+');
        try { SettingsService.Validate(s); Error = null; return true; }
        catch (ArgumentException ex) { Error = ex.Message; return false; }
    }

    public async Task ApplyAsync()
    {
        var s = _settings.Current.Clone();
        s.DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? "Local User" : DisplayName.Trim();
        s.DefaultCountryCode = CountryCode.Trim().TrimStart('+');
        s.Theme = UseDarkTheme ? ThemeService.Dark : ThemeService.Light;
        s.FirstRunCompleted = true; s.FirstRunUtc = DateTime.UtcNow;
        Directory.CreateDirectory(s.BackupFolder); Directory.CreateDirectory(s.ExportFolder);
        await _settings.SaveAsync(s);

        if (!LoadSample) return;
        var sample = Path.Combine(AppContext.BaseDirectory, "SampleData", "sample-contacts.csv");
        if (!File.Exists(sample)) { Serilog.Log.Warning("Sample dataset not found at {Path}", sample); return; }
        var table = await TableReader.ReadAsync(sample, 50);
        await _import.ImportAsync(sample, _import.DefaultOptions(ColumnMapper.Suggest(table)));
    }
}
