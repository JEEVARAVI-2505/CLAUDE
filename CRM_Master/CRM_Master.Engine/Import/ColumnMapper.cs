using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;

namespace CRM_Master.Engine.Import;

/// <summary>How one source column is used. Exactly one of Target / CustomFieldName is set, or neither (ignored).</summary>
public sealed class ColumnMapping
{
    public int SourceIndex { get; set; }
    public string SourceHeader { get; set; } = "";
    public ContactField? Target { get; set; }
    public string? CustomFieldName { get; set; }
    public double Confidence { get; set; }
    public string Reason { get; set; } = "";
    public bool IsIgnored => Target is null && CustomFieldName is null;
    public string TargetLabel => Target is { } t ? Contact.LabelOf(t) : CustomFieldName is { } c ? $"Custom: {c}" : "(ignore)";
}

/// <summary>Suggests and validates mappings from arbitrary spreadsheet headers to contact fields. Local rules only - no external AI.</summary>
public static class ColumnMapper
{
    private static readonly Dictionary<ContactField, string[]> Synonyms = new()
    {
        [ContactField.AlternatePhone] = new[] { "alternatephone", "altphone", "alternatenumber", "alternatemobile", "secondaryphone", "secondarymobile", "phone2", "mobile2", "othernumber", "alternatecontact", "altmobile", "altcontact", "alternatephonenumber", "alternativephone", "alternativenumber", "altnumber", "homephone", "phonenumber2", "mobilenumber2", "contactnumber2", "secondphone" },
        [ContactField.FirstName] = new[] { "firstname", "fname", "first", "givenname", "forename", "firstnames", "givennames" },
        [ContactField.LastName] = new[] { "lastname", "lname", "last", "surname", "familyname", "secondname" },
        [ContactField.FullName] = new[] { "fullname", "name", "contactname", "customername", "personname", "leadname", "clientname", "contact", "contactperson", "nameofperson", "candidatename", "prospectname", "ownername" },
        [ContactField.Company] = new[] { "company", "companyname", "organization", "organisation", "org", "employer", "business", "businessname", "account", "accountname", "firm", "organizationname", "organisationname", "currentcompany", "employername" },
        [ContactField.JobTitle] = new[] { "jobtitle", "title", "designation", "position", "role", "jobrole", "occupation", "jobposition", "currentposition", "currenttitle" },
        [ContactField.Email] = new[] { "email", "emailaddress", "mail", "emailid", "workemail", "primaryemail", "emailaddr", "mailid", "emailadd", "emailprimary" },
        [ContactField.Phone] = new[] { "phone", "phonenumber", "mobile", "mobilenumber", "mobileno", "cell", "cellphone", "contactnumber", "contactno", "tel", "telephone", "whatsapp", "phoneno", "primaryphone", "mob", "mobilephone", "workphone", "phonenum", "mobno", "whatsappnumber" },
        [ContactField.Website] = new[] { "website", "web", "url", "site", "websiteurl", "webaddress", "companywebsite", "homepage", "domain", "weburl" },
        [ContactField.Address] = new[] { "address", "streetaddress", "address1", "addressline1", "street", "fulladdress", "mailingaddress", "addressline", "postaladdress" },
        [ContactField.City] = new[] { "city", "town", "cityname", "locality", "citytown" },
        [ContactField.State] = new[] { "state", "province", "region", "statename", "stateprovince", "stateregion" },
        [ContactField.Country] = new[] { "country", "countryname", "nation", "countryregion" },
        [ContactField.LeadSource] = new[] { "leadsource", "source", "leadorigin", "channel", "origin", "referralsource", "campaign", "sourceofleads", "howdidyouhear" },
        [ContactField.Status] = new[] { "status", "contactstatus", "leadstatus", "stage", "leadstage", "lifecyclestage", "customerstatus" },
        [ContactField.Notes] = new[] { "notes", "note", "comments", "comment", "remarks", "description", "remark", "memo", "additionalinfo", "feedback" },
        [ContactField.CreatedDate] = new[] { "createddate", "createdon", "createdat", "datecreated", "dateadded", "addeddate", "created", "creationdate", "date", "signupdate", "registrationdate", "leaddate", "enquirydate" },
    };

    // Evaluate more specific fields first so "alternate phone" is not claimed by "phone".
    private static readonly ContactField[] Order =
    {
        ContactField.AlternatePhone, ContactField.FirstName, ContactField.LastName, ContactField.Email, ContactField.Phone, ContactField.Website,
        ContactField.JobTitle, ContactField.Company, ContactField.LeadSource, ContactField.CreatedDate, ContactField.Status, ContactField.Country,
        ContactField.State, ContactField.City, ContactField.Address, ContactField.Notes, ContactField.FullName
    };

    public static string Key(string? header) => new string((header ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    /// <summary>Suggests a target for every column, never assigning the same field twice (a second phone column becomes Alternate Phone).</summary>
    public static List<ColumnMapping> Suggest(ImportTable table, int sampleRows = 60)
    {
        var candidates = new List<(int col, ContactField field, double conf, string reason)>();
        for (var i = 0; i < table.Headers.Count; i++)
        {
            var best = Match(table.Headers[i]);
            if (best is null || best.Value.conf < 0.85)
            {
                var sniffed = Sniff(table, i, sampleRows);
                if (sniffed is not null && (best is null || sniffed.Value.conf > best.Value.conf)) best = sniffed;
            }
            if (best is { } b && b.conf >= 0.6) candidates.Add((i, b.field, b.conf, b.reason));
        }

        var mappings = table.Headers.Select((h, i) => new ColumnMapping { SourceIndex = i, SourceHeader = h, Reason = "No matching field found" }).ToList();
        var used = new HashSet<ContactField>();
        foreach (var c in candidates.OrderByDescending(c => c.conf).ThenBy(c => c.col))
        {
            var field = c.field;
            if (used.Contains(field))
            {
                if (field == ContactField.Phone && !used.Contains(ContactField.AlternatePhone)) field = ContactField.AlternatePhone;
                else continue;
            }
            used.Add(field);
            var m = mappings[c.col]; m.Target = field; m.Confidence = c.conf; m.Reason = c.reason;
        }
        return mappings;
    }

    // Generic words that appear in many unrelated headers ("Email Address", "Company Name") - only used as a last resort.
    private static readonly HashSet<string> Weak = new(StringComparer.Ordinal)
        { "address", "name", "contact", "title", "source", "status", "date", "web", "site", "role", "first", "last", "created", "note", "account", "business", "position", "origin", "channel", "region", "town", "cell", "tel", "mob", "org", "street" };

    private static (ContactField field, double conf, string reason)? Match(string header)
    {
        var key = Key(header);
        if (key.Length == 0) return null;
        foreach (var f in Order) if (Synonyms[f].Contains(key)) return (f, 1.0, "Column name matches");

        // 1) header contains a strong known term: "Primary Contact Email Address" -> email
        (ContactField field, double conf, string reason)? best = null; var bestLen = 0;
        foreach (var f in Order)
            foreach (var syn in Synonyms[f])
                if (syn.Length >= 4 && !Weak.Contains(syn) && key.Contains(syn, StringComparison.Ordinal) && syn.Length > bestLen)
                { best = (f, 0.9, $"Column name contains '{syn}'"); bestLen = syn.Length; }
        if (best is not null) return best;

        // 2) a word of the header is a typo of a term: "Emial Address", "Compnay Name"
        var words = header.Split(new[] { ' ', '_', '-', '.', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries).Select(Key).Where(w => w.Length >= 4 && !Weak.Contains(w));
        foreach (var w in words)
            foreach (var f in Order)
                foreach (var syn in Synonyms[f])
                {
                    if (syn.Length < 5 || Weak.Contains(syn.TrimEnd('1', '2'))) continue;
                    var sim = StringSimilarity.JaroWinkler(w, syn);
                    if (sim >= 0.92 && (best is null || sim * 0.9 > best.Value.conf)) best = (f, sim * 0.9, $"Similar to '{syn}'");
                }
        if (best is not null) return best;

        // 3) weak generic words ("Home Address", "Contact Name")
        foreach (var f in Order)
            foreach (var syn in Synonyms[f])
                if (syn.Length >= 4 && Weak.Contains(syn) && key.Contains(syn, StringComparison.Ordinal) && syn.Length > bestLen)
                { best = (f, 0.75, $"Column name contains '{syn}'"); bestLen = syn.Length; }
        return best;
    }

    /// <summary>Looks at sample values when the header says nothing (e.g. a column called "Col7" full of email addresses).</summary>
    private static (ContactField field, double conf, string reason)? Sniff(ImportTable t, int col, int sampleRows)
    {
        var values = t.Rows.Take(sampleRows * 4).Select(r => col < r.Length ? r[col] : null).Where(v => !string.IsNullOrWhiteSpace(v)).Take(sampleRows).Select(v => v!).ToList();
        if (values.Count < 3) return null;
        double Share(Func<string, bool> test) => values.Count(test) / (double)values.Count;
        if (Share(v => EmailRules.Check(v).IsValid) >= 0.7) return (ContactField.Email, 0.85, "Most values look like email addresses");
        if (Share(v => PhoneNumberParser.Parse(v).IsValid && v.Count(char.IsDigit) >= 10) >= 0.7) return (ContactField.Phone, 0.8, "Most values look like phone numbers");
        if (Share(v => v.Contains('.') && !v.Contains(' ') && UrlRules.IsValid(v)) >= 0.7) return (ContactField.Website, 0.75, "Most values look like web addresses");
        if (Share(v => v.Any(c => c is '/' or '-') && DateRules.TryParse(v, out _)) >= 0.8) return (ContactField.CreatedDate, 0.6, "Most values look like dates");
        return null;
    }

    /// <summary>Problems that prevent importing with this mapping (empty list = OK).</summary>
    public static List<string> Validate(IReadOnlyList<ColumnMapping> mappings)
    {
        var errors = new List<string>();
        var targets = mappings.Where(m => m.Target is not null).ToList();
        foreach (var g in targets.GroupBy(m => m.Target).Where(g => g.Count() > 1))
            errors.Add($"More than one column is mapped to '{Contact.LabelOf(g.Key!.Value)}': {string.Join(", ", g.Select(m => m.SourceHeader))}.");
        var identifying = new[] { ContactField.FullName, ContactField.FirstName, ContactField.LastName, ContactField.Email, ContactField.Phone };
        if (!targets.Any(m => identifying.Contains(m.Target!.Value)))
            errors.Add("Map at least one of Name, Email or Phone so each row can be identified.");
        foreach (var g in mappings.Where(m => m.CustomFieldName is not null).GroupBy(m => m.CustomFieldName!.Trim(), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            errors.Add($"Custom field name '{g.Key}' is used for more than one column.");
        return errors;
    }
}
