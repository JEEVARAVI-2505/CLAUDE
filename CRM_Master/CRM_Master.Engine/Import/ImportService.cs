using System.Diagnostics;
using System.Text.Json;
using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using CRM_Master.Engine.Automation;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Engine.Import;

/// <summary>Previews and runs imports. The whole import is one transaction: cancel or failure leaves the contact list untouched.</summary>
public sealed class ImportService
{
    private const int ChunkSize = 1000;
    private readonly IDbContextFactory<CrmDbContext> _factory;
    private readonly ISettingsService _settings;
    private readonly IBackupService? _backup;
    private readonly AutomationService? _automation;
    private readonly IImportReportGenerator? _reports;

    public ImportService(IDbContextFactory<CrmDbContext> factory, ISettingsService settings, IBackupService? backup = null,
        AutomationService? automation = null, IImportReportGenerator? reports = null)
    { _factory = factory; _settings = settings; _backup = backup; _automation = automation; _reports = reports; }

    public ImportOptions DefaultOptions(IReadOnlyList<ColumnMapping> mapping, string? sheet = null) =>
        new(mapping, _settings.Current.CleanOnImport, _settings.Current.ImportDuplicatePolicy, sheet);

    /// <summary>Dry run against the real database: counts every outcome and returns a sample of rows.</summary>
    public async Task<ImportPreview> PreviewAsync(string path, ImportOptions options, int sampleSize = 200, CancellationToken ct = default)
    {
        var preview = new ImportPreview();
        preview.MappingErrors.AddRange(ColumnMapper.Validate(options.Mapping));
        if (preview.MappingErrors.Count > 0) return preview;

        var table = await TableReader.ReadAsync(path, sheet: options.Sheet, ct: ct);
        var (email, phone) = await LoadExistingKeysAsync(ct);
        var cls = new RowClassifier(email, phone, options.Mapping, options.CleanOnImport, _settings.Current.DefaultCountryCode);
        await Task.Run(() =>
        {
            for (var i = 0; i < table.Rows.Count; i++)
            {
                if ((i & 0x3FF) == 0) ct.ThrowIfCancellationRequested();
                var (row, status, existing, msg) = cls.Classify(table.Rows[i], i + 2);   // +2: header is row 1
                preview.Total++;
                switch (status)
                {
                    case PreviewRowStatus.Ready: preview.Ready++; break;
                    case PreviewRowStatus.Warning: preview.Warnings++; break;
                    case PreviewRowStatus.DuplicateExisting: preview.DuplicateExisting++; break;
                    case PreviewRowStatus.DuplicateInFile: preview.DuplicateInFile++; break;
                    case PreviewRowStatus.Invalid: preview.Invalid++; break;
                    case PreviewRowStatus.Empty: preview.Empty++; break;
                }
                if (preview.Sample.Count < sampleSize && status != PreviewRowStatus.Empty) preview.Sample.Add(new PreviewRow(row.RowNumber, status, row.Contact, msg, existing));
            }
        }, ct);
        return preview;
    }

    private async Task<(Dictionary<string, int> Email, Dictionary<string, int> Phone)> LoadExistingKeysAsync(CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var email = new Dictionary<string, int>(); var phone = new Dictionary<string, int>();
        await foreach (var k in ctx.Contacts.AsNoTracking().Select(c => new { c.Id, c.EmailKey, c.PhoneKey, c.AlternatePhoneKey }).AsAsyncEnumerable().WithCancellation(ct))
        {
            if (k.EmailKey is not null) email.TryAdd(k.EmailKey, k.Id);
            if (k.PhoneKey is not null) phone.TryAdd(k.PhoneKey, k.Id);
            if (k.AlternatePhoneKey is not null) phone.TryAdd(k.AlternatePhoneKey, k.Id);
        }
        return (email, phone);
    }

    public async Task<ImportResult> ImportAsync(string path, ImportOptions options, IProgress<ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var fileName = Path.GetFileName(path);
        var mappingErrors = ColumnMapper.Validate(options.Mapping);
        if (mappingErrors.Count > 0) return await FailAsync(path, 0, string.Join(" ", mappingErrors), sw);

        ImportTable table;
        try { progress?.Report(new ProgressInfo("Reading file", 0, 1)); table = await TableReader.ReadAsync(path, sheet: options.Sheet, ct: ct); }
        catch (OperationCanceledException) { return await CancelAsync(path, 0, sw); }
        catch (ImportException ex) { return await FailAsync(path, 0, ex.Message, sw); }

        var s = _settings.Current;
        if (_backup is not null && s.AutoBackupBeforeBulk && await CountContactsAsync(ct) > 0)
        {
            progress?.Report(new ProgressInfo("Backing up database", 0, table.Rows.Count));
            await _backup.CreateAsync(BackupKind.PreOperation, $"Before importing {fileName}", ct);
        }

        var rules = _automation is null ? new List<ParsedRule>() : await _automation.LoadEnabledAsync(ct, AutomationTrigger.ContactImported, AutomationTrigger.ExistingContactMatched, AutomationTrigger.ImportCompleted);
        var ruleStats = new Dictionary<int, (ParsedRule Rule, int Contacts, int Changes)>();
        var quality = s.ToQualityOptions();
        var counts = new Counts();
        int jobId = 0;

        try
        {
            await using var ctx = await _factory.CreateDbContextAsync(ct);
            ctx.ChangeTracker.AutoDetectChangesEnabled = false;
            await using var tx = await ctx.Database.BeginTransactionAsync(ct);

            var job = new ImportJob { FileName = fileName, FilePath = path, FileType = table.FileType, TotalRows = table.Rows.Count, MappingJson = JsonSerializer.Serialize(options.Mapping.Where(m => !m.IsIgnored).Select(m => new { m.SourceHeader, Target = m.TargetLabel })) };
            ctx.ImportJobs.Add(job);
            await ctx.SaveChangesAsync(ct);
            jobId = job.Id;

            var (email, phone) = await LoadExistingKeysAsync(ct);
            var companies = await CompanyCache.LoadAsync(ctx, ct);
            var customIds = await EnsureCustomFieldsAsync(ctx, options.Mapping, ct);
            var cls = new RowClassifier(email, phone, options.Mapping, options.CleanOnImport, quality.DefaultCountryCode);
            var headers = table.Headers;

            for (var i = 0; i < table.Rows.Count; i++)
            {
                if ((i & 0xFF) == 0) ct.ThrowIfCancellationRequested();
                var cells = table.Rows[i];
                var (row, status, existingId, message) = cls.Classify(cells, i + 2);
                var flag = false;

                switch (status)
                {
                    case PreviewRowStatus.Empty: counts.Skipped++; continue;
                    case PreviewRowStatus.Invalid:
                        counts.Failed++; AddRecord(ctx, job.Id, row.RowNumber, ImportRecordStatus.Failed, null, message, Raw(headers, cells)); continue;
                    case PreviewRowStatus.DuplicateExisting:
                    {
                        var matchResult = RuleEvaluator.Run(rules, AutomationTrigger.ExistingContactMatched, row.Contact, quality.DefaultCountryCode);
                        Track(ruleStats, matchResult);
                        if (matchResult.ProposeUpdate || options.DuplicatePolicy == ImportDuplicatePolicy.ProposeUpdate)
                        { counts.Proposed++; counts.Duplicates++; AddRecord(ctx, job.Id, row.RowNumber, ImportRecordStatus.Proposed, existingId, message, Raw(headers, cells, row)); continue; }
                        counts.Duplicates++;
                        if (options.DuplicatePolicy == ImportDuplicatePolicy.Skip) { AddRecord(ctx, job.Id, row.RowNumber, ImportRecordStatus.Duplicate, existingId, message, null); continue; }
                        flag = true; break;
                    }
                    case PreviewRowStatus.DuplicateInFile:
                        counts.Duplicates++;
                        if (options.DuplicatePolicy != ImportDuplicatePolicy.ImportAndFlag) { AddRecord(ctx, job.Id, row.RowNumber, ImportRecordStatus.Duplicate, null, message, null); continue; }
                        flag = true; break;
                }

                var c = row.Contact;
                c.ImportJobId = job.Id; c.CreatedAt = c.UpdatedAt = DateTime.UtcNow;
                var ruleResult = RuleEvaluator.Run(rules, AutomationTrigger.ContactImported, c, quality.DefaultCountryCode);
                Track(ruleStats, ruleResult);
                var ruleSaysIncomplete = c.IsIncomplete;
                ContactPreparer.Prepare(ctx, c, quality, companies);
                if (ruleSaysIncomplete) c.IsIncomplete = true;
                if (flag) { c.IsDuplicateFlagged = true; c.NeedsReview = true; }
                foreach (var (name, value) in row.Custom) if (value is not null) c.CustomValues.Add(new ContactCustomFieldValue { CustomFieldId = customIds[name], Value = value });
                ctx.Contacts.Add(c);
                counts.Imported++;

                if ((i + 1) % ChunkSize == 0)
                {
                    await ctx.SaveChangesAsync(ct);
                    ctx.ChangeTracker.Clear();
                    progress?.Report(new ProgressInfo("Importing", i + 1, table.Rows.Count));
                }
            }
            await ctx.SaveChangesAsync(ct);
            progress?.Report(new ProgressInfo("Finishing", table.Rows.Count, table.Rows.Count));

            var status2 = counts.Failed > 0 || counts.Duplicates > 0 ? ImportJobStatus.CompletedWithWarnings : ImportJobStatus.Completed;
            await ctx.ImportJobs.Where(j => j.Id == job.Id).ExecuteUpdateAsync(u => u
                .SetProperty(j => j.Status, status2).SetProperty(j => j.CompletedAt, DateTime.UtcNow)
                .SetProperty(j => j.ImportedRows, counts.Imported).SetProperty(j => j.SkippedRows, counts.Skipped)
                .SetProperty(j => j.DuplicateRows, counts.Duplicates).SetProperty(j => j.FailedRows, counts.Failed), ct);
            ctx.AuditLogs.Add(Audit.Entry(AuditOperation.Import, "ImportJob", job.Id, null, null,
                status2 == ImportJobStatus.Completed ? OperationStatus.Success : OperationStatus.Warning,
                $"Imported {counts.Imported} of {table.Rows.Count} rows from {fileName} ({counts.Duplicates} duplicates, {counts.Failed} failed, {counts.Proposed} update proposals)", batchId: "import-" + job.Id));
            await ctx.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            string? reportPath = null;
            if (_automation is not null)
            {
                await _automation.RecordAsync(AutomationTrigger.ContactImported, ruleStats.Values, $"import {fileName}", "import-" + job.Id, ct: CancellationToken.None);
                var done = rules.Where(r => r.Trigger == AutomationTrigger.ImportCompleted && r.Definition.Actions.Any(a => a.Type == ActionType.GenerateImportReport)).ToList();
                if (done.Count > 0 && _reports is not null)
                {
                    reportPath = await _reports.GenerateImportSummaryAsync(job.Id, CancellationToken.None);
                    await _automation.RecordAsync(AutomationTrigger.ImportCompleted, done.Select(r => (r, 1, 0)), reportPath is null ? "report not created" : "report saved to " + reportPath, "import-" + job.Id, ct: CancellationToken.None);
                }
            }
            return new ImportResult(job.Id, status2, table.Rows.Count, counts.Imported, counts.Proposed, counts.Skipped, counts.Duplicates, counts.Failed, sw.Elapsed, null, reportPath);
        }
        catch (OperationCanceledException) { return await CancelAsync(path, table.Rows.Count, sw); }
        catch (Exception ex) when (ex is DbUpdateException or IOException or InvalidOperationException)
        {
            return await FailAsync(path, table.Rows.Count, "The import was rolled back because of a database problem: " + (ex.InnerException?.Message ?? ex.Message), sw);
        }
    }

    private sealed class Counts { public int Imported, Proposed, Skipped, Duplicates, Failed; }

    private static void Track(Dictionary<int, (ParsedRule Rule, int Contacts, int Changes)> stats, ContactRuleResult r)
    {
        foreach (var (rule, changes) in r.Fired)
        {
            stats.TryGetValue(rule.Id, out var cur);
            stats[rule.Id] = (rule, cur.Contacts + 1, cur.Changes + changes);
        }
    }

    private static void AddRecord(CrmDbContext ctx, int jobId, int row, ImportRecordStatus st, int? contactId, string msg, string? raw) =>
        ctx.ImportRecords.Add(new ImportRecord { ImportJobId = jobId, RowNumber = row, Status = st, ContactId = contactId, Message = msg, RawJson = raw });

    private static string Raw(List<string> headers, string?[] cells, ConvertedRow? row = null)
    {
        var d = new Dictionary<string, string?>();
        if (row is null) { for (var i = 0; i < headers.Count && i < cells.Length; i++) d[headers[i]] = cells[i]; }
        else
        {
            foreach (var f in Contact.Fields) if (row.Contact.Get(f.Field) is { } v) d[f.Field.ToString()] = v;
            foreach (var (k, v) in row.Custom) d["Custom:" + k] = v;
        }
        return JsonSerializer.Serialize(d);
    }

    private static async Task<Dictionary<string, int>> EnsureCustomFieldsAsync(CrmDbContext ctx, IReadOnlyList<ColumnMapping> mapping, CancellationToken ct)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var existing = await ctx.CustomFields.ToListAsync(ct);
        foreach (var name in mapping.Where(m => m.CustomFieldName is not null).Select(m => m.CustomFieldName!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var f = existing.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (f is null) { f = new CustomField { Name = name }; ctx.CustomFields.Add(f); await ctx.SaveChangesAsync(ct); }
            map[name] = f.Id;
        }
        return map;
    }

    private async Task<int> CountContactsAsync(CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.Contacts.CountAsync(ct);
    }

    // Failure / cancellation are recorded outside the (rolled back) import transaction so the history still shows them.
    private Task<ImportResult> FailAsync(string path, int total, string error, Stopwatch sw) => RecordEndedAsync(path, total, ImportJobStatus.Failed, OperationStatus.Failed, error, sw);
    private Task<ImportResult> CancelAsync(string path, int total, Stopwatch sw) => RecordEndedAsync(path, total, ImportJobStatus.Cancelled, OperationStatus.Cancelled, "Cancelled by user - no records were imported.", sw);

    private async Task<ImportResult> RecordEndedAsync(string path, int total, ImportJobStatus status, OperationStatus opStatus, string message, Stopwatch sw)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var job = new ImportJob { FileName = Path.GetFileName(path), FilePath = path, FileType = Path.GetExtension(path).TrimStart('.'), TotalRows = total, Status = status, CompletedAt = DateTime.UtcNow, ErrorMessage = message };
        ctx.ImportJobs.Add(job);
        await ctx.SaveChangesAsync();
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.Import, "ImportJob", job.Id, status: opStatus, summary: $"Import of {job.FileName} {status.ToString().ToLowerInvariant()}", error: message, batchId: "import-" + job.Id));
        await ctx.SaveChangesAsync();
        return new ImportResult(job.Id, status, total, 0, 0, 0, 0, 0, sw.Elapsed, message);
    }

    public async Task<List<ImportJob>> HistoryAsync(int take = 100, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.ImportJobs.AsNoTracking().OrderByDescending(j => j.StartedAt).ThenByDescending(j => j.Id).Take(take).ToListAsync(ct);
    }

    public async Task<List<ImportRecord>> RecordsAsync(int jobId, ImportRecordStatus? status = null, int take = 1000, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.ImportRecords.AsNoTracking().Where(r => r.ImportJobId == jobId);
        if (status is { } st) q = q.Where(r => r.Status == st);
        return await q.OrderBy(r => r.RowNumber).Take(take).ToListAsync(ct);
    }
}
