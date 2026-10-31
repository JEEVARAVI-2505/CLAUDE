using System.Windows;
using System.Windows.Threading;
using CRM_Master.App.Services;
using CRM_Master.App.ViewModels;
using CRM_Master.App.Views;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using CRM_Master.Engine;
using CRM_Master.Engine.Automation;
using CRM_Master.Engine.BulkUpdate;
using CRM_Master.Engine.Cleaning;
using CRM_Master.Engine.DuplicateDetection;
using CRM_Master.Engine.Import;
using CRM_Master.Engine.Licensing;
using CRM_Master.Engine.Reporting;
using CRM_Master.Integrations;
using CRM_Master.Integrations.Interfaces;
using CRM_Master.Integrations.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CRM_Master.App;

/// <summary>Composition root: logging, dependency injection, database start-up, first-run, global error handling.</summary>
public partial class App : Application
{
    private Mutex? _singleInstance;
    private ServiceProvider? _services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Directory.CreateDirectory(DatabasePaths.LogsFolder);
        Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
            .WriteTo.File(Path.Combine(DatabasePaths.LogsFolder, "crm-master-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 60, shared: true).CreateLogger();
        Log.Information("CRM MASTER {Version} starting", AppInfo.Version);

        DispatcherUnhandledException += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += (_, a) => Log.Fatal(a.ExceptionObject as Exception, "Unhandled exception");
        TaskScheduler.UnobservedTaskException += (_, a) => { Log.Error(a.Exception, "Unobserved task exception"); a.SetObserved(); };

        // One instance per Windows user: two processes on one SQLite file would risk lock contention.
        _singleInstance = new Mutex(true, "CRM_MASTER_SINGLE_INSTANCE_" + Environment.UserName, out var isFirst);
        if (!isFirst)
        {
            MessageBox.Show("CRM MASTER is already running. Switch to the open window.", "CRM MASTER", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown(); return;
        }

        try
        {
            _services = BuildServices();
            var theme = _services.GetRequiredService<ThemeService>();
            var settings = _services.GetRequiredService<ISettingsService>();

            await DatabaseInitializer.InitializeAsync(_services.GetRequiredService<IDbContextFactory<CrmDbContext>>());
            await settings.LoadAsync();
            theme.Apply(settings.Current.Theme);
            await _services.GetRequiredService<AutomationService>().SeedDefaultsAsync();

            if (!settings.Current.FirstRunCompleted && !await RunFirstRunAsync(_services)) { Shutdown(); return; }
            _ = PurgeOldLogsAsync(_services, settings);

            var main = new MainWindow(_services.GetRequiredService<MainViewModel>());
            MainWindow = main; main.Show();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Start-up failed");
            MessageBox.Show($"CRM MASTER could not start.\n\n{ex.GetType().Name}: {ex.Message}\n\nDetails were written to:\n{DatabasePaths.LogsFolder}", "CRM MASTER", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static ServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();
        var factory = new SqliteContextFactory(DatabasePaths.DatabaseFile);
        sc.AddSingleton(factory);
        sc.AddSingleton<IDbContextFactory<CrmDbContext>>(factory);

        sc.AddSingleton<ISettingsService, SettingsService>();
        sc.AddSingleton<IAuditService, AuditService>();
        sc.AddSingleton<IContactRepository, ContactRepository>();
        sc.AddSingleton<IBackupService, BackupService>();
        sc.AddSingleton<IDashboardService, DashboardService>();
        sc.AddSingleton<AutomationService>();
        sc.AddSingleton<ReportService>();
        sc.AddSingleton<IImportReportGenerator>(p => p.GetRequiredService<ReportService>());
        sc.AddSingleton<ImportService>();
        sc.AddSingleton<CleaningService>();
        sc.AddSingleton<DuplicateService>();
        sc.AddSingleton<BulkUpdateService>();
        sc.AddSingleton<BatchUndoService>();
        sc.AddSingleton<ILicenseProvider, EvaluationLicenseProvider>();
        sc.AddSingleton<IConnectorRegistry, ConnectorRegistry>();
        sc.AddSingleton<ICredentialStore>(_ => new DpapiCredentialStore(DatabasePaths.CredentialsFolder));

        sc.AddSingleton<ThemeService>();
        sc.AddSingleton<IDialogService, DialogService>();
        sc.AddSingleton<IBusyService, BusyService>();
        sc.AddSingleton<INotificationCenter, NotificationCenter>();
        sc.AddSingleton<INavigationService, NavigationService>();
        sc.AddSingleton<UiServices>();

        sc.AddSingleton<MainViewModel>();
        foreach (var t in new[] { typeof(DashboardViewModel), typeof(ImportViewModel), typeof(CleaningViewModel), typeof(DuplicatesViewModel), typeof(ContactsViewModel),
                                  typeof(BulkUpdateViewModel), typeof(ValidationViewModel), typeof(AutomationViewModel), typeof(ReportsViewModel), typeof(ActivityLogViewModel),
                                  typeof(SettingsViewModel), typeof(AboutViewModel) })
            sc.AddSingleton(t);
        return sc.BuildServiceProvider();
    }

    private static async Task<bool> RunFirstRunAsync(IServiceProvider sp)
    {
        var vm = new FirstRunViewModel(sp.GetRequiredService<ISettingsService>(), sp.GetRequiredService<ImportService>(), sp.GetRequiredService<ThemeService>());
        var win = new FirstRunWindow(vm);
        if (win.ShowDialog() != true) return false;
        await vm.ApplyAsync();
        return true;
    }

    private static async Task PurgeOldLogsAsync(IServiceProvider sp, ISettingsService settings)
    {
        try
        {
            await sp.GetRequiredService<IAuditService>().PurgeOlderThanAsync(settings.Current.LogRetentionDays);
            var cutoff = DateTime.Now.AddDays(-settings.Current.LogRetentionDays);
            foreach (var f in Directory.GetFiles(DatabasePaths.LogsFolder, "crm-master-*.log").Where(f => File.GetLastWriteTime(f) < cutoff))
                try { File.Delete(f); } catch (IOException) { /* in use */ }
        }
        catch (Exception ex) { Log.Warning(ex, "Log retention clean-up failed"); }
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI exception");
        e.Handled = true;
        try
        {
            if (_services?.GetService<IDialogService>() is { } d)
                d.Error("Unexpected error", $"Something unexpected happened, but your data is safe.\n\n{e.Exception.GetType().Name}: {e.Exception.Message}\n\nDetails were saved to the log folder:\n{DatabasePaths.LogsFolder}");
            _services?.GetService<INotificationCenter>()?.Add(NotificationLevel.Error, "Unexpected error", e.Exception.Message);
        }
        catch (Exception) { /* never throw from the error handler */ }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("CRM MASTER exiting");
        Log.CloseAndFlush();
        try { _singleInstance?.ReleaseMutex(); } catch (ApplicationException) { }
        _singleInstance?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }
}
