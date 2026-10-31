namespace CRM_Master.Core.Entities;

/// <summary>Result of checking a contact point (email / phone). Syntax only - never proof of deliverability.</summary>
public enum ValidationState { Missing = 0, Valid = 1, Invalid = 2 }

/// <summary>Kinds of operations written to the audit log.</summary>
public enum AuditOperation
{
    ContactCreated, ContactUpdated, ContactDeleted, Import, DataCleaning, DuplicateDetection,
    Merge, BulkUpdate, Export, AutomationExecution, Backup, Restore, SettingsChanged, Validation
}

/// <summary>Outcome of an audited operation.</summary>
public enum OperationStatus { Success, Warning, Failed, Cancelled }

/// <summary>Lifecycle of an import job.</summary>
public enum ImportJobStatus { Running, Completed, CompletedWithWarnings, Failed, Cancelled }

/// <summary>Per-row result of an import.</summary>
public enum ImportRecordStatus { Imported, Skipped, Duplicate, Failed, Proposed, Updated }

/// <summary>Review state of a duplicate group or match.</summary>
public enum DuplicateStatus { Open, Merged, Skipped, MarkedSeparate }

/// <summary>Confidence classes for duplicate matches.</summary>
public enum MatchClass { Low = 0, Possible = 1, High = 2, Exact = 3 }

/// <summary>Events that can start an automation rule.</summary>
public enum AutomationTrigger { ContactImported, ContactCreated, ContactUpdated, ExistingContactMatched, DuplicateDetected, ImportCompleted, Manual }

/// <summary>Data type of a user-defined custom field.</summary>
public enum CustomFieldType { Text, Number, Date }

/// <summary>Why a backup was taken.</summary>
public enum BackupKind { Manual, Automatic, PreOperation, PreRestore }
