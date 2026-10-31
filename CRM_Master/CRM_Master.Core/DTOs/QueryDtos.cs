using CRM_Master.Core.Entities;

namespace CRM_Master.Core.DTOs;

/// <summary>One page of results plus the total number of matches.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public static PagedResult<T> Empty(int pageSize = 100) => new(Array.Empty<T>(), 0, 1, pageSize);
}

/// <summary>Data-quality filter for the contact list.</summary>
public enum QualityFilter { All, InvalidEmail, InvalidPhone, MissingEmail, MissingPhone, Incomplete, NeedsReview, FlaggedDuplicate, Clean }

/// <summary>Search / filter / sort / paging request for contacts. SortBy is checked against a whitelist.</summary>
public sealed record ContactQuery(
    string? Search = null, string? Status = null, string? Country = null, QualityFilter Quality = QualityFilter.All,
    string SortBy = "UpdatedAt", bool Descending = true, int Page = 1, int PageSize = 100)
{
    public static readonly string[] SortableColumns =
        { "FullName", "Company", "Email", "Phone", "City", "Country", "Status", "CreatedAt", "UpdatedAt" };
}

/// <summary>Filter for the audit trail.</summary>
public sealed record AuditQuery(
    string? Search = null, AuditOperation? Operation = null, OperationStatus? Status = null,
    DateTime? FromUtc = null, DateTime? ToUtc = null, long? RecordId = null, string? EntityType = null, string? BatchId = null,
    int Page = 1, int PageSize = 100);

/// <summary>Metrics shown on the dashboard - all computed from the database.</summary>
public sealed class DashboardStats
{
    public int TotalContacts { get; set; }
    public int NewContactsImported { get; set; }
    public int DuplicateRecords { get; set; }
    public int InvalidEmails { get; set; }
    public int InvalidPhones { get; set; }
    public int IncompleteRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int FailedOperations { get; set; }
    public int CleanRecords { get; set; }
    public List<TimePoint> Growth { get; set; } = new();
    public List<ImportJob> RecentImports { get; set; } = new();
    public List<AuditLog> RecentAutomation { get; set; } = new();
    public List<AuditLog> RecentErrors { get; set; } = new();
    public bool IsEmpty => TotalContacts == 0;
}

/// <summary>A labelled point on a time series.</summary>
public sealed record TimePoint(string Label, double Value);

/// <summary>Everything the contact profile "history" tab shows.</summary>
public sealed record ContactHistory(IReadOnlyList<AuditLog> Entries, ImportJob? SourceImport);
