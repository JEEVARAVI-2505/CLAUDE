using LeadPilot.Application.Models;

namespace LeadPilot.Application.Abstractions;

/// <summary>Raw rows from a CSV/XLSX file.</summary>
public sealed class ImportTable
{
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string[]> Rows { get; init; } = Array.Empty<string[]>();
    public string FileName { get; init; } = "";
    public string FileHash { get; init; } = "";
}

/// <summary>Reads tabular files safely (size-limited, no macros, no external links).</summary>
public interface IImportReader { ImportTable Read(string path); }

/// <summary>Writes leads to files for hand-off.</summary>
public interface IExportService
{
    void ExportCsv(IReadOnlyList<LeadRow> rows, string path);
    void ExportXlsx(IReadOnlyList<LeadRow> rows, string path);
    void ExportPdf(IReadOnlyList<LeadRow> rows, string path, string title);
    void ExportTextReportPdf(string title, IReadOnlyList<string> lines, string path);
}

/// <summary>Access to the physical database file for backup/restore.</summary>
public interface IDatabaseFile
{
    string Path { get; }
    void BackupTo(string destination);
    /// <summary>Replaces the live database with the given file (already validated) and re-applies migrations.</summary>
    void ReplaceWith(string sourceFile);
    int SchemaVersion();
    /// <summary>Returns "ok" when SQLite's integrity check passes for the file.</summary>
    string IntegrityCheck(string file);
}

/// <summary>Encrypted/plain backup of the whole database.</summary>
public interface IBackupService
{
    string CreateBackup(string destinationFolder, string? password);
    void Restore(string backupFile, string? password);
}
