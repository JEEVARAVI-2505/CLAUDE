namespace LeadPilot.Domain;

/// <summary>A company or organisation. Phone/email here are business-level (e.g. a switchboard), not personal.</summary>
public sealed class Business
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Website { get; set; }
    public string? Domain { get; set; }
    public string? Phone { get; set; }
    public string? NormalizedPhone { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? Industry { get; set; }
    public string? Services { get; set; }
    public string? Description { get; set; }
    public string? ContactPageUrl { get; set; }
    public string? CompanySize { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Shallow copy used for merge snapshots.</summary>
    public Business Clone() => (Business)MemberwiseClone();
}

/// <summary>A named person at a business.</summary>
public sealed class Contact
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string FullName { get; set; } = "";
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? NormalizedPhone { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>A prospect being worked. Has a stable human-readable code such as LD-000042.</summary>
public sealed class Lead
{
    public int Id { get; set; }
    public string LeadCode { get; set; } = "";
    public int BusinessId { get; set; }
    public int? PrimaryContactId { get; set; }
    public int? CampaignId { get; set; }
    public int? AssignedUserId { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public int Score { get; set; }
    public LeadTemperature Temperature { get; set; } = LeadTemperature.Unqualified;
    public string? ScoreExplanation { get; set; }
    public EmailStatus EmailStatus { get; set; }
    public PhoneStatus PhoneStatus { get; set; }
    public WebsiteStatus WebsiteStatus { get; set; }
    /// <summary>Evidence of buying intent supplied by the user or an authorised source (e.g. an enquiry form).</summary>
    public string? IntentEvidence { get; set; }
    public bool IsArchived { get; set; }
    public int? MergedIntoLeadId { get; set; }
    public string SourceName { get; set; } = "Manual";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
}

/// <summary>Where a lead (or part of it) came from. Kept for attribution and licence compliance.</summary>
public sealed class LeadSourceRecord
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public string SourceName { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string? SourceUrl { get; set; }
    public string? ExternalId { get; set; }
    public string? License { get; set; }
    public DateTime RetrievedAt { get; set; }
}

/// <summary>Field-level provenance: which value came from which URL at what time.</summary>
public sealed class FieldEvidence
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public string FieldName { get; set; } = "";
    public string Value { get; set; } = "";
    public string SourceName { get; set; } = "";
    public string? SourceUrl { get; set; }
    public DateTime ExtractedAt { get; set; }
    public double Confidence { get; set; }
    /// <summary>True when the value was written to the lead; false when only recorded as an unapplied suggestion.</summary>
    public bool Applied { get; set; }
    public string? Note { get; set; }
}

/// <summary>A candidate duplicate pair with the reasons the detector gave.</summary>
public sealed class DuplicateMatch
{
    public int Id { get; set; }
    public int LeadAId { get; set; }
    public int LeadBId { get; set; }
    public double Score { get; set; }
    public DuplicateConfidence Confidence { get; set; }
    public string Reasons { get; set; } = "";
    public DuplicateDecision Decision { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
}

/// <summary>Audit trail of a merge, holding enough state to undo it.</summary>
public sealed class MergeRecord
{
    public int Id { get; set; }
    public int SurvivorLeadId { get; set; }
    public int MergedLeadId { get; set; }
    public DateTime MergedAt { get; set; }
    public string? MergedBy { get; set; }
    public DateTime? UndoneAt { get; set; }
    public string SurvivorBeforeJson { get; set; } = "";
    public string MergedBeforeJson { get; set; } = "";
    public string MovedJson { get; set; } = "";
}

/// <summary>One entry on a lead's timeline.</summary>
public sealed class LeadActivity
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public ActivityType Type { get; set; }
    public string Subject { get; set; } = "";
    public string? Body { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UserId { get; set; }
}

/// <summary>A scheduled follow-up.</summary>
public sealed class FollowUpTask
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime DueAt { get; set; }
    public FollowUpStatus Status { get; set; }
    public int? AssignedUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>A prospecting campaign (product + target market).</summary>
public sealed class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Product { get; set; } = "";
    public string TargetCustomer { get; set; } = "";
    public string Location { get; set; } = "";
    public int TargetCount { get; set; }
    public CampaignStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>A local application user. The optional PIN is stored only as a salted hash.</summary>
public sealed class SalesUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public UserRole Role { get; set; }
    public string? PinHash { get; set; }
    public string? PinSalt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Summary of one import run.</summary>
public sealed class ImportRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public string? FileHash { get; set; }
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Duplicates { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public string ReportJson { get; set; } = "";
    public DateTime ImportedAt { get; set; }
    public int? UserId { get; set; }
}

/// <summary>Security-relevant event.</summary>
public sealed class AuditEntry
{
    public int Id { get; set; }
    public DateTime At { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = "";
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
}

/// <summary>One call made to an external data provider.</summary>
public sealed class ApiUsageRecord
{
    public int Id { get; set; }
    public DateTime At { get; set; }
    public string Provider { get; set; } = "";
    public string Operation { get; set; } = "";
    public int Requests { get; set; }
    public bool Success { get; set; }
    public int ResultCount { get; set; }
    public string? ErrorMessage { get; set; }
}
