using System.Text.Json;
using CRM_Master.Core.Entities;

namespace CRM_Master.Core.BusinessRules;

/// <summary>One changed field between two versions of a contact.</summary>
public sealed record FieldChange(ContactField Field, string? Before, string? After);

/// <summary>Computes field-level differences and renders them for the audit log.</summary>
public static class ContactDiff
{
    public static List<FieldChange> Compute(Contact before, Contact after)
    {
        var changes = new List<FieldChange>();
        foreach (var f in Contact.Fields)
        {
            var b = before.Get(f.Field); var a = after.Get(f.Field);
            if (!string.Equals(b, a, StringComparison.Ordinal)) changes.Add(new FieldChange(f.Field, b, a));
        }
        return changes;
    }

    /// <summary>{"Email":"a@x.com"} style JSON for one side of the diff.</summary>
    public static string ToJson(IEnumerable<FieldChange> changes, bool previous) =>
        JsonSerializer.Serialize(changes.ToDictionary(c => c.Field.ToString(), c => previous ? c.Before : c.After));

    /// <summary>Full JSON snapshot of all standard fields (used when a record is deleted or merged away).</summary>
    public static string SnapshotJson(Contact c) =>
        JsonSerializer.Serialize(Contact.Fields.ToDictionary(f => f.Field.ToString(), f => c.Get(f.Field)));
}
