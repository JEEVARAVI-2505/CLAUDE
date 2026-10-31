using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Cleaning;

/// <summary>Which cleaning steps to run.</summary>
public sealed class CleaningOptions
{
    public bool TrimAndCollapseSpaces { get; set; } = true;
    public bool ProperCaseNames { get; set; } = true;
    public bool NormalizeEmail { get; set; } = true;
    public bool NormalizePhone { get; set; } = true;
    public bool NormalizeWebsite { get; set; } = true;
    /// <summary>Also propose removing contacts whose fields are all identical (the lowest id is kept).</summary>
    public bool RemoveExactDuplicates { get; set; }
    public string DefaultCountryCode { get; set; } = "91";
}

/// <summary>A proposed edit to one field (the "before" and "after" the user previews).</summary>
public sealed record CleaningChange(int ContactId, string ContactName, ContactField Field, string? Before, string? After, string Reason);

public enum CleaningIssueKind { EmptyField, MalformedEmail, MalformedPhone, InvalidDate, SuspiciousValue }

/// <summary>A data problem that cleaning cannot fix automatically - reported for review.</summary>
public sealed record CleaningIssue(int ContactId, string ContactName, ContactField Field, CleaningIssueKind Kind, string Detail, string? Value);

/// <summary>Result of analysing a data set; nothing is modified until the user applies it.</summary>
public sealed class CleaningReport
{
    public int ContactsScanned { get; set; }
    public List<CleaningChange> Changes { get; } = new();
    public List<CleaningIssue> Issues { get; } = new();
    /// <summary>Each group lists contact ids whose fields are identical after cleaning; the first (lowest id) is kept.</summary>
    public List<List<int>> ExactDuplicateGroups { get; } = new();
    public int RecordsAffected => Changes.Select(c => c.ContactId).Distinct().Count();
    public int ErrorCount => Issues.Count(i => i.Kind is CleaningIssueKind.MalformedEmail or CleaningIssueKind.MalformedPhone or CleaningIssueKind.InvalidDate);
    public int RemovableDuplicates => ExactDuplicateGroups.Sum(g => g.Count - 1);
}

/// <summary>What applying a cleaning report did.</summary>
public sealed record CleaningApplyResult(string BatchId, int ContactsUpdated, int FieldsChanged, int DuplicatesRemoved, int SkippedBecauseChanged, string? BackupPath);
