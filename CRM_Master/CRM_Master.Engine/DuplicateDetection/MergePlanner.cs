using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.DuplicateDetection;

/// <summary>For one field: every distinct value found across the records and the suggested choice.</summary>
public sealed class MergeFieldChoice
{
    public ContactField Field { get; init; }
    public string Label { get; init; } = "";
    /// <summary>Distinct non-empty values with the contact ids that hold them.</summary>
    public List<(string Value, List<int> ContactIds)> Candidates { get; init; } = new();
    public string? Selected { get; set; }
    public bool IsConflict => Candidates.Count > 1;
}

/// <summary>Builds the field-by-field merge proposal the user then adjusts. Nothing is merged until the user confirms.</summary>
public static class MergePlanner
{
    /// <summary>Default: keep the master's value; where the master is empty take the first non-empty value from the others. Notes are offered combined.</summary>
    public static List<MergeFieldChoice> Plan(Contact master, IReadOnlyList<Contact> others)
    {
        var all = new List<Contact> { master }; all.AddRange(others);
        var choices = new List<MergeFieldChoice>();
        foreach (var f in Contact.Fields)
        {
            var candidates = new List<(string Value, List<int> Ids)>();
            foreach (var c in all)
            {
                var v = c.Get(f.Field); if (v is null) continue;
                var existing = candidates.FindIndex(x => string.Equals(x.Value, v, StringComparison.OrdinalIgnoreCase));
                if (existing >= 0) candidates[existing].Ids.Add(c.Id); else candidates.Add((v, new List<int> { c.Id }));
            }
            if (f.Field == ContactField.Notes && candidates.Count > 1)
                candidates.Add((string.Join("\n---\n", candidates.Select(x => x.Value)), new List<int>()));   // "combine all" option
            var selected = master.Get(f.Field) ?? candidates.FirstOrDefault().Value;
            choices.Add(new MergeFieldChoice { Field = f.Field, Label = f.Label, Candidates = candidates.Select(c => (c.Value, c.Ids)).ToList(), Selected = selected });
        }
        return choices;
    }

    /// <summary>Picks the record to keep by default: the most complete one, then the oldest.</summary>
    public static Contact SuggestMaster(IReadOnlyList<Contact> contacts) =>
        contacts.OrderByDescending(c => Contact.Fields.Count(f => c.Get(f.Field) is not null)).ThenBy(c => c.CreatedAt).ThenBy(c => c.Id).First();
}
