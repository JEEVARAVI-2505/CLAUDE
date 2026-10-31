using CRM_Master.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM_Master.Data.Configurations;

internal sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> b)
    {
        b.ToTable("Contacts", t => t.HasCheckConstraint("CK_Contacts_Status", "length(trim(Status)) > 0"));
        b.HasKey(c => c.Id);
        b.Ignore(c => c.DisplayName);
        foreach (var p in new[] { nameof(Contact.FirstName), nameof(Contact.LastName), nameof(Contact.FullName), nameof(Contact.CompanyName), nameof(Contact.JobTitle) })
            b.Property(p).HasMaxLength(200);
        b.Property(c => c.Email).HasMaxLength(320);
        b.Property(c => c.EmailKey).HasMaxLength(320);
        b.Property(c => c.Phone).HasMaxLength(60);
        b.Property(c => c.AlternatePhone).HasMaxLength(60);
        b.Property(c => c.PhoneKey).HasMaxLength(30);
        b.Property(c => c.AlternatePhoneKey).HasMaxLength(30);
        b.Property(c => c.NameKey).HasMaxLength(250);
        b.Property(c => c.Website).HasMaxLength(500);
        b.Property(c => c.Address).HasMaxLength(500);
        b.Property(c => c.City).HasMaxLength(120);
        b.Property(c => c.State).HasMaxLength(120);
        b.Property(c => c.Country).HasMaxLength(120);
        b.Property(c => c.LeadSource).HasMaxLength(120);
        b.Property(c => c.Status).HasMaxLength(60).IsRequired();
        b.Property(c => c.Notes).HasMaxLength(8000);

        b.HasIndex(c => c.EmailKey);
        b.HasIndex(c => c.PhoneKey);
        b.HasIndex(c => c.AlternatePhoneKey);
        b.HasIndex(c => c.NameKey);
        b.HasIndex(c => c.CompanyName);
        b.HasIndex(c => c.Status);
        b.HasIndex(c => c.CreatedAt);
        b.HasIndex(c => c.UpdatedAt);
        b.HasIndex(c => c.ImportJobId);
        b.HasIndex(c => new { c.EmailState, c.PhoneState, c.IsIncomplete });

        b.HasOne(c => c.Company).WithMany(co => co.Contacts).HasForeignKey(c => c.CompanyId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<ImportJob>().WithMany().HasForeignKey(c => c.ImportJobId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(c => c.CustomValues).WithOne(v => v.Contact).HasForeignKey(v => v.ContactId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.ToTable("Companies", t => t.HasCheckConstraint("CK_Companies_Name", "length(trim(Name)) > 0"));
        b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        b.Property(c => c.NameKey).HasMaxLength(200).IsRequired();
        b.Property(c => c.Website).HasMaxLength(500);
        b.HasIndex(c => c.NameKey).IsUnique();
    }
}

internal sealed class CustomFieldConfiguration : IEntityTypeConfiguration<CustomField>
{
    public void Configure(EntityTypeBuilder<CustomField> b)
    {
        b.ToTable("CustomFields", t => t.HasCheckConstraint("CK_CustomFields_Name", "length(trim(Name)) > 0"));
        b.Property(c => c.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
        b.HasIndex(c => c.Name).IsUnique();
    }
}

internal sealed class ContactCustomFieldValueConfiguration : IEntityTypeConfiguration<ContactCustomFieldValue>
{
    public void Configure(EntityTypeBuilder<ContactCustomFieldValue> b)
    {
        b.ToTable("ContactCustomFieldValues");
        b.Property(v => v.Value).HasMaxLength(2000);
        b.HasIndex(v => new { v.ContactId, v.CustomFieldId }).IsUnique();
        b.HasOne(v => v.CustomField).WithMany().HasForeignKey(v => v.CustomFieldId).OnDelete(DeleteBehavior.Cascade);
    }
}
