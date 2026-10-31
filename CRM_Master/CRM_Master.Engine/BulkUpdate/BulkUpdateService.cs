using System.Text.Json;
using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using CRM_Master.Engine.Import;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Engine.BulkUpdate;

/// <summary>Builds update plans from files or stored import proposals and executes only what the user approved.</summary>
public sealed class BulkUpdateService
{
    private readonly IDbContextFactory<CrmDbContext> _factory;
    private readonly ISettingsService _settings;
    private readonly IBackupService _backup;
    private readonly IContactRepository _contacts;

    public BulkUpdateService(IDbContextFactory<CrmDbContext> factory, ISettingsService settings, IBackupService backup, IContactRepository contacts)
    { _factory = factory; _settings = settings; _backup = backup; _contacts = contacts; }

    /// <summary>Reads the file with the given mapping and proposes updates against the current contacts.</summary>
    public async Task<BulkUpdatePlan> PlanFromFileAsync(string path, IReadOnlyList<ColumnMapping> mapping, BulkUpdatePolicy policy, IReadOnlyCollection<ContactField> fields,
        MatchBy matchBy = MatchBy.EmailThenPhone, string? sheet = null, IProgress<ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var errors = ColumnMapper.Validate(mapping);
        if (errors.Count > 0) throw new ImportException(string.Join(" ", errors));
        var table = await TableReader.ReadAsync(path, sheet: sheet, ct: ct);
        var cc = _settings.Current.DefaultCountryCode;
        var existing = await _contacts.GetAllAsync(ct);
        return await Task.Run(() =>
        {
            var rows = new List<IncomingContact>(table.Rows.Count);
            for (var i = 0; i < table.Rows.Count; i++)
            {
                if ((i & 0x3FF) == 0) { ct.ThrowIfCancellationRequested(); progress?.Report(new ProgressInfo("Reading incoming rows", i, table.Rows.Count)); }
                var row = RowConverter.Convert(table.Rows[i], mapping, i + 2, _settings.Current.CleanOnImport, cc);
                if (!row.IsEmpty) rows.Add(new IncomingContact(row.RowNumber, row.Contact));
            }
            progress?.Report(new ProgressInfo("Comparing with existing contacts", 0, 1));
            return BulkUpdatePlanner.Plan(existing, rows, policy, fields, matchBy, cc);
        }, ct);
    }

    /// <summary>Plans from the update proposals an import stored (rows that matched existing contacts).</summary>
    public async Task<BulkUpdatePlan> PlanFromImportProposalsAsync(int importJobId, BulkUpdatePolicy policy, IReadOnlyCollection<ContactField> fields, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var records = await ctx.ImportRecords.AsNoTracking().Where(r => r.ImportJobId == importJobId && r.Status == ImportRecordStatus.Proposed).ToListAsync(ct);
        var rows = new List<IncomingContact>();
        foreach (var r in records)
        {
            var d = JsonSerializer.Deserialize<Dictionary<string, string?>>(r.RawJson ?? "{}") ?? new();
            var c = new Contact();
            foreach (var (k, v) in d) if (Enum.TryParse<ContactField>(k, out var f)) c.Set(f, v);
            rows.Add(new IncomingContact(r.RowNumber, c, r.ContactId));
        }
        var existing = await _contacts.GetAllAsync(ct);
        return BulkUpdatePlanner.Plan(existing, rows, policy, fields, MatchBy.EmailThenPhone, _settings.Current.DefaultCountryCode);
    }

    /// <summary>Applies the approved field changes in one transaction after a backup. Values changed since planning are never overwritten.</summary>
    public async Task<BulkUpdateResult> ApplyAsync(BulkUpdatePlan plan, IProgress<ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var work = plan.Records.Where(r => !r.Skipped && r.Actionable.Any(f => f.Approved)).ToList();
        var batch = Audit.NewBatchId();
        if (work.Count == 0) return new BulkUpdateResult(batch, 0, 0, 0, null);

        var backup = await _backup.CreateAsync(BackupKind.PreOperation, $"Before bulk update of {work.Count} contact(s)", ct);
        var quality = _settings.Current.ToQualityOptions();
        int updated = 0, fields = 0, skipped = 0, done = 0;

        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var companies = await CompanyCache.LoadAsync(ctx, ct);
        foreach (var chunk in work.Chunk(500))
        {
            ct.ThrowIfCancellationRequested();
            var ids = chunk.Select(r => r.ContactId).ToList();
            var loaded = await ctx.Contacts.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
            foreach (var rec in chunk)
            {
                if (!loaded.TryGetValue(rec.ContactId, out var c)) { skipped++; continue; }
                var before = c.Snapshot(); var applied = 0;
                foreach (var fp in rec.Actionable.Where(f => f.Approved))
                {
                    if (c.Get(fp.Field) != fp.Current) { skipped++; continue; }   // edited since the plan was built
                    if (!c.Set(fp.Field, fp.Incoming)) { skipped++; continue; }
                    applied++;
                }
                if (applied == 0) continue;
                ContactPreparer.Prepare(ctx, c, quality, companies);
                c.UpdatedAt = DateTime.UtcNow;
                var diff = ContactDiff.Compute(before, c);
                ctx.AuditLogs.Add(Audit.Entry(AuditOperation.BulkUpdate, "Contact", c.Id, ContactDiff.ToJson(diff, true), ContactDiff.ToJson(diff, false),
                    summary: $"Bulk update ({plan.Policy}) row {rec.RowNumber}: {string.Join(", ", diff.Select(d => d.Field))}", batchId: batch));
                updated++; fields += applied;
            }
            await ctx.SaveChangesAsync(ct);
            ctx.ChangeTracker.Clear();
            progress?.Report(new ProgressInfo("Applying approved changes", done += chunk.Length, work.Count));
        }
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.BulkUpdate, "Batch", null, summary: $"Bulk update applied: {updated} contact(s), {fields} field(s), policy {plan.Policy}, {plan.Unmatched.Count} unmatched row(s)", batchId: batch));
        await ctx.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new BulkUpdateResult(batch, updated, fields, skipped, backup.FilePath);
    }
}
