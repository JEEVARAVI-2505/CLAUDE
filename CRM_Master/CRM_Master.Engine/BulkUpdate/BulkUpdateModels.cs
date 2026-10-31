using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.BulkUpdate;

/// <summary>How incoming values are applied to existing contacts. Every policy still shows a proposal first.</summary>
public enum BulkUpdatePolicy
{
    /// <summary>Fill blanks in the selected fields; never change a value that exists.</summary>
    EmptyFieldsOnly,
    /// <summary>Replace existing values in the selected fields with the incoming value (when the incoming value is not blank).</summary>
    AllSelectedFields,
    /// <summary>Existing values always win; only blanks (in any field) are filled.</summary>
    PreserveExisting,
    /// <summary>Like AllSelectedFields, but an incoming value is only used if it passes validation.</summary>
    OnlyIfValid,
    /// <summary>Records where any field would conflict (both values present and different) are skipped entirely.</summary>
    SkipConflicts,
    /// <summary>Everything is proposed but nothing is pre-approved.</summary>
    ManualApproval
}

/// <summary>Which existing record an incoming row is matched to.</summary>
public enum MatchBy { EmailThenPhone, EmailOnly, PhoneOnly }

public enum ProposalKind { FillEmpty, Overwrite, Same, MissingIncoming, KeptExisting, RejectedInvalid, Conflict }

/// <summary>A proposed change to one field of one contact. <see cref="Approved"/> is what the user ticks.</summary>
public sealed class FieldProposal
{
    public ContactField Field { get; init; }
    public string Label => Contact.LabelOf(Field);
    public string? Current { get; init; }
    public string? Incoming { get; init; }
    public ProposalKind Kind { get; init; }
    public string Reason { get; init; } = "";
    public bool Approved { get; set; }
    public bool IsActionable => Kind is ProposalKind.FillEmpty or ProposalKind.Overwrite;
}

/// <summary>All proposals for one matched contact.</summary>
public sealed class RecordProposal
{
    public int ContactId { get; init; }
    public string ContactName { get; init; } = "";
    public int RowNumber { get; init; }
    public string MatchedBy { get; init; } = "";
    public List<FieldProposal> Fields { get; init; } = new();
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
    public IEnumerable<FieldProposal> Actionable => Skipped ? Enumerable.Empty<FieldProposal>() : Fields.Where(f => f.IsActionable);
    public bool HasChanges => Actionable.Any();
}

public sealed record UnmatchedRow(int RowNumber, string? Email, string? Phone, string? Name);

/// <summary>The proposed update: nothing has been written when you hold one of these.</summary>
public sealed class BulkUpdatePlan
{
    public BulkUpdatePolicy Policy { get; init; }
    public List<RecordProposal> Records { get; } = new();
    public List<UnmatchedRow> Unmatched { get; } = new();
    public int IncomingRows { get; set; }
    public int Matched => Records.Count;
    public int WithChanges => Records.Count(r => r.HasChanges);
    public int Skipped => Records.Count(r => r.Skipped);
    public int Unchanged => Records.Count(r => !r.Skipped && !r.HasChanges);
    public int ProposedFieldChanges => Records.Sum(r => r.Actionable.Count());
    public int ApprovedFieldChanges => Records.Sum(r => r.Actionable.Count(f => f.Approved));
    public int ConflictingFields => Records.Sum(r => r.Fields.Count(f => f.Kind is (ProposalKind.Conflict or ProposalKind.KeptExisting or ProposalKind.Overwrite) && f.Current is not null && f.Incoming is not null));
}

/// <summary>Outcome of executing the approved changes.</summary>
public sealed record BulkUpdateResult(string BatchId, int ContactsUpdated, int FieldsChanged, int SkippedBecauseChanged, string? BackupPath);
