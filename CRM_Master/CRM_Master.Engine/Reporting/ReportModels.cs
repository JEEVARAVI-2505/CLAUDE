using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Reporting;

public enum ReportType { ImportSummary, Duplicates, DataQuality, InvalidRecords, BulkUpdate, AutomationExecution, Audit }
public enum ExportFormat { Excel, Csv, Pdf }

/// <summary>User-selectable report filters. Each filter applies to the report types it makes sense for.</summary>
public sealed record ReportFilter(
    DateTime? FromUtc = null, DateTime? ToUtc = null,
    /// <summary>Contact category (the contact's Status, e.g. Lead / Customer) - for contact-based reports.</summary>
    string? ContactCategory = null,
    /// <summary>Outcome filter - for import, bulk update, automation and audit reports.</summary>
    OperationStatus? ProcessingStatus = null,
    string? BatchId = null);

/// <summary>A report as plain tabular data, independent of the output format.</summary>
public sealed class ReportData
{
    public string Title { get; init; } = "";
    public string? Subtitle { get; set; }
    public List<(string Label, string Value)> Summary { get; } = new();
    public List<string> Columns { get; } = new();
    public List<object?[]> Rows { get; } = new();
    public string Footer { get; set; } = "";
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public int RowCount => Rows.Count;
}
