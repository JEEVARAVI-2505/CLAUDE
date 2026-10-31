using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using LeadPilot.AI;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Services;
using LeadPilot.Application.ViewModels;
using LeadPilot.Data;
using LeadPilot.Desktop.Services;
using LeadPilot.Discovery.Providers;
using LeadPilot.Discovery.Web;
using LeadPilot.Infrastructure;
using LeadPilot.Infrastructure.Files;
using LeadPilot.Infrastructure.Licensing;
using LeadPilot.Infrastructure.Logging;
using LeadPilot.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeadPilot.Desktop;

/// <summary>Composition root: builds the service graph, applies migrations and shows the shell.</summary>
public partial class App : System.Windows.Application
{
    private Mutex? _single;
    private ServiceProvider? _services;
    private ILogger? _log;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--init"))
        {
            // Used by the installer: create the data folders and the empty database, then exit without showing a window.
            try { using var sp = Build(); Shutdown(0); } catch (Exception) { Shutdown(1); }
            return;
        }
        _single = new Mutex(true, @"Local\LeadPilotAI.SingleInstance", out var first);
        if (!first) { MessageBox.Show("LeadPilot AI is already running.", "LeadPilot AI", MessageBoxButton.OK, MessageBoxImage.Information); Shutdown(); return; }
        DispatcherUnhandledException += OnUnhandled;
        AppDomain.CurrentDomain.UnhandledException += (_, a) => _log?.LogCritical(a.ExceptionObject as Exception, "Unhandled exception");
        TaskScheduler.UnobservedTaskException += (_, a) => { _log?.LogError(a.Exception, "Unobserved task exception"); a.SetObserved(); };
        try
        {
            _services = Build();
            _log = _services.GetRequiredService<ILoggerFactory>().CreateLogger("App");
            var settings = _services.GetRequiredService<ISettingsService>();
            ThemeManager.Apply(settings.Current.Theme);
            _services.GetRequiredService<DataGovernanceService>(); // constructed early so retention can run after sign-in
            var main = _services.GetRequiredService<MainViewModel>();
            var win = new MainWindow { DataContext = main };
            MainWindow = win; win.Show();
            await main.StartAsync();
            var tourIdx = Array.IndexOf(e.Args, "--screenshot-dir");
            if (tourIdx >= 0 && tourIdx + 1 < e.Args.Length) await ScreenshotTour.RunAsync(win, main, e.Args[tourIdx + 1]);
        }
        catch (Exception ex)
        {
            _log?.LogCritical(ex, "Startup failed");
            MessageBox.Show("LeadPilot AI could not start.\n\n" + ex.Message + "\n\nDetails were written to the log folder:\n" + new AppPaths().Logs, "LeadPilot AI", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _log?.LogError(e.Exception, "Unhandled UI exception");
        MessageBox.Show("Something unexpected happened, but your data is safe.\n\n" + e.Exception.Message + "\n\nIf this keeps happening, send the newest file from the Logs folder to support.", "LeadPilot AI", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        _single?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider Build()
    {
        var paths = new AppPaths(); paths.EnsureCreated();
        var db = new SqliteDb(paths.DatabaseFile); db.Migrate();
        var s = new ServiceCollection();
        s.AddSingleton(paths); s.AddSingleton<IAppFolders>(paths);
        s.AddSingleton(db); s.AddSingleton<IDatabaseFile, DatabaseFile>();
        s.AddLogging(b => b.AddProvider(new FileLoggerProvider(paths.Logs)));
        s.AddSingleton<IClock, SystemClock>();

        s.AddSingleton<ILeadRepository, LeadRepository>(); s.AddSingleton<IWorkRepository, WorkRepository>(); s.AddSingleton<IDuplicateRepository, DuplicateRepository>();
        s.AddSingleton<ISystemRepository, SystemRepository>(); s.AddSingleton<IStatsRepository, StatsRepository>();

        s.AddSingleton<ISettingsService, SettingsService>();
        s.AddSingleton<UserService>(); s.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<UserService>());
        s.AddSingleton<ISecretStore>(new SecretStore(paths.SecretsFile));
        s.AddSingleton<ILicenseService, LicenseService>();
        s.AddSingleton<IBackupService, BackupService>();
        s.AddSingleton<IExportService, ExportService>(); s.AddSingleton<IImportReader, TabularFileReader>();
        s.AddSingleton<IDialogService, DialogService>();

        s.AddSingleton(sp => HttpClients.Create(sp.GetRequiredService<ISettingsService>().Current.ContactEmail));
        s.AddSingleton<IWebsiteExtractor>(sp => new WebsiteExtractor(sp.GetRequiredService<HttpClient>(), () => sp.GetRequiredService<ISettingsService>().Current.DefaultRegion));
        s.AddSingleton<IWebsiteProbe>(sp => new HttpWebsiteProbe(sp.GetRequiredService<HttpClient>()));
        s.AddSingleton<IDnsResolver, DnsMailResolver>();
        s.AddSingleton<IWebsiteFinder>(sp => new BraveWebsiteFinder(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<ISecretStore>()));
        s.AddSingleton<IDiscoveryProvider>(sp => new OsmOverpassProvider(sp.GetRequiredService<HttpClient>()));
        s.AddSingleton<IAiClient>(sp => new SwitchableAiClient(sp.GetRequiredService<ISettingsService>(), sp.GetRequiredService<ISecretStore>(), sp.GetRequiredService<HttpClient>()));

        s.AddSingleton<LeadIngestService>(); s.AddSingleton<ScoringService>(); s.AddSingleton<DuplicateService>(); s.AddSingleton<VerificationService>();
        s.AddSingleton<EnrichmentService>(); s.AddSingleton<DiscoveryOrchestrator>(); s.AddSingleton<FollowUpService>(); s.AddSingleton<OutreachDrafter>();
        s.AddSingleton<AssistantService>(); s.AddSingleton<ImportService>(); s.AddSingleton<DataGovernanceService>();

        s.AddSingleton<MainViewModel>(); s.AddSingleton<INavigator>(sp => sp.GetRequiredService<MainViewModel>());
        s.AddSingleton<Func<PageKey, ViewModelBase>>(sp => key => key switch
        {
            PageKey.Setup => sp.GetRequiredService<SetupViewModel>(), PageKey.Dashboard => sp.GetRequiredService<DashboardViewModel>(),
            PageKey.LeadFinder => sp.GetRequiredService<LeadFinderViewModel>(), PageKey.LeadDatabase => sp.GetRequiredService<LeadDatabaseViewModel>(),
            PageKey.LeadDetails => sp.GetRequiredService<LeadDetailsViewModel>(), PageKey.Duplicates => sp.GetRequiredService<DuplicateManagerViewModel>(),
            PageKey.Enrichment => sp.GetRequiredService<EnrichmentViewModel>(), PageKey.Qualification => sp.GetRequiredService<QualificationViewModel>(),
            PageKey.FollowUps => sp.GetRequiredService<FollowUpViewModel>(), PageKey.Campaigns => sp.GetRequiredService<CampaignViewModel>(),
            PageKey.Analytics => sp.GetRequiredService<AnalyticsViewModel>(), PageKey.Assistant => sp.GetRequiredService<AssistantViewModel>(),
            PageKey.Import => sp.GetRequiredService<ImportViewModel>(), PageKey.Export => sp.GetRequiredService<ExportViewModel>(),
            PageKey.Integrations => sp.GetRequiredService<IntegrationsViewModel>(), PageKey.Settings => sp.GetRequiredService<SettingsViewModel>(),
            PageKey.Backup => sp.GetRequiredService<BackupViewModel>(), PageKey.About => sp.GetRequiredService<AboutViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(key))
        });
        foreach (var t in new[] { typeof(SetupViewModel), typeof(DashboardViewModel), typeof(LeadFinderViewModel), typeof(LeadDatabaseViewModel), typeof(LeadDetailsViewModel), typeof(DuplicateManagerViewModel),
                                  typeof(EnrichmentViewModel), typeof(QualificationViewModel), typeof(FollowUpViewModel), typeof(CampaignViewModel), typeof(AnalyticsViewModel), typeof(AssistantViewModel),
                                  typeof(ImportViewModel), typeof(ExportViewModel), typeof(IntegrationsViewModel), typeof(SettingsViewModel), typeof(BackupViewModel), typeof(AboutViewModel) })
            s.AddTransient(t);
        return s.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false });
    }
}
