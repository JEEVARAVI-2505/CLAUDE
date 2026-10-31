using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.BulkUpdate;

/// <summary>An incoming row to compare. <see cref="ExistingId"/> pins the match (e.g. a proposal stored during import).</summary>
public sealed record IncomingContact(int RowNumber, Contact Contact, int? ExistingId = null);

/// <summary>Pure planning logic: matches rows to contacts, compares fields, applies the policy. Writes nothing.</summary>
public static class BulkUpdatePlanner
{
    public static BulkUpdatePlan Plan(IReadOnlyList<Contact> existing, IReadOnlyList<IncomingContact> incoming, BulkUpdatePolicy policy,
        IReadOnlyCollection<ContactField> selectedFields, MatchBy matchBy = MatchBy.EmailThenPhone, string countryCode = "91")
    {
        var plan = new BulkUpdatePlan { Policy = policy, IncomingRows = incoming.Count };
        var byId = existing.ToDictionary(c => c.Id);
        var byEmail = new Dictionary<string, Contact>(); var byPhone = new Dictionary<string, Contact>();
        foreach (var c in existing.OrderBy(c => c.Id))
        {
            if (c.EmailKey is not null) byEmail.TryAdd(c.EmailKey, c);
            foreach (var k in new[] { c.PhoneKey, c.AlternatePhoneKey }) if (k is not null) byPhone.TryAdd(k, c);
        }

        // PreserveExisting looks at every field; the others only at the user's selection.
        var fields = policy == BulkUpdatePolicy.PreserveExisting ? Contact.Fields.Select(f => f.Field).ToList() : selectedFields.ToList();
        var done = new HashSet<int>();

        foreach (var row in incoming)
        {
            var inc = row.Contact; ContactKeys.Apply(inc, countryCode);
            Contact? target = null; string how = "";
            if (row.ExistingId is { } id && byId.TryGetValue(id, out var pinned)) { target = pinned; how = "Matched during import"; }
            else
            {
                if (matchBy != MatchBy.PhoneOnly && inc.EmailKey is not null && byEmail.TryGetValue(inc.EmailKey, out var e)) { target = e; how = "Email"; }
                if (target is null && matchBy != MatchBy.EmailOnly)
                    foreach (var k in new[] { inc.PhoneKey, inc.AlternatePhoneKey })
                        if (k is not null && byPhone.TryGetValue(k, out var p)) { target = p; how = "Phone"; break; }
            }
            if (target is null) { plan.Unmatched.Add(new UnmatchedRow(row.RowNumber, inc.Email, inc.Phone, inc.DisplayName)); continue; }
            if (!done.Add(target.Id)) continue;   // several incoming rows for one contact: first wins

            var rec = new RecordProposal { ContactId = target.Id, ContactName = target.DisplayName, RowNumber = row.RowNumber, MatchedBy = how };
            foreach (var f in fields) rec.Fields.Add(Compare(f, target, inc, policy, countryCode));
            if (policy == BulkUpdatePolicy.SkipConflicts && rec.Fields.Any(x => x.Kind == ProposalKind.Conflict))
            { rec.Skipped = true; rec.SkipReason = "Conflicting values in: " + string.Join(", ", rec.Fields.Where(x => x.Kind == ProposalKind.Conflict).Select(x => x.Label)); }
            foreach (var fp in rec.Actionable) fp.Approved = policy != BulkUpdatePolicy.ManualApproval;
            plan.Records.Add(rec);
        }
        return plan;
    }

    private static FieldProposal Compare(ContactField f, Contact current, Contact incoming, BulkUpdatePolicy policy, string cc)
    {
        var cur = current.Get(f); var inc = incoming.Get(f);
        FieldProposal P(ProposalKind k, string reason) => new() { Field = f, Current = cur, Incoming = inc, Kind = k, Reason = reason };

        if (f == ContactField.FullName && cur is null && ContactQuality.HasName(current)) cur = current.DisplayName;
        if (inc is null) return P(ProposalKind.MissingIncoming, "Incoming value is empty");
        if (cur is not null && Equivalent(f, cur, inc, cc)) return P(ProposalKind.Same, "Same value");
        if (policy == BulkUpdatePolicy.OnlyIfValid && !IsValid(f, inc, cc)) return P(ProposalKind.RejectedInvalid, "Incoming value failed validation");
        if (cur is null) return P(ProposalKind.FillEmpty, "Field is empty");

        return policy switch
        {
            BulkUpdatePolicy.EmptyFieldsOnly or BulkUpdatePolicy.PreserveExisting => P(ProposalKind.KeptExisting, "Existing value preserved"),
            BulkUpdatePolicy.SkipConflicts => P(ProposalKind.Conflict, "Existing and incoming values differ"),
            _ => P(ProposalKind.Overwrite, "Existing value would be replaced")
        };
    }

    private static bool Equivalent(ContactField f, string a, string b, string cc) => f switch
    {
        ContactField.Email => ContactKeys.EmailKey(a) == ContactKeys.EmailKey(b),
        ContactField.Phone or ContactField.AlternatePhone => ContactKeys.PhoneKey(a, cc) == ContactKeys.PhoneKey(b, cc),
        ContactField.Company => CompanyRules.Key(a) == CompanyRules.Key(b) && CompanyRules.Key(a).Length > 0,
        _ => string.Equals(StringSimilarity.NormalizeText(a), StringSimilarity.NormalizeText(b), StringComparison.Ordinal)
    };

    private static bool IsValid(ContactField f, string v, string cc) => f switch
    {
        ContactField.Email => EmailRules.Check(v).IsValid,
        ContactField.Phone or ContactField.AlternatePhone => PhoneNumberParser.Parse(v, cc).IsValid,
        ContactField.Website => UrlRules.IsValid(v),
        ContactField.CreatedDate => DateRules.TryParse(v, out var d) && DateRules.IsPlausible(d),
        ContactField.Country => CountryTable.Find(v) is not null,
        _ => true
    };
}
