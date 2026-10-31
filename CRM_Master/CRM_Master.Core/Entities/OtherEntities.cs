namespace CRM_Master.Core.Entities;

/// <summary>A company referenced by one or more contacts.</summary>
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Normalised name (legal suffixes and punctuation removed) - unique.</summary>
    public string NameKey { get; set; } = "";
    public string? Website { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Contact> Contacts { get; set; } = new();
}

/// <summary>User-defined extra field attached to contacts.</summary>
public class CustomField
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public CustomFieldType DataType { get; set; } = CustomFieldType.Text;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Value of a custom field for one contact.</summary>
public class ContactCustomFieldValue
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public Contact? Contact { get; set; }
    public int CustomFieldId { get; set; }
    public CustomField? CustomField { get; set; }
    public string? Value { get; set; }
}

/// <summary>One run of a file import.</summary>
public class ImportJob
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileType { get; set; } = "";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Running;
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int SkippedRows { get; set; }
    public int DuplicateRows { get; set; }
    public int FailedRows { get; set; }
    public string? MappingJson { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ImportRecord> Records { get; set; } = new();
}

/// <summary>Outcome for one source row. Only non-imported rows keep their raw data to stay small.</summary>
public class ImportRecord
{
    public long Id { get; set; }
    public int ImportJobId { get; set; }
    public ImportJob? ImportJob { get; set; }
    public int RowNumber { get; set; }
    public ImportRecordStatus Status { get; set; }
    public int? ContactId { get; set; }
    public string? Message { get; set; }
    public string? RawJson { get; set; }
}

/// <summary>A cluster of contacts that may be the same customer.</summary>
public class DuplicateGroup
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DuplicateStatus Status { get; set; } = DuplicateStatus.Open;
    public MatchClass Classification { get; set; }
    public double TopScore { get; set; }
    public int? MasterContactId { get; set; }
    public List<DuplicateMatch> Matches { get; set; } = new();
}

/// <summary>A scored pair of contacts inside a duplicate group.</summary>
public class DuplicateMatch
{
    public int Id { get; set; }
    public int DuplicateGroupId { get; set; }
    public DuplicateGroup? Group { get; set; }
    public int ContactAId { get; set; }
    public int ContactBId { get; set; }
    public double Score { get; set; }
    public MatchClass Classification { get; set; }
    public string MatchedOn { get; set; } = "";
    public DuplicateStatus Status { get; set; } = DuplicateStatus.Open;
}

/// <summary>A reusable local automation rule. Conditions and actions are stored as JSON.</summary>
public class AutomationRule
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public AutomationTrigger Trigger { get; set; }
    public string ConditionsJson { get; set; } = "{}";
    public string ActionsJson { get; set; } = "[]";
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunAt { get; set; }
    public int RunCount { get; set; }
}

/// <summary>History entry: a rule fired (for bulk triggers, one row summarises many contacts).</summary>
public class AutomationExecution
{
    public int Id { get; set; }
    public int AutomationRuleId { get; set; }
    public AutomationRule? Rule { get; set; }
    public int? ContactId { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public AutomationTrigger Trigger { get; set; }
    public OperationStatus Status { get; set; } = OperationStatus.Success;
    public int AffectedCount { get; set; }
    public string? Message { get; set; }
}

/// <summary>Immutable-by-convention audit trail row.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AuditOperation Operation { get; set; }
    public string EntityType { get; set; } = "Contact";
    public long? RecordId { get; set; }
    /// <summary>Groups all rows of one logical batch (import, bulk update, cleaning run).</summary>
    public string? BatchId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public OperationStatus Status { get; set; } = OperationStatus.Success;
    public string? Summary { get; set; }
    public string? ErrorDetails { get; set; }
}

/// <summary>Key/value application setting (value is JSON).</summary>
public class ApplicationSetting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>Registry entry for a database backup file.</summary>
public class BackupRecord
{
    public int Id { get; set; }
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long SizeBytes { get; set; }
    public BackupKind Kind { get; set; }
    public string? Reason { get; set; }
    public int ContactCount { get; set; }
}
