namespace CRM_Master.Core.Entities;

/// <summary>Standard contact fields that import, cleaning, bulk update and automation can address.</summary>
public enum ContactField
{
    FirstName, LastName, FullName, Company, JobTitle, Email, Phone, AlternatePhone, Website,
    Address, City, State, Country, LeadSource, Status, Notes, CreatedDate
}

/// <summary>Display metadata for a <see cref="ContactField"/>.</summary>
public sealed record ContactFieldInfo(ContactField Field, string Label, bool IsMultiline = false);

public partial class Contact
{
    /// <summary>All standard fields in display order.</summary>
    public static IReadOnlyList<ContactFieldInfo> Fields { get; } = new ContactFieldInfo[]
    {
        new(ContactField.FirstName, "First Name"), new(ContactField.LastName, "Last Name"), new(ContactField.FullName, "Full Name"),
        new(ContactField.Company, "Company"), new(ContactField.JobTitle, "Job Title"), new(ContactField.Email, "Email"),
        new(ContactField.Phone, "Phone"), new(ContactField.AlternatePhone, "Alternate Phone"), new(ContactField.Website, "Website"),
        new(ContactField.Address, "Address", true), new(ContactField.City, "City"), new(ContactField.State, "State"),
        new(ContactField.Country, "Country"), new(ContactField.LeadSource, "Lead Source"), new(ContactField.Status, "Contact Status"),
        new(ContactField.Notes, "Notes", true), new(ContactField.CreatedDate, "Created Date"),
    };

    public static string LabelOf(ContactField f) => Fields.First(x => x.Field == f).Label;

    /// <summary>Reads a field as text (dates as yyyy-MM-dd). Empty values are returned as null.</summary>
    public string? Get(ContactField field)
    {
        var v = field switch
        {
            ContactField.FirstName => FirstName, ContactField.LastName => LastName, ContactField.FullName => FullName,
            ContactField.Company => CompanyName, ContactField.JobTitle => JobTitle, ContactField.Email => Email,
            ContactField.Phone => Phone, ContactField.AlternatePhone => AlternatePhone, ContactField.Website => Website,
            ContactField.Address => Address, ContactField.City => City, ContactField.State => State, ContactField.Country => Country,
            ContactField.LeadSource => LeadSource, ContactField.Status => Status, ContactField.Notes => Notes,
            ContactField.CreatedDate => SourceCreatedDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            _ => null
        };
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    /// <summary>Writes a field from text. Returns false only for a date that cannot be parsed.</summary>
    public bool Set(ContactField field, string? value)
    {
        var v = string.IsNullOrWhiteSpace(value) ? null : value;
        switch (field)
        {
            case ContactField.FirstName: FirstName = v; break;
            case ContactField.LastName: LastName = v; break;
            case ContactField.FullName: FullName = v; break;
            case ContactField.Company: CompanyName = v; break;
            case ContactField.JobTitle: JobTitle = v; break;
            case ContactField.Email: Email = v; break;
            case ContactField.Phone: Phone = v; break;
            case ContactField.AlternatePhone: AlternatePhone = v; break;
            case ContactField.Website: Website = v; break;
            case ContactField.Address: Address = v; break;
            case ContactField.City: City = v; break;
            case ContactField.State: State = v; break;
            case ContactField.Country: Country = v; break;
            case ContactField.LeadSource: LeadSource = v; break;
            case ContactField.Status: Status = v ?? "Lead"; break;
            case ContactField.Notes: Notes = v; break;
            case ContactField.CreatedDate:
                if (v is null) { SourceCreatedDate = null; break; }
                if (!BusinessRules.DateRules.TryParse(v, out var d)) return false;
                SourceCreatedDate = d.Date; break;
        }
        return true;
    }

    /// <summary>Shallow copy of all standard fields (not keys, flags or custom values) - used for before/after diffs.</summary>
    public Contact Snapshot()
    {
        var c = new Contact { Id = Id, CompanyId = CompanyId, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt };
        foreach (var f in Fields) c.Set(f.Field, Get(f.Field));
        c.NeedsReview = NeedsReview; c.IsDuplicateFlagged = IsDuplicateFlagged; c.IsIncomplete = IsIncomplete;
        return c;
    }
}
