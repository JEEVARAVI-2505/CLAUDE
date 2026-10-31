using System.Text.Json;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

/// <summary>Target fields an imported column can map to.</summary>
public enum ImportField { CompanyName, ContactName, ContactTitle, Phone, Email, Website, Address, City, Region, Country, Industry, CompanySize, Source, IntentEvidence, Status }

/// <summary>Preview shown before the user confirms an import.</summary>
public sealed class ImportPreview
{
    public ImportTable Table { get; init; } = new();
    public Dictionary<ImportField, int> SuggestedMapping { get; init; } = new();
    public bool AlreadyImported { get; init; }
}

/// <summary>Row-level validation findings.</summary>
public sealed class ImportValidation
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}

/// <summary>Final import report (also stored in ImportHistory).</summary>
public sealed class ImportReport
{
    public string FileName { get; set; } = "";
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int DuplicatesLinked { get; set; }
    public int FlaggedForReview { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public List<string> Messages { get; } = new();
}

/// <summary>Preview, column mapping, validation and commit for CSV/XLSX imports.</summary>
public sealed class ImportService
{
    private static readonly Dictionary<ImportField, string[]> Synonyms = new()
    {
        [ImportField.CompanyName] = new[] { "company", "company name", "business", "business name", "organization", "organisation", "name", "account", "firm" },
        [ImportField.ContactName] = new[] { "contact", "contact name", "contact person", "person", "full name", "owner", "decision maker" },
        [ImportField.ContactTitle] = new[] { "title", "job title", "designation", "position", "role" },
        [ImportField.Phone] = new[] { "phone", "phone number", "mobile", "mobile number", "telephone", "tel", "contact number", "cell" },
        [ImportField.Email] = new[] { "email", "e-mail", "email address", "mail", "email id" },
        [ImportField.Website] = new[] { "website", "web", "url", "site", "web site", "domain", "web address" },
        [ImportField.Address] = new[] { "address", "street", "street address", "location address" },
        [ImportField.City] = new[] { "city", "town", "location" },
        [ImportField.Region] = new[] { "state", "region", "province" },
        [ImportField.Country] = new[] { "country" },
        [ImportField.Industry] = new[] { "industry", "sector", "category", "business category", "vertical" },
        [ImportField.CompanySize] = new[] { "size", "company size", "employees", "employee count", "headcount" },
        [ImportField.Source] = new[] { "source", "lead source", "origin", "channel" },
        [ImportField.IntentEvidence] = new[] { "notes", "intent", "enquiry", "inquiry", "message", "requirement", "comments" },
        [ImportField.Status] = new[] { "status", "lead status", "stage" }
    };

    private readonly IImportReader _reader;
    private readonly LeadIngestService _ingest;
    private readonly ISystemRepository _sys;
    private readonly ICurrentUser _user;
    private readonly ISettingsService _settings;
    private readonly ScoringService _scoring;
    private readonly IClock _clock;

    public ImportService(IImportReader reader, LeadIngestService ingest, ISystemRepository sys, ICurrentUser user, ISettingsService settings, ScoringService scoring, IClock clock)
    { _reader = reader; _ingest = ingest; _sys = sys; _user = user; _settings = settings; _scoring = scoring; _clock = clock; }

    /// <summary>Reads the file and suggests a column mapping from header names.</summary>
    public ImportPreview Preview(string path)
    {
        var table = _reader.Read(path);
        var already = _sys.GetImports(500).Any(i => i.FileHash == table.FileHash);
        return new ImportPreview { Table = table, SuggestedMapping = SuggestMapping(table.Headers), AlreadyImported = already };
    }

    /// <summary>Maps header names to fields using the synonym table (first unused best match).</summary>
    public static Dictionary<ImportField, int> SuggestMapping(IReadOnlyList<string> headers)
    {
        var map = new Dictionary<ImportField, int>();
        var used = new HashSet<int>();
        foreach (var (field, names) in Synonyms)
            for (var i = 0; i < headers.Count; i++)
            {
                if (used.Contains(i)) continue;
                var h = Normalizer.NormalizeText(headers[i]);
                if (names.Any(n => Normalizer.NormalizeText(n) == h)) { map[field] = i; used.Add(i); break; }
            }
        return map;
    }

    /// <summary>Checks every row against the mapping without writing anything.</summary>
    public ImportValidation Validate(ImportTable table, IReadOnlyDictionary<ImportField, int> map)
    {
        var v = new ImportValidation { TotalRows = table.Rows.Count };
        if (!map.ContainsKey(ImportField.CompanyName)) { v.Errors.Add("Map a column to Company name; it is required."); return v; }
        var region = _settings.Current.DefaultRegion;
        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i]; var line = i + 2; // header is line 1
            if (string.IsNullOrWhiteSpace(Get(row, map, ImportField.CompanyName))) { v.Errors.Add($"Row {line}: company name is empty – row will be skipped."); continue; }
            var email = Get(row, map, ImportField.Email); var phone = Get(row, map, ImportField.Phone); var web = Get(row, map, ImportField.Website);
            if (email != null && !Normalizer.IsValidEmailFormat(email)) v.Warnings.Add($"Row {line}: e-mail \"{email}\" has an invalid format (kept, flagged).");
            if (phone != null && !Normalizer.ParsePhone(phone, region).IsValid) v.Warnings.Add($"Row {line}: phone \"{phone}\" is not valid for region {region} (kept, flagged).");
            if (web != null && Normalizer.NormalizeUrl(web) == null) v.Warnings.Add($"Row {line}: website \"{web}\" is not a valid URL.");
            v.ValidRows++;
        }
        return v;
    }

    /// <summary>Imports all valid rows, detecting duplicates, and stores an import report.</summary>
    public ImportReport Import(ImportTable table, IReadOnlyDictionary<ImportField, int> map, int? campaignId, IProgress<int>? progress = null)
    {
        var report = new ImportReport { FileName = table.FileName, TotalRows = table.Rows.Count };
        if (!map.ContainsKey(ImportField.CompanyName)) throw new InvalidOperationException("Company name must be mapped.");
        var now = _clock.UtcNow;
        var cands = new List<(int Line, ProspectCandidate C)>();
        for (var i = 0; i < table.Rows.Count; i++)
        {
            var r = table.Rows[i];
            var name = Get(r, map, ImportField.CompanyName);
            if (name == null) { report.Errors++; report.Messages.Add($"Row {i + 2}: skipped – company name empty."); continue; }
            cands.Add((i + 2, new ProspectCandidate
            {
                Name = name, ContactName = Get(r, map, ImportField.ContactName), ContactTitle = Get(r, map, ImportField.ContactTitle), Phone = Get(r, map, ImportField.Phone),
                Email = Get(r, map, ImportField.Email), Website = Get(r, map, ImportField.Website), Address = Get(r, map, ImportField.Address), City = Get(r, map, ImportField.City),
                Region = Get(r, map, ImportField.Region), Country = Get(r, map, ImportField.Country), Industry = Get(r, map, ImportField.Industry),
                CompanySize = Get(r, map, ImportField.CompanySize), IntentEvidence = Get(r, map, ImportField.IntentEvidence), Status = Get(r, map, ImportField.Status),
                SourceName = Get(r, map, ImportField.Source) is { } s ? $"Import: {s}" : $"Import: {table.FileName}", SourceType = "File import", RetrievedAt = now
            }));
        }
        var ctx = new IngestContext { CampaignId = campaignId, Profile = _scoring.GetActiveProfile(), UserId = _user.UserId };
        var outcomes = _ingest.IngestMany(cands.Select(c => c.C), ctx, progress);
        for (var i = 0; i < outcomes.Count; i++)
        {
            var o = outcomes[i]; var line = cands[i].Line;
            switch (o.Status)
            {
                case IngestStatus.Created: report.Imported++; if (o.Matches.Count > 0) { report.FlaggedForReview++; report.Messages.Add($"Row {line}: imported, {o.Message}"); } break;
                case IngestStatus.DuplicateLinked: report.DuplicatesLinked++; report.Messages.Add($"Row {line}: {o.Message}"); break;
                case IngestStatus.Skipped: report.Skipped++; report.Messages.Add($"Row {line}: skipped – {o.Message}"); break;
                default: report.Errors++; report.Messages.Add($"Row {line}: {o.Message}"); break;
            }
        }
        _sys.AddImport(new ImportRecord
        {
            FileName = table.FileName, FileHash = table.FileHash, TotalRows = report.TotalRows, Imported = report.Imported, Duplicates = report.DuplicatesLinked + report.FlaggedForReview,
            Skipped = report.Skipped, Errors = report.Errors, ReportJson = JsonSerializer.Serialize(report), ImportedAt = now, UserId = _user.UserId
        });
        _sys.AddAudit(new AuditEntry { At = now, UserId = _user.UserId, Action = "import", Entity = "File", EntityId = table.FileName, Details = $"{report.Imported} imported, {report.DuplicatesLinked} linked, {report.Errors} errors" });
        return report;
    }

    private static string? Get(string[] row, IReadOnlyDictionary<ImportField, int> map, ImportField f)
    {
        if (!map.TryGetValue(f, out var i) || i < 0 || i >= row.Length) return null;
        var v = row[i]?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }
}
