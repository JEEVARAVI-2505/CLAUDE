using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Import;

/// <summary>How a file should be imported.</summary>
public sealed record ImportOptions(
    IReadOnlyList<ColumnMapping> Mapping, bool CleanOnImport = true,
    ImportDuplicatePolicy DuplicatePolicy = ImportDuplicatePolicy.Skip, string? Sheet = null);

public enum PreviewRowStatus { Ready, Warning, DuplicateExisting, DuplicateInFile, Invalid, Empty }

/// <summary>One row of the import preview: what it would become and why.</summary>
public sealed record PreviewRow(int RowNumber, PreviewRowStatus Status, Contact Contact, string Message, int? ExistingContactId);

/// <summary>Dry-run result shown before the user confirms an import.</summary>
public sealed class ImportPreview
{
    public List<PreviewRow> Sample { get; } = new();
    public List<string> MappingErrors { get; } = new();
    public int Total { get; set; }
    public int Ready { get; set; }
    public int Warnings { get; set; }
    public int DuplicateExisting { get; set; }
    public int DuplicateInFile { get; set; }
    public int Invalid { get; set; }
    public int Empty { get; set; }
    public int Importable => Ready + Warnings;
}

/// <summary>Summary of a finished (or failed / cancelled) import.</summary>
public sealed record ImportResult(
    int JobId, ImportJobStatus Status, int Total, int Imported, int Proposed, int Skipped, int Duplicates, int Failed,
    TimeSpan Elapsed, string? Error = null, string? ReportPath = null);

/// <summary>Implemented by the reporting module so an "import completed" automation rule can produce a report.</summary>
public interface IImportReportGenerator
{
    Task<string?> GenerateImportSummaryAsync(int importJobId, CancellationToken ct = default);
}
