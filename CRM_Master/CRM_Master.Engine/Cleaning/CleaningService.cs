using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Engine.Cleaning;

/// <summary>Analyses and applies data cleaning. Applying always takes a backup first and logs previous values so it can be undone.</summary>
public sealed class CleaningService
{
    private readonly IDbContextFactory<CrmDbContext> _factory;
    private readonly ISettingsService _settings;
    private readonly IBackupService _backup;
    private readonly IContactRepository _contacts;

    public CleaningService(IDbContextFactory<CrmDbContext> factory, ISettingsService settings, IBackupService backup, IContactRepository contacts)
    { _factory = factory; _settings = settings; _backup = backup; _contacts = contacts; }

    public CleaningOptions DefaultOptions() => new() { DefaultCountryCode = _settings.Current.DefaultCountryCode };

    public async Task<CleaningReport> AnalyzeAsync(CleaningOptions options, IProgress<ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var all = await _contacts.GetAllAsync(ct);
        return await Task.Run(() => CleaningEngine.Analyze(all, options, progress, ct), ct);
    }

    public async Task<CleaningApplyResult> ApplyAsync(CleaningReport report, CleaningOptions options, bool removeDuplicates, IProgress<ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var batch = Audit.NewBatchId();
        var quality = _settings.Current.ToQualityOptions();
        var backup = await _backup.CreateAsync(BackupKind.PreOperation, "Before applying data cleaning", ct);

        var byContact = report.Changes.GroupBy(c => c.ContactId).ToList();
        int updated = 0, fields = 0, removed = 0, skipped = 0, done = 0;
        var total = byContact.Count + (removeDuplicates ? report.RemovableDuplicates : 0);

        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var companies = await CompanyCache.LoadAsync(ctx, ct);

        foreach (var chunk in byContact.Chunk(500))
        {
            ct.ThrowIfCancellationRequested();
            var ids = chunk.Select(g => g.Key).ToList();
            var loaded = await ctx.Contacts.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
            foreach (var g in chunk)
            {
                if (!loaded.TryGetValue(g.Key, out var c)) { skipped++; continue; }
                var before = c.Snapshot(); var applied = 0;
                foreach (var ch in g)
                {
                    if (c.Get(ch.Field) != ch.Before) { skipped++; continue; }   // edited since the analysis: never overwrite newer data
                    c.Set(ch.Field, ch.After); applied++;
                }
                if (applied == 0) continue;
                ContactPreparer.Prepare(ctx, c, quality, companies);
                c.UpdatedAt = DateTime.UtcNow;
                var diff = ContactDiff.Compute(before, c);
                ctx.AuditLogs.Add(Audit.Entry(AuditOperation.DataCleaning, "Contact", c.Id, ContactDiff.ToJson(diff, true), ContactDiff.ToJson(diff, false), summary: $"Cleaned {c.DisplayName}: {string.Join(", ", diff.Select(d => d.Field))}", batchId: batch));
                updated++; fields += applied;
            }
            await ctx.SaveChangesAsync(ct);
            ctx.ChangeTracker.Clear();
            done += chunk.Length; progress?.Report(new ProgressInfo("Applying changes", done, total));
        }

        if (removeDuplicates)
            foreach (var group in report.ExactDuplicateGroups)
            {
                ct.ThrowIfCancellationRequested();
                var keep = group[0];
                foreach (var dupId in group.Skip(1))
                {
                    var d = await ctx.Contacts.FirstOrDefaultAsync(c => c.Id == dupId, ct);
                    if (d is null) continue;
                    ctx.AuditLogs.Add(Audit.Entry(AuditOperation.DataCleaning, "Contact", d.Id, ContactDiff.SnapshotJson(d), null, summary: $"Removed exact duplicate of contact #{keep}: {d.DisplayName}", batchId: batch));
                    ctx.Contacts.Remove(d); removed++; done++;
                }
                await ctx.SaveChangesAsync(ct);
                ctx.ChangeTracker.Clear();
                progress?.Report(new ProgressInfo("Removing exact duplicates", done, total));
            }

        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.DataCleaning, "Batch", null, summary: $"Cleaning applied: {updated} contact(s), {fields} field(s), {removed} duplicate(s) removed", batchId: batch));
        await ctx.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new CleaningApplyResult(batch, updated, fields, removed, skipped, backup.FilePath);
    }
}
