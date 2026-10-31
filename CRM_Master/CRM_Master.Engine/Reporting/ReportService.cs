using System.Globalization;
using System.Text.Json;
using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using CRM_Master.Engine.Import;
using CRM_Master.Engine.Validation;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Engine.Reporting;

/// <summary>Builds the seven standard reports from the live database and exports them. Every export is audited.</summary>
public sealed class ReportService : IImportReportGenerator
{
    private const int MaxRows = 200_000;
    private readonly IDbContextFactory<CrmDbContext> _factory;
    private readonly ISettingsService _settings;
    public ReportService(IDbContextFactory<CrmDbContext> factory, ISettingsService settings) { _factory = factory; _settings = settings; }

    public static string Title(ReportType t) => t switch
    {
        ReportType.ImportSummary => "Import Summary", ReportType.Duplicates => "Duplicate Report", ReportType.DataQuality => "Data Quality Report",
        ReportType.InvalidRecords => "Invalid Records Report", ReportType.BulkUpdate => "Bulk Update Report",
        ReportType.AutomationExecution => "Automation Execution Report", _ => "Audit Report"
    };

    public Task<ReportData> BuildAsync(ReportType type, ReportFilter filter, CancellationToken ct = default) => type switch
    {
        ReportType.ImportSummary => ImportSummaryAsync(filter, ct),
        ReportType.Duplicates => DuplicatesAsync(filter, ct),
        ReportType.DataQuality => DataQualityAsync(filter, ct),
        ReportType.InvalidRecords => InvalidRecordsAsync(filter, ct),
        ReportType.BulkUpdate => BulkUpdateAsync(filter, ct),
        ReportType.AutomationExecution => AutomationAsync(filter, ct),
        _ => AuditAsync(filter, ct),
    };

    private ReportData New(ReportType t, ReportFilter f)
    {
        var r = new ReportData { Title = Title(t), Footer = "CRM MASTER - local report" };
        var parts = new List<string>();
        if (f.FromUtc is { } a) parts.Add("from " + a.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (f.ToUtc is { } b) parts.Add("to " + b.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(f.ContactCategory)) parts.Add("category: " + f.ContactCategory);
        if (f.ProcessingStatus is { } s) parts.Add("status: " + s);
        if (!string.IsNullOrWhiteSpace(f.BatchId)) parts.Add("batch: " + f.BatchId);
        r.Subtitle = parts.Count == 0 ? "All records" : "Filters - " + string.Join(", ", parts);
        return r;
    }

    private static ImportJobStatus[] JobStatuses(OperationStatus s) => s switch
    {
        OperationStatus.Success => new[] { ImportJobStatus.Completed }, OperationStatus.Warning => new[] { ImportJobStatus.CompletedWithWarnings },
        OperationStatus.Failed => new[] { ImportJobStatus.Failed }, _ => new[] { ImportJobStatus.Cancelled }
    };

    // 1 -----------------------------------------------------------------------------------------------
    private async Task<ReportData> ImportSummaryAsync(ReportFilter f, CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.ImportJobs.AsNoTracking().AsQueryable();
        if (f.FromUtc is { } from) q = q.Where(j => j.StartedAt >= from);
        if (f.ToUtc is { } to) q = q.Where(j => j.StartedAt <= to);
        if (f.ProcessingStatus is { } ps) { var st = JobStatuses(ps); q = q.Where(j => st.Contains(j.Status)); }
        var jobs = await q.OrderByDescending(j => j.StartedAt).Take(MaxRows).ToListAsync(ct);

        var r = New(ReportType.ImportSummary, f);
        r.Summary.Add(("Imports", jobs.Count.ToString(CultureInfo.InvariantCulture)));
        r.Summary.Add(("Rows read", jobs.Sum(j => j.TotalRows).ToString("N0", CultureInfo.InvariantCulture)));
        r.Summary.Add(("Contacts imported", jobs.Sum(j => j.ImportedRows).ToString("N0", CultureInfo.InvariantCulture)));
        r.Summary.Add(("Duplicates found", jobs.Sum(j => j.DuplicateRows).ToString("N0", CultureInfo.InvariantCulture)));
        r.Summary.Add(("Rows failed", jobs.Sum(j => j.FailedRows).ToString("N0", CultureInfo.InvariantCulture)));
        r.Columns.AddRange(new[] { "Job", "File", "Type", "Started", "Status", "Rows", "Imported", "Duplicates", "Failed", "Skipped", "Error" });
        foreach (var j in jobs) r.Rows.Add(new object?[] { j.Id, j.FileName, j.FileType, j.StartedAt.ToLocalTime(), j.Status.ToString(), j.TotalRows, j.ImportedRows, j.DuplicateRows, j.FailedRows, j.SkippedRows, j.ErrorMessage });
        return r;
    }

    /// <summary>Report for one import, listing every row that was not imported and why. Saved as Excel in the export folder.</summary>
    public async Task<string?> GenerateImportSummaryAsync(int importJobId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var job = await ctx.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == importJobId, ct);
        if (job is null) return null;
        var records = await ctx.ImportRecords.AsNoTracking().Where(x => x.ImportJobId == importJobId).OrderBy(x => x.RowNumber).Take(MaxRows).ToListAsync(ct);
        var r = new ReportData { Title = $"Import Report - {job.FileName}", Subtitle = $"Job #{job.Id} started {job.StartedAt.ToLocalTime():yyyy-MM-dd HH:mm}", Footer = "CRM MASTER - local report" };
        r.Summary.Add(("Status", job.Status.ToString())); r.Summary.Add(("Rows read", job.TotalRows.ToString(CultureInfo.InvariantCulture)));
        r.Summary.Add(("Imported", job.ImportedRows.ToString(CultureInfo.InvariantCulture))); r.Summary.Add(("Duplicates", job.DuplicateRows.ToString(CultureInfo.InvariantCulture)));
        r.Summary.Add(("Failed", job.FailedRows.ToString(CultureInfo.InvariantCulture)));
        r.Columns.AddRange(new[] { "Row", "Outcome", "Existing contact", "Reason" });
        foreach (var x in records) r.Rows.Add(new object?[] { x.RowNumber, x.Status.ToString(), x.ContactId, x.Message });
        var path = Path.Combine(_settings.Current.ExportFolder, $"import-report-{job.Id}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        await ExportAsync(r, ExportFormat.Excel, path, ct);
        return path;
    }

    // 2 -----------------------------------------------------------------------------------------------
    private async Task<ReportData> DuplicatesAsync(ReportFilter f, CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.DuplicateMatches.AsNoTracking().Include(m => m.Group).AsQueryable();
        if (f.FromUtc is { } from) q = q.Where(m => m.Group!.CreatedAt >= from);
        if (f.ToUtc is { } to) q = q.Where(m => m.Group!.CreatedAt <= to);
        var matches = await q.OrderByDescending(m => m.Classification).ThenByDescending(m => m.Score).Take(MaxRows).ToListAsync(ct);
        var ids = matches.SelectMany(m => new[] { m.ContactAId, m.ContactBId }).Distinct().ToList();
        var contacts = new Dictionary<int, Contact>();
        foreach (var chunk in ids.Chunk(500)) foreach (var c in await ctx.Contacts.AsNoTracking().Where(c => chunk.Contains(c.Id)).ToListAsync(ct)) contacts[c.Id] = c;
        if (!string.IsNullOrWhiteSpace(f.ContactCategory))
            matches = matches.Where(m => contacts.TryGetValue(m.ContactAId, out var a) && a.Status == f.ContactCategory || contacts.TryGetValue(m.ContactBId, out var b) && b.Status == f.ContactCategory).ToList();

        var r = New(ReportType.Duplicates, f);
        r.Summary.Add(("Duplicate pairs", matches.Count.ToString(CultureInfo.InvariantCulture)));
        foreach (var cls in new[] { MatchClass.Exact, MatchClass.High, MatchClass.Possible, MatchClass.Low })
            r.Summary.Add(($"  {cls}", matches.Count(m => m.Classification == cls).ToString(CultureInfo.InvariantCulture)));
        r.Summary.Add(("Note", "Matches are statistical suggestions. Review each pair before merging."));
        r.Columns.AddRange(new[] { "Group", "Class", "Score", "Matched on", "Review status", "ID A", "Name A", "Email A", "Phone A", "Company A", "ID B", "Name B", "Email B", "Phone B", "Company B" });
        foreach (var m in matches)
        {
            contacts.TryGetValue(m.ContactAId, out var a); contacts.TryGetValue(m.ContactBId, out var b);
            r.Rows.Add(new object?[] { m.DuplicateGroupId, m.Classification.ToString(), Math.Round(m.Score, 3), m.MatchedOn, m.Status.ToString(), m.ContactAId, a?.DisplayName, a?.Email, a?.Phone, a?.CompanyName, m.ContactBId, b?.DisplayName, b?.Email, b?.Phone, b?.CompanyName });
        }
        return r;
    }

    // 3 -----------------------------------------------------------------------------------------------
    private async Task<ReportData> DataQualityAsync(ReportFilter f, CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.Contacts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(f.ContactCategory)) q = q.Where(c => c.Status == f.ContactCategory);
        if (f.FromUtc is { } from) q = q.Where(c => c.CreatedAt >= from);
        if (f.ToUtc is { } to) q = q.Where(c => c.CreatedAt <= to);
        var contacts = await q.Take(MaxRows).ToListAsync(ct);
        var total = contacts.Count;
        string Pct(int n) => total == 0 ? "-" : (100.0 * n / total).ToString("0.0", CultureInfo.InvariantCulture) + "%";

        var r = New(ReportType.DataQuality, f);
        var clean = contacts.Count(c => !c.IsIncomplete && c.EmailState != ValidationState.Invalid && c.PhoneState != ValidationState.Invalid && !c.NeedsReview && !c.IsDuplicateFlagged);
        r.Summary.Add(("Contacts analysed", total.ToString("N0", CultureInfo.InvariantCulture)));
        r.Summary.Add(("Clean records", $"{clean:N0} ({Pct(clean)})"));
        r.Summary.Add(("Note", "Email checks are syntax-only; they do not prove a mailbox exists."));
        r.Columns.AddRange(new[] { "Section", "Metric", "Count", "Percent" });
        void Row(string sec, string metric, int n) => r.Rows.Add(new object?[] { sec, metric, n, Pct(n) });
        Row("Status", "Invalid email", contacts.Count(c => c.EmailState == ValidationState.Invalid));
        Row("Status", "Missing email", contacts.Count(c => c.EmailState == ValidationState.Missing));
        Row("Status", "Invalid phone", contacts.Count(c => c.PhoneState == ValidationState.Invalid));
        Row("Status", "Missing phone", contacts.Count(c => c.PhoneState == ValidationState.Missing));
        Row("Status", "Incomplete records", contacts.Count(c => c.IsIncomplete));
        Row("Status", "Flagged for review", contacts.Count(c => c.NeedsReview));
        Row("Status", "Flagged as duplicate", contacts.Count(c => c.IsDuplicateFlagged));
        foreach (var fi in Contact.Fields) Row("Completeness (filled)", fi.Label, contacts.Count(c => c.Get(fi.Field) is not null));

        var rules = ValidationRules.FromSettings(_settings.Current);
        var summary = await Task.Run(() => ValidationEngine.ValidateAll(contacts, rules, null, ct), ct);
        foreach (var (code, n) in summary.ByCode.OrderByDescending(k => k.Value)) Row("Validation findings", code, n);
        return r;
    }

    // 4 -----------------------------------------------------------------------------------------------
    private async Task<ReportData> InvalidRecordsAsync(ReportFilter f, CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.Contacts.AsNoTracking().Where(c => c.EmailState == ValidationState.Invalid || c.PhoneState == ValidationState.Invalid || c.IsIncomplete || c.NeedsReview);
        if (!string.IsNullOrWhiteSpace(f.ContactCategory)) q = q.Where(c => c.Status == f.ContactCategory);
        if (f.FromUtc is { } from) q = q.Where(c => c.CreatedAt >= from);
        if (f.ToUtc is { } to) q = q.Where(c => c.CreatedAt <= to);
        var contacts = await q.OrderBy(c => c.Id).Take(MaxRows).ToListAsync(ct);
        var rules = ValidationRules.FromSettings(_settings.Current);

        var r = New(ReportType.InvalidRecords, f);
        r.Summary.Add(("Records needing attention", contacts.Count.ToString("N0", CultureInfo.InvariantCulture)));
        r.Columns.AddRange(new[] { "ID", "Name", "Email", "Phone", "Company", "Status", "Problems", "Suggested fix" });
        foreach (var c in contacts)
        {
            var issues = ValidationEngine.ValidateContact(c, rules).Where(i => i.Severity != Severity.Info).ToList();
            var problems = issues.Select(i => i.Message).ToList();
            if (c.NeedsReview) problems.Add("Flagged for review");
            if (c.IsDuplicateFlagged) problems.Add("Flagged as possible duplicate");
            var fix = issues.FirstOrDefault(i => i.Suggestion is not null);
            r.Rows.Add(new object?[] { c.Id, c.DisplayName, c.Email, c.Phone, c.CompanyName, c.Status, string.Join("; ", problems), fix is null ? null : $"{Contact.LabelOf(fix.Field!.Value)}: {fix.Suggestion}" });
        }
        return r;
    }

    // 5 -----------------------------------------------------------------------------------------------
    private async Task<ReportData> BulkUpdateAsync(ReportFilter f, CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.AuditLogs.AsNoTracking().Where(a => a.Operation == AuditOperation.BulkUpdate && a.EntityType == "Contact");
        if (!string.IsNullOrWhiteSpace(f.BatchId)) q = q.Where(a => a.BatchId == f.BatchId);
        if (f.FromUtc is { } from) q = q.Where(a => a.Timestamp >= from);
        if (f.ToUtc is { } to) q = q.Where(a => a.Timestamp <= to);
        if (f.ProcessingStatus is { } s) q = q.Where(a => a.Status == s);
        var logs = await q.OrderBy(a => a.Timestamp).ThenBy(a => a.Id).Take(MaxRows).ToListAsync(ct);

        var r = New(ReportType.BulkUpdate, f);
        r.Columns.AddRange(new[] { "Time", "Batch", "Contact ID", "Field", "Previous value", "New value" });
        var contacts = new HashSet<long?>(); int fields = 0;
        foreach (var a in logs)
        {
            var prev = Parse(a.PreviousValue); var next = Parse(a.NewValue);
            contacts.Add(a.RecordId);
            foreach (var k in next.Keys.Union(prev.Keys)) { r.Rows.Add(new object?[] { a.Timestamp.ToLocalTime(), a.BatchId, a.RecordId, k, prev.GetValueOrDefault(k), next.GetValueOrDefault(k) }); fields++; }
        }
        r.Summary.Add(("Contacts updated", contacts.Count.ToString(CultureInfo.InvariantCulture)));
        r.Summary.Add(("Field changes", fields.ToString(CultureInfo.InvariantCulture)));
        r.Summary.Add(("Batches", logs.Select(l => l.BatchId).Distinct().Count().ToString(CultureInfo.InvariantCulture)));
        return r;
    }

    private static Dictionary<string, string?> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? new(); } catch (JsonException) { return new(); }
    }

    // 6 -----------------------------------------------------------------------------------------------
    private async Task<ReportData> AutomationAsync(ReportFilter f, CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.AutomationExecutions.AsNoTracking().Include(e => e.Rule).AsQueryable();
        if (f.FromUtc is { } from) q = q.Where(e => e.ExecutedAt >= from);
        if (f.ToUtc is { } to) q = q.Where(e => e.ExecutedAt <= to);
        if (f.ProcessingStatus is { } s) q = q.Where(e => e.Status == s);
        var rows = await q.OrderByDescending(e => e.ExecutedAt).ThenByDescending(e => e.Id).Take(MaxRows).ToListAsync(ct);
        var r = New(ReportType.AutomationExecution, f);
        r.Summary.Add(("Executions", rows.Count.ToString(CultureInfo.InvariantCulture)));
        r.Summary.Add(("Contacts affected", rows.Sum(e => e.AffectedCount).ToString("N0", CultureInfo.InvariantCulture)));
        r.Columns.AddRange(new[] { "Time", "Rule", "Trigger", "Status", "Contacts", "Details" });
        foreach (var e in rows) r.Rows.Add(new object?[] { e.ExecutedAt.ToLocalTime(), e.Rule?.Name, e.Trigger.ToString(), e.Status.ToString(), e.AffectedCount, e.Message });
        return r;
    }

    // 7 -----------------------------------------------------------------------------------------------
    private async Task<ReportData> AuditAsync(ReportFilter f, CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var rows = await AuditServiceFilter(ctx, f).OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id).Take(MaxRows).ToListAsync(ct);
        var r = New(ReportType.Audit, f);
        r.Summary.Add(("Entries", rows.Count.ToString("N0", CultureInfo.InvariantCulture)));
        r.Summary.Add(("Failed", rows.Count(a => a.Status == OperationStatus.Failed).ToString(CultureInfo.InvariantCulture)));
        r.Columns.AddRange(new[] { "Time", "Operation", "Entity", "Record", "Status", "Summary", "Previous value", "New value", "Error" });
        foreach (var a in rows) r.Rows.Add(new object?[] { a.Timestamp.ToLocalTime(), a.Operation.ToString(), a.EntityType, a.RecordId, a.Status.ToString(), a.Summary, a.PreviousValue, a.NewValue, a.ErrorDetails });
        return r;
    }

    private static IQueryable<AuditLog> AuditServiceFilter(CrmDbContext ctx, ReportFilter f)
    {
        var q = ctx.AuditLogs.AsNoTracking().AsQueryable();
        if (f.FromUtc is { } from) q = q.Where(a => a.Timestamp >= from);
        if (f.ToUtc is { } to) q = q.Where(a => a.Timestamp <= to);
        if (f.ProcessingStatus is { } s) q = q.Where(a => a.Status == s);
        if (!string.IsNullOrWhiteSpace(f.BatchId)) q = q.Where(a => a.BatchId == f.BatchId);
        return q;
    }

    // export ------------------------------------------------------------------------------------------
    public string SuggestedFileName(ReportType type, ExportFormat format) =>
        $"{type.ToString().ToLowerInvariant()}-report-{DateTime.Now:yyyyMMdd-HHmmss}{ReportExporter.Extension(format)}";

    /// <summary>Writes the report file and records the export in the audit log.</summary>
    public async Task ExportAsync(ReportData report, ExportFormat format, string path, CancellationToken ct = default)
    {
        try
        {
            await Task.Run(() => ReportExporter.Export(report, format, path), ct);
            await using var ctx = await _factory.CreateDbContextAsync(ct);
            ctx.AuditLogs.Add(Audit.Entry(AuditOperation.Export, "Report", null, summary: $"Exported '{report.Title}' ({report.RowCount} rows) as {format} to {Path.GetFileName(path)}"));
            await ctx.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await using var ctx = await _factory.CreateDbContextAsync(CancellationToken.None);
            ctx.AuditLogs.Add(Audit.Entry(AuditOperation.Export, "Report", null, status: OperationStatus.Failed, summary: $"Export of '{report.Title}' failed", error: ex.Message));
            await ctx.SaveChangesAsync(CancellationToken.None);
            throw new ImportException("The report could not be saved. Check that the file is not open in another program and that the folder is writable.", ex);
        }
    }

    /// <summary>Exports a contact list (any subset) as Excel / CSV / PDF.</summary>
    public async Task ExportContactsAsync(IReadOnlyList<Contact> contacts, ExportFormat format, string path, string title = "Contacts", CancellationToken ct = default)
    {
        var r = new ReportData { Title = title, Subtitle = $"{contacts.Count:N0} contact(s)", Footer = "CRM MASTER - local export" };
        r.Columns.AddRange(new[] { "ID" }.Concat(Contact.Fields.Select(x => x.Label)).Append("Created").Append("Updated"));
        foreach (var c in contacts)
            r.Rows.Add(new object?[] { c.Id }.Concat(Contact.Fields.Select(x => (object?)c.Get(x.Field))).Append(c.CreatedAt.ToLocalTime()).Append(c.UpdatedAt.ToLocalTime()).ToArray());
        await ExportAsync(r, format, path, ct);
    }
}
