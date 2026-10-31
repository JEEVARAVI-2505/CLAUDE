using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Services;

namespace LeadPilot.Application.ViewModels;

/// <summary>A discovery source with its enable switch, key and endpoint.</summary>
public sealed partial class ProviderItem : ObservableObject
{
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string _apiKey = "";
    [ObservableProperty] private string _endpoint = "";
    [ObservableProperty] private string _testResult = "";
    [ObservableProperty] private bool _hasSavedKey;
    public IDiscoveryProvider Provider { get; init; } = null!;
    public string Name => Provider.DisplayName;
    public string Type => Provider.SourceType;
    public string Description => Provider.Description;
    public string Cost => Provider.CostNote;
    public string License => Provider.LicenseNote;
    public bool NeedsKey => Provider.RequiresApiKey;
}

public sealed record IntegrationNote(string Title, string Status, string Text);

/// <summary>Data sources, AI model, web-search key and a plain-language disclosure of what leaves the computer.</summary>
public sealed partial class IntegrationsViewModel : ViewModelBase
{
    private readonly DiscoveryOrchestrator _orch;
    private readonly ISettingsService _settings;
    private readonly ISecretStore _secrets;
    private readonly ISystemRepository _sys;
    private readonly IAiClient _ai;
    private readonly IWebsiteFinder _finder;
    private readonly ICurrentUser _user;

    [ObservableProperty] private string _aiProvider = "None";
    [ObservableProperty] private string _aiEndpoint = "";
    [ObservableProperty] private string _aiModel = "";
    [ObservableProperty] private string _aiKey = "";
    [ObservableProperty] private string _aiTestResult = "";
    [ObservableProperty] private string _braveKey = "";
    [ObservableProperty] private bool _hasBraveKey;
    [ObservableProperty] private bool _hasAiKey;
    [ObservableProperty] private string _searchTestResult = "";

    public ObservableCollection<ProviderItem> Providers { get; } = new();
    public ObservableCollection<UsageRow> Usage { get; } = new();
    public IReadOnlyList<string> AiProviders { get; } = new[] { "None", "Ollama", "OpenAI-compatible" };
    public IReadOnlyList<IntegrationNote> Notes { get; } = new[]
    {
        new IntegrationNote("Company websites", "Built in", "Each lead's own website can be read for public contact details (robots.txt respected, rate-limited, no forms or logins). Switch off in Settings → Privacy."),
        new IntegrationNote("Excel / CSV files", "Built in", "Import Centre. Legacy .xls must be saved as .xlsx or CSV first."),
        new IntegrationNote("Licensed B2B data providers", "Needs your own account", "Not bundled. Export a list from your provider (CSV/XLSX) and import it – the provider's licence terms still apply."),
        new IntegrationNote("CRM systems (HubSpot, Zoho, Salesforce…)", "Via file", "Export contacts from your CRM and import them. Direct two-way sync is not included in this version."),
        new IntegrationNote("Website enquiry forms & ad lead forms", "Via file", "Export submissions as CSV and import them; put the enquiry text in a column mapped to 'Notes / enquiry text' so it counts as buying-intent evidence."),
        new IntegrationNote("E-mail / WhatsApp sending", "Drafts only", "LeadPilot AI prepares drafts and opens your own mail app or WhatsApp. It never sends messages itself and never pretends a message was delivered.")
    };
    public static string Disclosure => "What leaves this computer (only when you use the feature): OpenStreetMap place search – the location text and the business categories you searched. Website reading – a normal page request to each company's own site. " +
        "Web search – the company name and city sent to Brave Search with your key. AI model – with Ollama everything stays on your computer; with an OpenAI-compatible provider, the lead text in the prompt is sent to that provider. " +
        "Nothing is uploaded automatically: no analytics, no lead data, no telemetry.";

    public IntegrationsViewModel(DiscoveryOrchestrator orch, ISettingsService settings, ISecretStore secrets, ISystemRepository sys, IAiClient ai, IWebsiteFinder finder, ICurrentUser user)
    { _orch = orch; _settings = settings; _secrets = secrets; _sys = sys; _ai = ai; _finder = finder; _user = user; }

    public override Task OnNavigatedToAsync(object? parameter) => RunAsync(async () =>
    {
        var s = _settings.Current;
        Providers.Clear();
        foreach (var p in _orch.Providers)
            Providers.Add(new ProviderItem { Provider = p, IsEnabled = s.EnabledProviders.Contains(p.Id), Endpoint = s.ProviderEndpoints.GetValueOrDefault(p.Id, ""), HasSavedKey = !string.IsNullOrEmpty(_secrets.Get(SecretNames.Provider(p.Id))) });
        AiProvider = s.AiProvider; AiEndpoint = s.AiEndpoint ?? ""; AiModel = s.AiModel ?? ""; HasAiKey = !string.IsNullOrEmpty(_secrets.Get(SecretNames.AiKey)); HasBraveKey = !string.IsNullOrEmpty(_secrets.Get(SecretNames.BraveSearch));
        var usage = await Task.Run(() => _sys.GetApiUsage(50));
        Usage.Clear(); foreach (var u in usage) Usage.Add(new UsageRow(u.At.ToLocalTime().ToString("dd MMM HH:mm"), u.Provider, u.Operation, u.Success ? "OK" : "Failed: " + u.ErrorMessage, u.ResultCount));
    });

    private void Require() { if (!_user.Can(Permission.ManageIntegrations)) throw new UnauthorizedAccessException("Only an administrator can change integrations."); }

    [RelayCommand]
    private Task SaveProvider(ProviderItem? item) => RunAsync(() =>
    {
        Require();
        if (item == null) return Task.CompletedTask;
        var s = _settings.Current;
        s.EnabledProviders.Remove(item.Provider.Id); if (item.IsEnabled) s.EnabledProviders.Add(item.Provider.Id);
        if (string.IsNullOrWhiteSpace(item.Endpoint)) s.ProviderEndpoints.Remove(item.Provider.Id); else s.ProviderEndpoints[item.Provider.Id] = item.Endpoint.Trim();
        if (!string.IsNullOrWhiteSpace(item.ApiKey)) { _secrets.Set(SecretNames.Provider(item.Provider.Id), item.ApiKey.Trim()); item.ApiKey = ""; item.HasSavedKey = true; }
        _settings.Save(s);
        StatusMessage = $"{item.Name} saved.";
        return Task.CompletedTask;
    });

    [RelayCommand]
    private Task RemoveProviderKey(ProviderItem? item) => RunAsync(() => { Require(); if (item != null) { _secrets.Set(SecretNames.Provider(item.Provider.Id), null); item.HasSavedKey = false; } return Task.CompletedTask; });

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TestProviderAsync(ProviderItem? item, CancellationToken ct)
    {
        if (item == null) return;
        item.TestResult = "Testing…";
        try { var a = await item.Provider.CheckAvailabilityAsync(_orch.SettingsFor(item.Provider), ct); item.TestResult = a.IsAvailable ? "✔ Reachable" : "✖ " + a.Message; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { item.TestResult = "✖ " + ex.Message; }
    }

    [RelayCommand]
    private Task SaveAi() => RunAsync(() =>
    {
        Require();
        var s = _settings.Current;
        s.AiProvider = AiProvider; s.AiEndpoint = string.IsNullOrWhiteSpace(AiEndpoint) ? null : AiEndpoint.Trim(); s.AiModel = string.IsNullOrWhiteSpace(AiModel) ? null : AiModel.Trim();
        if (!string.IsNullOrWhiteSpace(AiKey)) { _secrets.Set(SecretNames.AiKey, AiKey.Trim()); AiKey = ""; HasAiKey = true; }
        _settings.Save(s); StatusMessage = "AI settings saved.";
        return Task.CompletedTask;
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TestAiAsync(CancellationToken ct)
    {
        AiTestResult = "Testing…";
        if (!_ai.IsAvailable) { AiTestResult = "AI is off or no key is saved. Everything still works with rules."; return; }
        try { var r = await _ai.CompleteAsync("Reply with the single word OK.", "ping", ct); AiTestResult = string.IsNullOrWhiteSpace(r) ? "✖ Empty answer" : $"✔ {_ai.Name} answered"; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException) { AiTestResult = "✖ " + Friendly(ex); }
    }

    [RelayCommand]
    private Task SaveSearchKey() => RunAsync(() =>
    {
        Require();
        _secrets.Set(SecretNames.BraveSearch, string.IsNullOrWhiteSpace(BraveKey) ? null : BraveKey.Trim());
        HasBraveKey = !string.IsNullOrWhiteSpace(BraveKey) || HasBraveKey; BraveKey = ""; StatusMessage = "Web-search key saved.";
        return Task.CompletedTask;
    });

    [RelayCommand]
    private Task RemoveSearchKey() => RunAsync(() => { Require(); _secrets.Set(SecretNames.BraveSearch, null); HasBraveKey = false; return Task.CompletedTask; });

    [RelayCommand]
    private async Task TestSearchAsync(CancellationToken ct)
    {
        if (!_finder.IsConfigured) { SearchTestResult = "No key saved."; return; }
        try { var r = await _finder.FindAsync("OpenAI", null, ct); SearchTestResult = r.Count > 0 ? $"✔ Working ({r.Count} results)" : "✖ No results returned"; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { SearchTestResult = "✖ " + Friendly(ex); }
    }
}
