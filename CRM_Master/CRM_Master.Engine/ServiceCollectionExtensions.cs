using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using CRM_Master.Engine.Automation;
using CRM_Master.Engine.BulkUpdate;
using CRM_Master.Engine.Cleaning;
using CRM_Master.Engine.DuplicateDetection;
using CRM_Master.Engine.Import;
using CRM_Master.Engine.Licensing;
using CRM_Master.Engine.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CRM_Master.Engine;

/// <summary>Registers every UI-free service. Shared by the WPF app and by tests so a missing registration is caught without a Windows machine.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrmMasterEngine(this IServiceCollection sc, string databaseFile)
    {
        var factory = new SqliteContextFactory(databaseFile);
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
        return sc;
    }
}
