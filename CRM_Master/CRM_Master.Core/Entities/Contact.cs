namespace CRM_Master.Core.Entities;

/// <summary>A customer / lead record. The single source of truth for contact data.</summary>
public partial class Contact
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? LeadSource { get; set; }
    public string Status { get; set; } = "Lead";
    public string? Notes { get; set; }

    /// <summary>Created date as given by the source system / spreadsheet (not the local record creation time).</summary>
    public DateTime? SourceCreatedDate { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    // Derived lookup keys (maintained by ContactKeys.Apply) - indexed for duplicate checks.
    public string? EmailKey { get; set; }
    public string? PhoneKey { get; set; }
    public string? AlternatePhoneKey { get; set; }
    public string? NameKey { get; set; }

    // Persisted quality flags (maintained by ContactQuality.Evaluate) - drive dashboard counts.
    public ValidationState EmailState { get; set; }
    public ValidationState PhoneState { get; set; }
    public bool IsIncomplete { get; set; }
    public bool NeedsReview { get; set; }
    public bool IsDuplicateFlagged { get; set; }

    public int? ImportJobId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ContactCustomFieldValue> CustomValues { get; set; } = new();

    /// <summary>Best human-readable name for lists and dialogs.</summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(FullName)) return FullName!;
            var composed = $"{FirstName} {LastName}".Trim();
            if (composed.Length > 0) return composed;
            return Email ?? Phone ?? $"Contact #{Id}";
        }
    }
}
