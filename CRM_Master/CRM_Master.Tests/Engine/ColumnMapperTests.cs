using CRM_Master.Core.Entities;
using CRM_Master.Engine.Import;

namespace CRM_Master.Tests.Engine;

public class ColumnMapperTests
{
    private static ImportTable Table(string[] headers, params string?[][] rows) => new() { Headers = headers.ToList(), Rows = rows.ToList() };
    private static ContactField? Target(List<ColumnMapping> m, string header) => m.Single(x => x.SourceHeader == header).Target;

    [Fact]
    public void Standard_headers_map_exactly()
    {
        var m = ColumnMapper.Suggest(Table(new[] { "First Name", "Last Name", "Email", "Phone", "Company", "Job Title", "Website", "City", "State", "Country", "Lead Source", "Status", "Notes", "Address" }));
        Target(m, "First Name").Should().Be(ContactField.FirstName);
        Target(m, "Last Name").Should().Be(ContactField.LastName);
        Target(m, "Email").Should().Be(ContactField.Email);
        Target(m, "Phone").Should().Be(ContactField.Phone);
        Target(m, "Company").Should().Be(ContactField.Company);
        Target(m, "Job Title").Should().Be(ContactField.JobTitle);
        Target(m, "Website").Should().Be(ContactField.Website);
        Target(m, "Lead Source").Should().Be(ContactField.LeadSource);
        Target(m, "Status").Should().Be(ContactField.Status);
        Target(m, "Notes").Should().Be(ContactField.Notes);
        m.Should().OnlyContain(x => x.Confidence == 1.0);
    }

    [Theory]
    [InlineData("E-mail Address", ContactField.Email)]
    [InlineData("EMAIL_ID", ContactField.Email)]
    [InlineData("Mobile No.", ContactField.Phone)]
    [InlineData("Contact Number", ContactField.Phone)]
    [InlineData("Organisation", ContactField.Company)]
    [InlineData("Designation", ContactField.JobTitle)]
    [InlineData("Customer Name", ContactField.FullName)]
    [InlineData("Surname", ContactField.LastName)]
    [InlineData("fname", ContactField.FirstName)]
    [InlineData("Created On", ContactField.CreatedDate)]
    [InlineData("Alt Mobile", ContactField.AlternatePhone)]
    [InlineData("Secondary Phone", ContactField.AlternatePhone)]
    public void Differently_named_headers_are_understood(string header, ContactField expected) =>
        Target(ColumnMapper.Suggest(Table(new[] { header })), header).Should().Be(expected);

    [Fact]
    public void Header_containing_a_known_term_maps_with_lower_confidence()
    {
        var m = ColumnMapper.Suggest(Table(new[] { "Primary Contact Email Address" }));
        m[0].Target.Should().Be(ContactField.Email); m[0].Confidence.Should().BeLessThan(1.0);
    }

    [Fact]
    public void Typos_are_matched_fuzzily()
    {
        var m = ColumnMapper.Suggest(Table(new[] { "Emial Address", "Compnay Name" }));
        m[0].Target.Should().Be(ContactField.Email);
        m[1].Target.Should().Be(ContactField.Company);
    }

    [Fact]
    public void Unknown_headers_are_recognised_from_the_data()
    {
        var m = ColumnMapper.Suggest(Table(new[] { "Col A", "Col B", "Col C" },
            new[] { "a@x.com", "9812345670", "https://a.com" }, new[] { "b@x.com", "9812345671", "https://b.com" },
            new[] { "c@x.com", "9812345672", "https://c.com" }, new[] { "d@x.com", "9812345673", "https://d.com" }));
        m[0].Target.Should().Be(ContactField.Email);
        m[1].Target.Should().Be(ContactField.Phone);
        m[2].Target.Should().Be(ContactField.Website);
        m[0].Reason.Should().Contain("look like");
    }

    [Fact]
    public void Same_field_is_never_assigned_twice_second_phone_becomes_alternate()
    {
        var m = ColumnMapper.Suggest(Table(new[] { "Mobile", "Phone" }));
        m.Select(x => x.Target).Should().BeEquivalentTo(new ContactField?[] { ContactField.Phone, ContactField.AlternatePhone });
    }

    [Fact]
    public void Unrecognised_columns_are_left_unmapped()
    {
        var m = ColumnMapper.Suggest(Table(new[] { "Internal Ref", "Zzz" }, new[] { "1", "q" }, new[] { "2", "w" }, new[] { "3", "e" }));
        m.Should().OnlyContain(x => x.IsIgnored);
    }

    [Fact]
    public void Validation_requires_an_identifying_column()
    {
        var m = ColumnMapper.Suggest(Table(new[] { "City", "Country" }));
        ColumnMapper.Validate(m).Should().ContainSingle().Which.Should().Contain("Name, Email or Phone");
    }

    [Fact]
    public void Validation_rejects_two_columns_for_one_field()
    {
        var m = new List<ColumnMapping>
        {
            new() { SourceIndex = 0, SourceHeader = "A", Target = ContactField.Email }, new() { SourceIndex = 1, SourceHeader = "B", Target = ContactField.Email }
        };
        ColumnMapper.Validate(m).Should().Contain(e => e.Contains("More than one column"));
    }

    [Fact]
    public void Custom_columns_count_as_mapped_and_must_be_unique()
    {
        var m = new List<ColumnMapping>
        {
            new() { SourceIndex = 0, SourceHeader = "E", Target = ContactField.Email },
            new() { SourceIndex = 1, SourceHeader = "X", CustomFieldName = "Budget" }, new() { SourceIndex = 2, SourceHeader = "Y", CustomFieldName = "budget" }
        };
        ColumnMapper.Validate(m).Should().ContainSingle().Which.Should().Contain("Custom field name");
    }
}
