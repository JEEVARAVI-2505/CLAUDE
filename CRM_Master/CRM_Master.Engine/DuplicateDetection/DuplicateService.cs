using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using CRM_Master.Engine.Automation;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Engine.DuplicateDetection;

/// <summary>A persisted duplicate group with its contacts loaded for review.</summary>
public sealed class DuplicateGroupView
{
    public int GroupId { get; init; }
    public MatchClass Class { get; init; }
    public double TopScore { get; init; }
    public DuplicateStatus Status { get; init; }
    public List<Contact> Contacts { get; init; } = new();
    public List<DuplicateMatch> Matches { get; init; } = new();
    public string Title => string.Join("  ~  ", Contacts.Take(3).Select(c => c.DisplayName)) + (Contacts.Count > 3 ? $"  (+{Contacts.Count - 3})" : "");
}

public sealed record MergeResult(string BatchId, int MasterId, int Merged, string? BackupPath);

/// <summary>Scans for duplicates, stores groups, and performs reviewed merges. Uncertain matches are never merged automatically.</summary>
public sealed class DuplicateService
{
    private readonly IDbContextFactory<CrmDbContext> _factory;
    private readonly ISettingsService _settings;
    private readonly IBackupService _backup;
    private readonly IContactRepository _contacts;
    private readonly AutomationService? _automation;

    public DuplicateService(IDbContextFactory<CrmDbContext> factory, ISettingsService settings, IBackupService backup, IContactRepository contacts, AutomationService? automation = null)
    { _factory = factory; _settings = settings; _backup = backup; _contacts = contacts; _automation = automation; }

    public DuplicateOptions DefaultOptions() => DuplicateOptions.FromSettings(_settings.Current);

    public async Task<ScanResult> ScanAsync(DuplicateOptions options, IProgress<ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        progress?.Report(new ProgressInfo("Loading contacts", 0, 1));
        var contacts = await _contacts.GetAllAsync(ct);
        HashSet<(int, int)> separate;
        await using (var c0 = await _factory.CreateDbContextAsync(ct))
            separate = (await c0.DuplicateMatches.AsNoTracking().Where(m => m.Status == DuplicateStatus.MarkedSeparate).Select(m => new { m.ContactAId, m.ContactBId }).ToListAsync(ct))
                .Select(m => (m.ContactAId, m.ContactBId)).ToHashSet();

        var scan = await Task.Run(() => DuplicateDetector.Scan(contacts, options, separate, progress, ct), ct);
        progress?.Report(new ProgressInfo("Saving results", 0, scan.Clusters.Count));

        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        // Replace the previous open results; keep decisions the user already made (merged / separate).
        await ctx.DuplicateGroups.Where(g => g.Status == DuplicateStatus.Open || g.Status == DuplicateStatus.Skipped).ExecuteDeleteAsync(ct);
        await ctx.Contacts.Where(c => c.IsDuplicateFlagged && c.NeedsReview == false).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDuplicateFlagged, false), ct);

        foreach (var chunk in scan.Clusters.Chunk(500))
        {
            foreach (var cl in chunk)
            {
                var g = new DuplicateGroup { Classification = cl.Class, TopScore = cl.TopScore };
                foreach (var p in cl.Pairs)
                    g.Matches.Add(new DuplicateMatch { ContactAId = p.AId, ContactBId = p.BId, Score = p.Score, Classification = p.Class, MatchedOn = p.MatchedOn });
                ctx.DuplicateGroups.Add(g);
            }
            try { await ctx.SaveChangesAsync(ct); }
            catch (DbUpdateException) { throw new InvalidOperationException("Duplicate results conflicted with earlier decisions; please run the scan again."); }
            ctx.ChangeTracker.Clear();
        }
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.DuplicateDetection, "Scan", null,
            summary: $"Scanned {scan.ContactsScanned} contacts: {scan.Clusters.Count} group(s) ({scan.Clusters.Count(c => c.Class == MatchClass.Exact)} exact, {scan.Clusters.Count(c => c.Class == MatchClass.High)} high, {scan.Clusters.Count(c => c.Class == MatchClass.Possible)} possible, {scan.Clusters.Count(c => c.Class == MatchClass.Low)} low)"));
        await ctx.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await RunDuplicateRulesAsync(scan, contacts, ct);
        return scan;
    }

    // "When duplicate email addresses are detected, flag them" - rules with the DuplicateDetected trigger.
    private async Task RunDuplicateRulesAsync(ScanResult scan, IReadOnlyList<Contact> contacts, CancellationToken ct)
    {
        if (_automation is null) return;
        var rules = await _automation.LoadEnabledAsync(ct, AutomationTrigger.DuplicateDetected);
        if (rules.Count == 0) return;
        var byId = contacts.ToDictionary(c => c.Id);
        var ids = scan.Clusters.Where(c => c.Class >= MatchClass.Possible).SelectMany(c => c.ContactIds).Distinct().ToList();
        var stats = new Dictionary<int, (ParsedRule Rule, int Contacts, int Changes)>();
        var flagged = new List<int>(); var cc = _settings.Current.DefaultCountryCode;
        foreach (var id in ids)
        {
            var c = byId[id].Snapshot();
            var res = RuleEvaluator.Run(rules, AutomationTrigger.DuplicateDetected, c, cc);
            foreach (var (rule, ch) in res.Fired) { stats.TryGetValue(rule.Id, out var cur); stats[rule.Id] = (rule, cur.Contacts + 1, cur.Changes + ch); }
            if (c.IsDuplicateFlagged || c.NeedsReview) flagged.Add(id);
        }
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        foreach (var chunk in flagged.Chunk(500))
            await ctx.Contacts.Where(c => chunk.Contains(c.Id)).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDuplicateFlagged, true), ct);
        await _automation.RecordAsync(AutomationTrigger.DuplicateDetected, stats.Values, "duplicate scan", ct: ct);
    }

    public async Task<PagedResult<DuplicateGroupView>> GetGroupsAsync(DuplicateStatus status = DuplicateStatus.Open, MatchClass? minClass = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.DuplicateGroups.AsNoTracking().Where(g => g.Status == status);
        if (minClass is { } mc) q = q.Where(g => g.Classification >= mc);
        var total = await q.CountAsync(ct);
        var groups = await q.Include(g => g.Matches).OrderByDescending(g => g.Classification).ThenByDescending(g => g.TopScore).ThenBy(g => g.Id)
            .Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var ids = groups.SelectMany(g => g.Matches.SelectMany(m => new[] { m.ContactAId, m.ContactBId })).Distinct().ToList();
        var contacts = await _contacts.GetByIdsAsync(ids, ct);
        var map = contacts.ToDictionary(c => c.Id);
        var views = groups.Select(g => new DuplicateGroupView
        {
            GroupId = g.Id, Class = g.Classification, TopScore = g.TopScore, Status = g.Status, Matches = g.Matches.ToList(),
            Contacts = g.Matches.SelectMany(m => new[] { m.ContactAId, m.ContactBId }).Distinct().Where(map.ContainsKey).Select(i => map[i]).OrderBy(c => c.Id).ToList()
        }).ToList();
        return new PagedResult<DuplicateGroupView>(views, total, page, pageSize);
    }

    public async Task<Dictionary<MatchClass, int>> CountsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.DuplicateGroups.AsNoTracking().Where(g => g.Status == DuplicateStatus.Open).GroupBy(g => g.Classification).ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    }

    /// <summary>The user decided these are different customers: remembered so future scans never propose them again.</summary>
    public async Task MarkSeparateAsync(int groupId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var g = await ctx.DuplicateGroups.Include(x => x.Matches).FirstAsync(x => x.Id == groupId, ct);
        g.Status = DuplicateStatus.MarkedSeparate;
        foreach (var m in g.Matches) m.Status = DuplicateStatus.MarkedSeparate;
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.DuplicateDetection, "DuplicateGroup", g.Id, summary: $"Marked group #{g.Id} ({g.Matches.Count} pair(s)) as separate customers"));
        await ctx.SaveChangesAsync(ct);
        await ClearFlagsAsync(ctx, g.Matches.SelectMany(m => new[] { m.ContactAId, m.ContactBId }).Distinct().ToList(), ct);
        await tx.CommitAsync(ct);
    }

    public async Task SkipAsync(int groupId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await ctx.DuplicateGroups.Where(g => g.Id == groupId).ExecuteUpdateAsync(s => s.SetProperty(g => g.Status, DuplicateStatus.Skipped), ct);
    }

    private static async Task ClearFlagsAsync(CrmDbContext ctx, List<int> ids, CancellationToken ct)
    {
        foreach (var id in ids)
        {
            var stillOpen = await ctx.DuplicateMatches.AnyAsync(m => m.Status == DuplicateStatus.Open && (m.ContactAId == id || m.ContactBId == id), ct);
            if (!stillOpen) await ctx.Contacts.Where(c => c.Id == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDuplicateFlagged, false), ct);
        }
    }

    /// <summary>
    /// Merges the other contacts of a group into <paramref name="masterId"/> using the user's field choices.
    /// A backup is taken first; the removed records are stored in the audit log (and the batch can be undone).
    /// </summary>
    public async Task<MergeResult> MergeAsync(int groupId, int masterId, IReadOnlyDictionary<ContactField, string?> chosen, IReadOnlyCollection<int>? mergeIds = null, CancellationToken ct = default)
    {
        var backup = await _backup.CreateAsync(BackupKind.PreOperation, $"Before merging duplicate group #{groupId}", ct);
        var quality = _settings.Current.ToQualityOptions();
        var batch = Audit.NewBatchId();
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);

        var group = await ctx.DuplicateGroups.Include(g => g.Matches).FirstAsync(g => g.Id == groupId, ct);
        var memberIds = group.Matches.SelectMany(m => new[] { m.ContactAId, m.ContactBId }).Distinct().ToList();
        if (!memberIds.Contains(masterId)) throw new ArgumentException("The master record is not part of this group.");
        var loserIds = (mergeIds ?? memberIds).Where(id => id != masterId && memberIds.Contains(id)).ToList();
        if (loserIds.Count == 0) throw new ArgumentException("Select at least one other record to merge into the master.");

        var master = await ctx.Contacts.Include(c => c.CustomValues).FirstAsync(c => c.Id == masterId, ct);
        var losers = await ctx.Contacts.Include(c => c.CustomValues).ThenInclude(v => v.CustomField).Where(c => loserIds.Contains(c.Id)).ToListAsync(ct);
        var before = master.Snapshot();
        foreach (var (field, value) in chosen) master.Set(field, value);
        var companies = await CompanyCache.LoadAsync(ctx, ct);
        ContactPreparer.Prepare(ctx, master, quality, companies);
        master.UpdatedAt = DateTime.UtcNow; master.IsDuplicateFlagged = false;

        foreach (var l in losers)
        {
            foreach (var v in l.CustomValues.Where(v => !master.CustomValues.Any(m => m.CustomFieldId == v.CustomFieldId)))
                master.CustomValues.Add(new ContactCustomFieldValue { CustomFieldId = v.CustomFieldId, Value = v.Value });
            ctx.AuditLogs.Add(Audit.Entry(AuditOperation.Merge, "Contact", l.Id, ContactDiff.SnapshotJson(l), null, summary: $"Merged {l.DisplayName} into contact #{masterId}", batchId: batch));
        }
        var diff = ContactDiff.Compute(before, master);
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.Merge, "Contact", master.Id, ContactDiff.ToJson(diff, true), ContactDiff.ToJson(diff, false), summary: $"Merged {losers.Count} duplicate(s) into {master.DisplayName}", batchId: batch));
        ctx.Contacts.RemoveRange(losers);
        group.Status = DuplicateStatus.Merged; group.MasterContactId = masterId;
        await ctx.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new MergeResult(batch, masterId, losers.Count, backup.FilePath);
    }

    /// <summary>Merges every open Exact group using the default (most complete record as master). Needs explicit user confirmation in the UI.</summary>
    public async Task<(int Groups, int Records)> MergeAllExactAsync(IProgress<ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        int groups = 0, records = 0, done = 0;
        var page = await GetGroupsAsync(DuplicateStatus.Open, MatchClass.Exact, 1, 100000, ct);
        var exact = page.Items.Where(g => g.Class == MatchClass.Exact && g.Contacts.Count >= 2).ToList();
        foreach (var g in exact)
        {
            ct.ThrowIfCancellationRequested();
            var master = MergePlanner.SuggestMaster(g.Contacts);
            var plan = MergePlanner.Plan(master, g.Contacts.Where(c => c.Id != master.Id).ToList());
            var res = await MergeAsync(g.GroupId, master.Id, plan.ToDictionary(p => p.Field, p => p.Selected), null, ct);
            groups++; records += res.Merged;
            progress?.Report(new ProgressInfo("Merging exact duplicates", ++done, exact.Count));
        }
        return (groups, records);
    }
}
