using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Application.ViewModels;
using LeadPilot.Data;
using LeadPilot.Infrastructure;
using LeadPilot.Infrastructure.Files;
using LeadPilot.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeadPilot.Tests.Support;

public sealed class FakeDialogs : IDialogService
{
    public string? OpenFile, SaveFile, Folder, Password, Text; public bool ConfirmAnswer = true;
    public List<string> Infos = new(), Errors = new(), Opened = new(), Copied = new(); public string? Theme;
    public Queue<string?> Passwords = new();
    public string? PickOpenFile(string t, string f) => OpenFile;
    public string? PickSaveFile(string t, string f, string d) => SaveFile;
    public string? PickFolder(string t) => Folder;
    public bool Confirm(string t, string m) => ConfirmAnswer;
    public void Info(string t, string m) => Infos.Add(m);
    public void Error(string t, string m) => Errors.Add(m);
    public string? PromptPassword(string t, string m, bool c) => Passwords.Count > 0 ? Passwords.Dequeue() : Password;
    public string? PromptText(string t, string m, string? d = null) => Text;
    public void OpenUrl(string u) => Opened.Add(u);
    public void CopyToClipboard(string t) => Copied.Add(t);
    public void SetTheme(string t) => Theme = t;
}

public sealed class FakeNav : INavigator
{
    public List<(PageKey Key, object? Param)> Calls = new();
    public void Navigate(PageKey key, object? parameter = null) => Calls.Add((key, parameter));
}

public sealed class FakeAiClient : IAiClient
{
    public bool IsAvailable => false; public string Name => "off";
    public Task<string?> CompleteAsync(string s, string p, CancellationToken ct) => Task.FromResult<string?>(null);
}

public sealed class FakeFinder : IWebsiteFinder
{
    public bool IsConfigured => false;
    public Task<IReadOnlyList<WebsiteCandidate>> FindAsync(string n, string? l, CancellationToken ct) => Task.FromResult<IReadOnlyList<WebsiteCandidate>>(Array.Empty<WebsiteCandidate>());
}

public sealed class FakeExtractor : IWebsiteExtractor
{
    public Task<WebsiteExtraction> ExtractAsync(string url, CancellationToken ct) => Task.FromResult(new WebsiteExtraction { Url = url, Error = "offline test" });
}

public sealed class FakeDns : IDnsResolver { public Task<DnsCheck> CheckAsync(string d, CancellationToken ct) => Task.FromResult(new DnsCheck(true, true, null)); }
public sealed class FakeProbe : IWebsiteProbe { public Task<WebsiteProbeResult> ProbeAsync(string u, CancellationToken ct) => Task.FromResult(new WebsiteProbeResult(true, 200, u, false, null)); }
public sealed class MemSecretStore : ISecretStore
{
    public Dictionary<string, string> Data = new();
    public string? Get(string n) => Data.GetValueOrDefault(n);
    public void Set(string n, string? v) { if (string.IsNullOrEmpty(v)) Data.Remove(n); else Data[n] = v; }
}

public sealed class ScriptedProvider : IDiscoveryProvider
{
    public List<ProspectCandidate> Items { get; } = new();
    public Exception? Fail { get; set; }
    public string Id => "scripted"; public string DisplayName => "Scripted source"; public string SourceType => "Test"; public string Description => "";
    public bool RequiresApiKey => false; public string CostNote => ""; public string LicenseNote => ""; public TimeSpan MinInterval => TimeSpan.Zero;
    public Task<ProviderAvailability> CheckAvailabilityAsync(ProviderSettings s, CancellationToken ct) => Task.FromResult(new ProviderAvailability(true, "ok"));
    public Task<ProviderPage> SearchPageAsync(DiscoveryRequest r, string? t, ProviderSettings s, CancellationToken ct)
        => Fail != null ? Task.FromException<ProviderPage>(Fail) : Task.FromResult(new ProviderPage(Items.Select(i => i).ToList(), null, Array.Empty<string>()));
    public Task<ProspectCandidate?> GetDetailsAsync(string id, ProviderSettings s, CancellationToken ct) => Task.FromResult<ProspectCandidate?>(null);
}

/// <summary>Full service graph around a <see cref="TestEnv"/> for view-model tests.</summary>
public sealed class VmEnv : IDisposable
{
    public TestEnv E { get; } = new();
    public FakeDialogs Dlg { get; } = new();
    public FakeNav Nav { get; } = new();
    public ScriptedProvider Provider { get; } = new();
    public MemSecretStore Secrets { get; } = new();
    public AppPaths Paths { get; }
    public ExportService Export { get; } = new();
    public VerificationService Verify { get; }
    public EnrichmentService Enrich { get; }
    public OutreachDrafter Drafter { get; }
    public DataGovernanceService Gov { get; }
    public DiscoveryOrchestrator Orchestrator { get; }
    public AssistantService Assistant { get; }
    public ImportService Import { get; }
    public BackupService Backup { get; }
    public FakeAiClient Ai { get; } = new();

    public VmEnv()
    {
        Paths = new AppPaths(Path.Combine(E.Folder, "app")); Paths.EnsureCreated();
        var s = E.Settings.Current; s.SetupCompleted = true; s.OwnerName = "Asha Admin"; s.CompanyName = "SunCo"; s.EnabledProviders = new() { "scripted" }; s.VisitWebsitesDuringDiscovery = false; E.Settings.Save(s);
        Verify = new VerificationService(E.Leads, E.Work, new FakeDns(), new FakeProbe(), E.Settings);
        Enrich = new EnrichmentService(E.Leads, new FakeExtractor(), new FakeFinder(), Ai, E.Scoring, E.Settings, E.Work, E.Clock);
        Drafter = new OutreachDrafter(Ai, E.Work);
        Gov = new DataGovernanceService(E.Leads, E.Sys, E.Settings, E.Users, E.Clock);
        Orchestrator = new DiscoveryOrchestrator(new IDiscoveryProvider[] { Provider }, Secrets, E.Settings, E.Sys, new FakeExtractor(), NullLogger<DiscoveryOrchestrator>.Instance, E.Clock);
        Assistant = new AssistantService(E.Leads, E.Work, E.Dups, E.Stats, Drafter, E.Settings, Ai, E.Clock);
        Import = new ImportService(new TabularFileReader(), E.Ingest, E.Sys, E.Users, E.Settings, E.Scoring, E.Clock);
        Backup = new BackupService(new DatabaseFile(E.Db), Paths, E.Clock);
    }

    public void Dispose() => E.Dispose();
}
