using System.Globalization;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Data.Services;

/// <summary>All dashboard numbers come from live queries - nothing is hard-coded.</summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<CrmDbContext> _factory;
    public DashboardService(IDbContextFactory<CrmDbContext> factory) => _factory = factory;

    public async Task<DashboardStats> GetStatsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var since = DateTime.UtcNow.AddDays(-30);
        var stats = new DashboardStats
        {
            TotalContacts = await ctx.Contacts.CountAsync(ct),
            InvalidEmails = await ctx.Contacts.CountAsync(c => c.EmailState == ValidationState.Invalid, ct),
            InvalidPhones = await ctx.Contacts.CountAsync(c => c.PhoneState == ValidationState.Invalid, ct),
            IncompleteRecords = await ctx.Contacts.CountAsync(c => c.IsIncomplete, ct),
            CleanRecords = await ctx.Contacts.CountAsync(c => !c.IsIncomplete && c.EmailState != ValidationState.Invalid && c.PhoneState != ValidationState.Invalid && !c.NeedsReview && !c.IsDuplicateFlagged, ct),
            DuplicateRecords = await ctx.Contacts.CountAsync(c => c.IsDuplicateFlagged ||
                ctx.DuplicateMatches.Any(m => m.Status == DuplicateStatus.Open && (m.ContactAId == c.Id || m.ContactBId == c.Id)), ct),
            NewContactsImported = await ctx.ImportJobs.Where(j => j.StartedAt >= since).SumAsync(j => (int?)j.ImportedRows, ct) ?? 0,
            UpdatedRecords = await ctx.AuditLogs.CountAsync(a => a.Timestamp >= since && a.RecordId != null && a.EntityType == "Contact" &&
                (a.Operation == AuditOperation.ContactUpdated || a.Operation == AuditOperation.BulkUpdate || a.Operation == AuditOperation.DataCleaning), ct),
            FailedOperations = await ctx.AuditLogs.CountAsync(a => a.Timestamp >= since && a.Status == OperationStatus.Failed, ct),
            RecentImports = await ctx.ImportJobs.AsNoTracking().OrderByDescending(j => j.StartedAt).Take(8).ToListAsync(ct),
            RecentAutomation = await ctx.AuditLogs.AsNoTracking().Where(a => a.Operation == AuditOperation.AutomationExecution).OrderByDescending(a => a.Timestamp).Take(8).ToListAsync(ct),
            RecentErrors = await ctx.AuditLogs.AsNoTracking().Where(a => a.Status == OperationStatus.Failed).OrderByDescending(a => a.Timestamp).Take(8).ToListAsync(ct),
        };
        stats.Growth = await GrowthAsync(ctx, ct);
        return stats;
    }

    /// <summary>Cumulative contact count at the end of each of the last 12 calendar months.</summary>
    private static async Task<List<TimePoint>> GrowthAsync(CrmDbContext ctx, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
        var before = await ctx.Contacts.CountAsync(c => c.CreatedAt < start, ct);
        var created = await ctx.Contacts.AsNoTracking().Where(c => c.CreatedAt >= start).Select(c => c.CreatedAt).ToListAsync(ct);
        var perMonth = created.GroupBy(d => d.Year * 12 + d.Month - 1).ToDictionary(g => g.Key, g => g.Count());
        var points = new List<TimePoint>();
        double running = before;
        for (var i = 0; i < 12; i++)
        {
            var m = start.AddMonths(i);
            running += perMonth.GetValueOrDefault(m.Year * 12 + m.Month - 1);
            points.Add(new TimePoint(m.ToString("MMM yy", CultureInfo.InvariantCulture), running));
        }
        return points;
    }
}
