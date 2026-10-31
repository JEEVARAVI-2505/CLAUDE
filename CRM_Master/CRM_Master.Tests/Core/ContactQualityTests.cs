using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;

namespace CRM_Master.Tests.Core;

public class ContactQualityTests
{
    [Fact]
    public void Complete_valid_contact_is_flagged_valid_and_complete()
    {
        var c = new Contact { FullName = "Ravi Kumar", Email = "ravi@acme.com", Phone = "9812345670" };
        ContactQuality.Evaluate(c, QualityOptions.Default);
        c.EmailState.Should().Be(ValidationState.Valid);
        c.PhoneState.Should().Be(ValidationState.Valid);
        c.IsIncomplete.Should().BeFalse();
    }

    [Fact]
    public void Missing_email_makes_contact_incomplete_under_default_rules()
    {
        var c = new Contact { FullName = "Ravi Kumar" };
        ContactQuality.Evaluate(c, QualityOptions.Default);
        c.EmailState.Should().Be(ValidationState.Missing);
        c.IsIncomplete.Should().BeTrue();
    }

    [Fact]
    public void First_and_last_name_satisfy_the_name_requirement()
    {
        var c = new Contact { FirstName = "Ravi", Email = "ravi@acme.com" };
        ContactQuality.Evaluate(c, QualityOptions.Default);
        c.IsIncomplete.Should().BeFalse();
    }

    [Fact]
    public void Invalid_email_and_phone_are_flagged()
    {
        var c = new Contact { FullName = "X", Email = "nope", Phone = "123" };
        ContactQuality.Evaluate(c, QualityOptions.Default);
        c.EmailState.Should().Be(ValidationState.Invalid);
        c.PhoneState.Should().Be(ValidationState.Invalid);
    }

    [Fact]
    public void Required_fields_are_configurable()
    {
        var c = new Contact { FullName = "Ravi", Email = "r@acme.com" };
        ContactQuality.Evaluate(c, new QualityOptions(new[] { ContactField.FullName, ContactField.Email, ContactField.Company }));
        c.IsIncomplete.Should().BeTrue();
    }

    [Fact]
    public void Keys_make_equivalent_records_comparable()
    {
        var a = new Contact { FullName = "Smith, John" , Email = " John@X.com ", Phone = "98765 43210" };
        var b = new Contact { FirstName = "John", LastName = "Smith", Email = "john@x.com", Phone = "+91 9876543210" };
        ContactKeys.Apply(a, "91"); ContactKeys.Apply(b, "91");
        a.EmailKey.Should().Be(b.EmailKey);
        a.PhoneKey.Should().Be(b.PhoneKey);
        a.NameKey.Should().Be(b.NameKey).And.Be("john smith");
    }

    [Fact]
    public void Diff_reports_only_changed_fields_and_serialises_both_sides()
    {
        var before = new Contact { FullName = "A", Email = "a@x.com", City = "Pune" };
        var after = before.Snapshot(); after.City = "Mumbai"; after.Phone = "9812345670";
        var diff = ContactDiff.Compute(before, after);
        diff.Select(d => d.Field).Should().BeEquivalentTo(new[] { ContactField.City, ContactField.Phone });
        ContactDiff.ToJson(diff, previous: true).Should().Contain("\"City\":\"Pune\"").And.Contain("\"Phone\":null");
        ContactDiff.ToJson(diff, previous: false).Should().Contain("\"City\":\"Mumbai\"");
    }

    [Fact]
    public void Get_and_Set_round_trip_every_field()
    {
        var c = new Contact();
        foreach (var f in Contact.Fields) c.Set(f.Field, f.Field == ContactField.CreatedDate ? "2024-03-05" : "v-" + f.Field).Should().BeTrue();
        foreach (var f in Contact.Fields) c.Get(f.Field).Should().Be(f.Field == ContactField.CreatedDate ? "2024-03-05" : "v-" + f.Field);
        c.Set(ContactField.CreatedDate, "garbage").Should().BeFalse();
    }
}
