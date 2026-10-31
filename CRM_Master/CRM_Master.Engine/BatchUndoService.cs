using System.Text.Json;
using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Engine;

/// <summary>Result of reverting one audit batch.</summary>
public sealed record UndoResult(int Reverted, int Restored, int Conflicts);

/// <summary>
/// Reverts a logged batch (data cleaning, bulk update, automation run, delete, merge) from the previous values recorded in the audit trail.
/// A field is only reverted if it still holds the value the batch wrote - later edits are never overwritten.
/// </summary>
public sealed class BatchUndoService
{
    private readonly IDbContextFactory<CrmDbContext> _factory;
    private readonly ISettingsService _settings;
    public BatchUndoService(IDbContextFactory<CrmDbContext> factory, ISettingsService settings) { _factory = factory; _settings = settings; }

    private static readonly AuditOperation[] Undoable = { AuditOperation.DataCleaning, AuditOperation.BulkUpdate, AuditOperation.AutomationExecution, AuditOperation.ContactDeleted, AuditOperation.Merge };

    public async Task<bool> CanUndoAsync(string batchId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.AuditLogs.AnyAsync(a => a.BatchId == batchId && a.EntityType == "Contact" && a.PreviousValue != null && Undoable.Contains(a.Operation), ct)
            && !await ctx.AuditLogs.AnyAsync(a => a.BatchId == batchId && a.Summary != null && a.Summary.StartsWith("Undone"), ct);
    }

    public async Task<UndoResult> UndoAsync(string batchId, CancellationToken ct = default)
    {
        var quality = _settings.Current.ToQualityOptions();
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var rows = await ctx.AuditLogs.AsNoTracking().Where(a => a.BatchId == batchId && a.EntityType == "Contact" && a.PreviousValue != null && Undoable.Contains(a.Operation))
            .OrderByDescending(a => a.Id).ToListAsync(ct);
        if (rows.Count == 0) throw new InvalidOperationException("Nothing to undo for this batch.");
        if (await ctx.AuditLogs.AnyAsync(a => a.BatchId == batchId && a.Summary != null && a.Summary.StartsWith("Undone"), ct))
            throw new InvalidOperationException("This batch has already been undone.");

        var companies = await CompanyCache.LoadAsync(ctx, ct);
        int reverted = 0, restored = 0, conflicts = 0;
        var undoBatch = Audit.NewBatchId();

        foreach (var row in rows)
        {
            var prev = JsonSerializer.Deserialize<Dictionary<string, string?>>(row.PreviousValue!) ?? new();
            if (row.NewValue is null)
            {
                // The record was removed by the batch (duplicate removal / delete / merged-away): recreate it from its snapshot.
                var c = new Contact();
                foreach (var (k, v) in prev) if (Enum.TryParse<ContactField>(k, out var f)) c.Set(f, v);
                c.CreatedAt = c.UpdatedAt = DateTime.UtcNow;
                ContactPreparer.Prepare(ctx, c, quality, companies);
                ctx.Contacts.Add(c);
                await ctx.SaveChangesAsync(ct);
                foreach (var (k, v) in prev.Where(p => p.Key.StartsWith("Custom:") && p.Value is not null))
                {
                    var name = k["Custom:".Length..];
                    var cf = await ctx.CustomFields.FirstOrDefaultAsync(f => f.Name == name, ct) ?? ctx.CustomFields.Add(new CustomField { Name = name }).Entity;
                    await ctx.SaveChangesAsync(ct);
                    ctx.ContactCustomFieldValues.Add(new ContactCustomFieldValue { ContactId = c.Id, CustomFieldId = cf.Id, Value = v });
                }
                ctx.AuditLogs.Add(Audit.Entry(AuditOperation.ContactCreated, "Contact", c.Id, null, row.PreviousValue, summary: $"Undone: restored {c.DisplayName} (was contact #{row.RecordId})", batchId: undoBatch));
                restored++;
                continue;
            }

            var current = await ctx.Contacts.FirstOrDefaultAsync(c => c.Id == row.RecordId, ct);
            if (current is null) { conflicts++; continue; }
            var next = JsonSerializer.Deserialize<Dictionary<string, string?>>(row.NewValue) ?? new();
            var before = current.Snapshot(); var any = false;
            foreach (var (k, oldValue) in prev)
            {
                if (!Enum.TryParse<ContactField>(k, out var f)) continue;     // custom-field changes are not auto-reverted
                if (current.Get(f) != (next.GetValueOrDefault(k) is { Length: > 0 } n ? n : null)) { conflicts++; continue; }
                current.Set(f, oldValue); any = true;
            }
            if (!any) continue;
            ContactPreparer.Prepare(ctx, current, quality, companies);
            current.UpdatedAt = DateTime.UtcNow;
            var diff = ContactDiff.Compute(before, current);
            ctx.AuditLogs.Add(Audit.Entry(AuditOperation.ContactUpdated, "Contact", current.Id, ContactDiff.ToJson(diff, true), ContactDiff.ToJson(diff, false), summary: $"Undone: reverted {current.DisplayName}", batchId: undoBatch));
            reverted++;
        }
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.ContactUpdated, "Batch", null, summary: $"Undone batch {batchId}: {reverted} reverted, {restored} restored, {conflicts} left unchanged because they were edited later", batchId: batchId));
        await ctx.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new UndoResult(reverted, restored, conflicts);
    }
}
