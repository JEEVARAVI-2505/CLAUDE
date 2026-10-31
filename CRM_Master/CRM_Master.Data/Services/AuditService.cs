using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Data.Services;

/// <summary>Writes and queries the audit trail.</summary>
public sealed class AuditService : IAuditService
{
    private readonly IDbContextFactory<CrmDbContext> _factory;
    public AuditService(IDbContextFactory<CrmDbContext> factory) => _factory = factory;

    public async Task WriteAsync(AuditLog entry, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        ctx.AuditLogs.Add(entry);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditLog>> QueryAsync(AuditQuery q, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var query = Filter(ctx.AuditLogs.AsNoTracking(), q);
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, q.Page); var size = Math.Clamp(q.PageSize, 1, 5000);
        var items = await query.OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<AuditLog>(items, total, page, size);
    }

    public async Task<List<AuditLog>> QueryAllAsync(AuditQuery q, int maxRows, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await Filter(ctx.AuditLogs.AsNoTracking(), q).OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id).Take(maxRows).ToListAsync(ct);
    }

    public async Task<int> PurgeOlderThanAsync(int retentionDays, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, retentionDays));
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.AuditLogs.Where(a => a.Timestamp < cutoff).ExecuteDeleteAsync(ct);
    }

    internal static IQueryable<AuditLog> Filter(IQueryable<AuditLog> s, AuditQuery q)
    {
        if (q.Operation is { } op) s = s.Where(a => a.Operation == op);
        if (q.Status is { } st) s = s.Where(a => a.Status == st);
        if (q.FromUtc is { } from) s = s.Where(a => a.Timestamp >= from);
        if (q.ToUtc is { } to) s = s.Where(a => a.Timestamp <= to);
        if (q.RecordId is { } rid) s = s.Where(a => a.RecordId == rid);
        if (!string.IsNullOrWhiteSpace(q.EntityType)) s = s.Where(a => a.EntityType == q.EntityType);
        if (!string.IsNullOrWhiteSpace(q.BatchId)) s = s.Where(a => a.BatchId == q.BatchId);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pat = "%" + ContactRepository.EscapeLike(q.Search.Trim()) + "%";
            s = s.Where(a => EF.Functions.Like(a.Summary ?? "", pat, "\\") || EF.Functions.Like(a.PreviousValue ?? "", pat, "\\")
                          || EF.Functions.Like(a.NewValue ?? "", pat, "\\") || EF.Functions.Like(a.ErrorDetails ?? "", pat, "\\"));
        }
        return s;
    }
}

/// <summary>Convenience factory for audit rows.</summary>
public static class Audit
{
    public static AuditLog Entry(AuditOperation op, string entityType = "Contact", long? recordId = null, string? previous = null, string? next = null,
        OperationStatus status = OperationStatus.Success, string? summary = null, string? error = null, string? batchId = null) =>
        new() { Operation = op, EntityType = entityType, RecordId = recordId, PreviousValue = previous, NewValue = next, Status = status, Summary = summary, ErrorDetails = error, BatchId = batchId };

    public static string NewBatchId() => Guid.NewGuid().ToString("N")[..12];
}
