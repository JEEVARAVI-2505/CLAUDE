using LeadPilot.Domain;

namespace LeadPilot.Application.Models;

/// <summary>Sortable grid columns.</summary>
public enum LeadSort { Created, Company, Score, Status, NextFollowUp, LastContacted, LeadId }

/// <summary>Search, filter, sort and paging options for the lead grid.</summary>
public sealed class LeadQuery
{
    public string? Text { get; set; }
    public LeadStatus? Status { get; set; }
    public LeadTemperature? Temperature { get; set; }
    public string? Source { get; set; }
    public string? Industry { get; set; }
    public string? City { get; set; }
    public int? AssignedUserId { get; set; }
    public int? CampaignId { get; set; }
    public int? MinScore { get; set; }
    public bool OnlyIncomplete { get; set; }
    public bool OnlyMissingContact { get; set; }
    public bool OnlyNeedsFollowUp { get; set; }
    public bool IncludeArchived { get; set; }
    public LeadSort Sort { get; set; } = LeadSort.Created;
    public bool Descending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>Flat lead row for grids and exports.</summary>
public sealed class LeadRow
{
    public int Id { get; set; }
    public string LeadCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Location { get; set; }
    public string Source { get; set; } = "";
    public int Score { get; set; }
    public LeadTemperature Temperature { get; set; }
    public LeadStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public EmailStatus EmailStatus { get; set; }
    public PhoneStatus PhoneStatus { get; set; }
    public WebsiteStatus WebsiteStatus { get; set; }

    /// <summary>True when phone, e-mail or website is missing.</summary>
    public bool IsIncomplete => string.IsNullOrWhiteSpace(Phone) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Website);
}

/// <summary>One page of grid results.</summary>
public sealed record LeadPage(IReadOnlyList<LeadRow> Rows, int Total, int Page, int PageSize)
{
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(1, PageSize)));
}

/// <summary>A lead with all related records.</summary>
public sealed class LeadDetail
{
    public Lead Lead { get; init; } = new();
    public Business Business { get; init; } = new();
    public List<Contact> Contacts { get; init; } = new();
    public List<LeadSourceRecord> Sources { get; init; } = new();
    public List<FieldEvidence> Evidence { get; init; } = new();
    public List<LeadActivity> Activities { get; init; } = new();
    public List<FollowUpTask> Tasks { get; init; } = new();
    public string? AssignedTo { get; set; }
}

/// <summary>Everything written when a lead is created, inside one transaction.</summary>
public sealed class LeadInsert
{
    public Business Business { get; init; } = new();
    public Contact? Contact { get; init; }
    public Lead Lead { get; init; } = new();
    public LeadSourceRecord? Source { get; init; }
    public List<FieldEvidence> Evidence { get; init; } = new();
    public LeadActivity? Activity { get; init; }
}

/// <summary>Distinct values for filter drop-downs.</summary>
public sealed record FilterOptions(IReadOnlyList<string> Industries, IReadOnlyList<string> Cities, IReadOnlyList<string> Sources);

/// <summary>Snapshot of a lead + business used for merge undo.</summary>
public sealed class LeadSnapshot
{
    public Business Business { get; set; } = new();
    public Lead Lead { get; set; } = new();
}

/// <summary>Child record ids moved from the merged lead to the survivor.</summary>
public sealed class MovedRecords
{
    public List<int> Sources { get; set; } = new();
    public List<int> Evidence { get; set; } = new();
    public List<int> Activities { get; set; } = new();
    public List<int> Tasks { get; set; } = new();
}

/// <summary>Instructions to the repository for a merge (computed by <c>MergeService</c>).</summary>
public sealed class MergePlan
{
    public int SurvivorLeadId { get; init; }
    public int MergedLeadId { get; init; }
    public Business SurvivorBusinessAfter { get; init; } = new();
    public int? SurvivorPrimaryContactAfter { get; init; }
    public string SurvivorBeforeJson { get; init; } = "";
    public string MergedBeforeJson { get; init; } = "";
    public string? MergedBy { get; init; }
}

/// <summary>Side-by-side data for the duplicate manager.</summary>
public sealed class DuplicateView
{
    public DuplicateMatch Match { get; init; } = new();
    public LeadRow A { get; init; } = new();
    public LeadRow B { get; init; } = new();
}

/// <summary>Task row joined with lead info.</summary>
public sealed class TaskRow
{
    public FollowUpTask Task { get; init; } = new();
    public string LeadCode { get; init; } = "";
    public string CompanyName { get; init; } = "";
    public string? AssignedTo { get; init; }
    public bool IsOverdue(DateTime nowUtc) => Task.Status == FollowUpStatus.Open && Task.DueAt < nowUtc;
}

/// <summary>Filter for the follow-up list.</summary>
public sealed class TaskQuery
{
    public FollowUpStatus? Status { get; set; }
    public bool OnlyOverdue { get; set; }
    public int? LeadId { get; set; }
    public int? AssignedUserId { get; set; }
    public DateTime? DueBefore { get; set; }
}

/// <summary>A label with a count, used by charts.</summary>
public sealed record KeyCount(string Key, int Count);

/// <summary>Per-source conversion info.</summary>
public sealed record SourcePerformance(string Source, int Leads, int Qualified, int Won, double AverageScore);

/// <summary>All dashboard numbers; every value comes from the database.</summary>
public sealed class DashboardStats
{
    public int TotalDiscovered { get; set; }
    public int TotalLeads { get; set; }
    public int NewLeads { get; set; }
    public int QualifiedLeads { get; set; }
    public int HighPriorityLeads { get; set; }
    public int DuplicateRecords { get; set; }
    public int IncompleteRecords { get; set; }
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }
    public int ConvertedLeads { get; set; }
    public IReadOnlyList<KeyCount> ByIndustry { get; set; } = Array.Empty<KeyCount>();
    public IReadOnlyList<KeyCount> ByLocation { get; set; } = Array.Empty<KeyCount>();
    public IReadOnlyList<KeyCount> BySource { get; set; } = Array.Empty<KeyCount>();
    public IReadOnlyList<KeyCount> Quality { get; set; } = Array.Empty<KeyCount>();
    public IReadOnlyList<KeyCount> MonthlyDiscovery { get; set; } = Array.Empty<KeyCount>();
    public IReadOnlyList<KeyCount> Pipeline { get; set; } = Array.Empty<KeyCount>();
    public IReadOnlyList<KeyCount> ConversionTrend { get; set; } = Array.Empty<KeyCount>();
}
