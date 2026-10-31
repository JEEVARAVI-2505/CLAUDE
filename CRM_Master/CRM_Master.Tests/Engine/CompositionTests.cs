using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Engine;
using CRM_Master.Engine.Automation;
using CRM_Master.Engine.BulkUpdate;
using CRM_Master.Engine.Cleaning;
using CRM_Master.Engine.DuplicateDetection;
using CRM_Master.Engine.Import;
using CRM_Master.Engine.Reporting;
using CRM_Master.Integrations;
using CRM_Master.Integrations.Interfaces;
using CRM_Master.Integrations.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CRM_Master.Tests.Engine;

/// <summary>Proves the application's dependency-injection graph resolves and the real start-up sequence works (the WPF app uses the same registration).</summary>
public class CompositionTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "crm-master-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); try { Directory.Delete(_dir, true); } catch (IOException) { } }

    [Fact]
    public async Task Every_engine_service_resolves_and_the_startup_sequence_runs()
    {
        var sc = new ServiceCollection();
        sc.AddCrmMasterEngine(Path.Combine(_dir, "app.db"));
        sc.AddSingleton<IConnectorRegistry, ConnectorRegistry>();
        sc.AddSingleton<ICredentialStore>(_ => new DpapiCredentialStore(Path.Combine(_dir, "cred")));
        await using var sp = sc.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        // same order as App.OnStartup
        await DatabaseInitializer.InitializeAsync(sp.GetRequiredService<IDbContextFactory<CrmDbContext>>());
        var settings = sp.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();
        await sp.GetRequiredService<AutomationService>().SeedDefaultsAsync();

        foreach (var t in new[] { typeof(IContactRepository), typeof(IAuditService), typeof(IBackupService), typeof(IDashboardService), typeof(ImportService), typeof(CleaningService),
                                  typeof(DuplicateService), typeof(BulkUpdateService), typeof(BatchUndoService), typeof(ReportService), typeof(IImportReportGenerator),
                                  typeof(ILicenseProvider), typeof(IConnectorRegistry), typeof(ICredentialStore) })
            sp.GetRequiredService(t).Should().NotBeNull(t.Name);

        // the import service must have been wired with automation + reporting (rule 6 depends on it)
        sp.GetRequiredService<ReportService>().Should().BeSameAs(sp.GetRequiredService<IImportReportGenerator>());
        (await sp.GetRequiredService<IDashboardService>().GetStatsAsync()).IsEmpty.Should().BeTrue();
        (await sp.GetRequiredService<AutomationService>().ListAsync()).Should().HaveCount(6);
        settings.Current.FirstRunCompleted.Should().BeFalse();   // fresh profile -> the first-run dialog would show
    }

    [Fact]
    public async Task First_run_flow_with_sample_data_through_the_container()
    {
        var sc = new ServiceCollection(); sc.AddCrmMasterEngine(Path.Combine(_dir, "app.db"));
        await using var sp = sc.BuildServiceProvider();
        await DatabaseInitializer.InitializeAsync(sp.GetRequiredService<IDbContextFactory<CrmDbContext>>());
        var settings = sp.GetRequiredService<ISettingsService>(); await settings.LoadAsync();
        await sp.GetRequiredService<AutomationService>().SeedDefaultsAsync();

        var s = settings.Current.Clone(); s.FirstRunCompleted = true; s.FirstRunUtc = DateTime.UtcNow; s.BackupFolder = Path.Combine(_dir, "b"); s.ExportFolder = Path.Combine(_dir, "e");
        await settings.SaveAsync(s);

        var sample = Path.Combine(AppContext.BaseDirectory, "SampleData", "sample-contacts.csv");
        var import = sp.GetRequiredService<ImportService>();
        var res = await import.ImportAsync(sample, import.DefaultOptions(ColumnMapper.Suggest(await TableReader.ReadAsync(sample, 50))));
        res.Imported.Should().BeGreaterThan(90);

        var stats = await sp.GetRequiredService<IDashboardService>().GetStatsAsync();
        (stats.TotalContacts, stats.NewContactsImported).Should().Be((res.Imported, res.Imported));
        stats.InvalidEmails.Should().BeGreaterThan(0);
        stats.RecentImports.Should().ContainSingle();

        sp.GetRequiredService<ILicenseProvider>().GetStatus(DateTime.UtcNow).DaysRemaining.Should().Be(30);
        // a restart (new provider on the same file) sees the same data and settings
        await sp.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        var sc2 = new ServiceCollection(); sc2.AddCrmMasterEngine(Path.Combine(_dir, "app.db"));
        await using var sp2 = sc2.BuildServiceProvider();
        await DatabaseInitializer.InitializeAsync(sp2.GetRequiredService<IDbContextFactory<CrmDbContext>>());
        await sp2.GetRequiredService<ISettingsService>().LoadAsync();
        sp2.GetRequiredService<ISettingsService>().Current.FirstRunCompleted.Should().BeTrue();
        (await sp2.GetRequiredService<IContactRepository>().CountAsync()).Should().Be(res.Imported);
    }
}
