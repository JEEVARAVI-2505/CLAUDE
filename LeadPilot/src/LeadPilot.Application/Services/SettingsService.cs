using System.Text.Json;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;

namespace LeadPilot.Application.Services;

/// <summary>Loads and saves <see cref="AppSettings"/> as JSON in the Settings table.</summary>
public sealed class SettingsService : ISettingsService
{
    private const string Key = "app.settings";
    private readonly ISystemRepository _repo;
    private AppSettings _current;

    public SettingsService(ISystemRepository repo)
    {
        _repo = repo;
        _current = Load();
    }

    public AppSettings Current => _current;
    public event EventHandler? Changed;

    public void Save(AppSettings s)
    {
        _repo.SetSetting(Key, JsonSerializer.Serialize(s));
        _current = s;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Reload() { _current = Load(); Changed?.Invoke(this, EventArgs.Empty); }

    private AppSettings Load()
    {
        var json = _repo.GetSetting(Key);
        if (string.IsNullOrWhiteSpace(json)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings(); }
        catch (JsonException) { return new AppSettings(); }
    }
}
