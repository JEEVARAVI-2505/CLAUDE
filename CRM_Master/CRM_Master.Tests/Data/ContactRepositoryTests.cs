using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Tests.Data;

public class ContactRepositoryTests
{
    [Fact]
    public async Task Add_computes_keys_quality_company_and_audits()
    {
        using var db = await TestDb.CreateAsync();
        var c = await db.Contacts.AddAsync(TestDb.Person("Ravi Kumar", " Ravi@Acme.COM ", "98765 43210", "Acme Pvt Ltd"));
        var stored = (await db.Contacts.GetAsync(c.Id))!;
        stored.EmailKey.Should().Be("ravi@acme.com");
        stored.PhoneKey.Should().Be("919876543210");
        stored.NameKey.Should().Be("kumar ravi");
        stored.EmailState.Should().Be(ValidationState.Valid);
        stored.PhoneState.Should().Be(ValidationState.Valid);
        stored.IsIncomplete.Should().BeFalse();
        stored.CompanyId.Should().NotBeNull();
        var log = (await db.Audit.QueryAsync(new(Operation: AuditOperation.ContactCreated))).Items.Single();
        log.RecordId.Should().Be(c.Id);
        log.NewValue.Should().Contain("Ravi Kumar");
    }

    [Fact]
    public async Task Companies_are_shared_by_normalised_name()
    {
        using var db = await TestDb.CreateAsync();
        await db.Contacts.AddAsync(TestDb.Person("A", company: "Infosys Ltd"));
        await db.Contacts.AddAsync(TestDb.Person("B", company: "INFOSYS Limited"));
        await using var ctx = db.Factory.CreateDbContext();
        (await ctx.Companies.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Update_audits_previous_and_new_values_only_for_changed_fields()
    {
        using var db = await TestDb.CreateAsync();
        var c = await db.Contacts.AddAsync(TestDb.Person("Ravi", "r@acme.com", city: "Pune"));
        var edit = (await db.Contacts.GetAsync(c.Id))!;
        edit.City = "Mumbai"; edit.Phone = "9812345670";
        await db.Contacts.UpdateAsync(edit);

        var log = (await db.Audit.QueryAsync(new(Operation: AuditOperation.ContactUpdated, RecordId: c.Id))).Items.Single();
        log.PreviousValue.Should().Contain("\"City\":\"Pune\"").And.NotContain("Email");
        log.NewValue.Should().Contain("\"City\":\"Mumbai\"").And.Contain("9812345670");
        (await db.Contacts.GetAsync(c.Id))!.PhoneState.Should().Be(ValidationState.Valid);
    }

    [Fact]
    public async Task Update_without_changes_writes_no_audit_row()
    {
        using var db = await TestDb.CreateAsync();
        var c = await db.Contacts.AddAsync(TestDb.Person("Ravi", "r@acme.com"));
        await db.Contacts.UpdateAsync((await db.Contacts.GetAsync(c.Id))!);
        (await db.Audit.QueryAsync(new(Operation: AuditOperation.ContactUpdated))).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Custom_fields_are_created_updated_and_audited()
    {
        using var db = await TestDb.CreateAsync();
        var c = await db.Contacts.AddAsync(TestDb.Person("Ravi"), new Dictionary<string, string?> { ["Budget"] = "50000" });
        (await db.Contacts.GetCustomValuesAsync(c.Id)).Should().ContainKey("Budget").WhoseValue.Should().Be("50000");
        await db.Contacts.UpdateAsync((await db.Contacts.GetAsync(c.Id))!, new Dictionary<string, string?> { ["budget"] = "75000" }); // case-insensitive field match
        (await db.Contacts.GetCustomValuesAsync(c.Id))["Budget"].Should().Be("75000");
        (await db.Contacts.GetCustomFieldsAsync()).Should().ContainSingle();
        (await db.Audit.QueryAsync(new(Operation: AuditOperation.ContactUpdated))).Items.Single().NewValue.Should().Contain("Custom:budget");
    }

    [Fact]
    public async Task Delete_removes_contact_and_dependents_but_keeps_full_snapshot_in_audit()
    {
        using var db = await TestDb.CreateAsync();
        var c = await db.Contacts.AddAsync(TestDb.Person("Gone Soon", "g@x.com"), new Dictionary<string, string?> { ["Tier"] = "Gold" });
        (await db.Contacts.DeleteAsync(new[] { c.Id })).Should().Be(1);
        (await db.Contacts.GetAsync(c.Id)).Should().BeNull();
        await using var ctx = db.Factory.CreateDbContext();
        (await ctx.ContactCustomFieldValues.CountAsync()).Should().Be(0);
        var log = (await db.Audit.QueryAsync(new(Operation: AuditOperation.ContactDeleted))).Items.Single();
        log.PreviousValue.Should().Contain("g@x.com").And.Contain("Gone Soon").And.Contain("Custom:Tier");
    }

    [Fact]
    public async Task Query_supports_search_filter_sort_and_paging()
    {
        using var db = await TestDb.CreateAsync();
        for (var i = 1; i <= 25; i++)
            await db.Contacts.AddAsync(new Contact { FullName = $"Person {i:00}", Email = i % 5 == 0 ? "bad-email" : $"p{i}@x.com", City = i <= 10 ? "Pune" : "Delhi", Status = i % 2 == 0 ? "Customer" : "Lead" });

        var page2 = await db.Contacts.QueryAsync(new(SortBy: "FullName", Descending: false, Page: 2, PageSize: 10));
        page2.TotalCount.Should().Be(25); page2.TotalPages.Should().Be(3);
        page2.Items.First().FullName.Should().Be("Person 11");

        (await db.Contacts.QueryAsync(new(Search: "person 07"))).Items.Select(c => c.FullName).Should().Equal("Person 07");
        (await db.Contacts.QueryAsync(new(Search: "pune"))).TotalCount.Should().Be(10);
        (await db.Contacts.QueryAsync(new(Status: "Customer"))).TotalCount.Should().Be(12);
        (await db.Contacts.QueryAsync(new(Quality: QualityFilter.InvalidEmail))).TotalCount.Should().Be(5);
        (await db.Contacts.QueryAsync(new(Search: "p3@x"))).TotalCount.Should().Be(1);
        (await db.Contacts.QueryIdsAsync(new(Status: "Lead"))).Count.Should().Be(13);
        (await db.Contacts.QueryAsync(new(Page: 99, PageSize: 10))).Page.Should().Be(3);   // clamped
    }

    [Fact]
    public async Task Phone_search_matches_any_formatting()
    {
        using var db = await TestDb.CreateAsync();
        await db.Contacts.AddAsync(TestDb.Person("Phone Guy", phone: "+91 98765 43210"));
        (await db.Contacts.QueryAsync(new(Search: "9876543210"))).TotalCount.Should().Be(1);
        (await db.Contacts.QueryAsync(new(Search: "98765-43"))).TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task History_returns_audit_entries_for_that_contact_only()
    {
        using var db = await TestDb.CreateAsync();
        var a = await db.Contacts.AddAsync(TestDb.Person("A")); var b = await db.Contacts.AddAsync(TestDb.Person("B"));
        var edit = (await db.Contacts.GetAsync(a.Id))!; edit.City = "X"; await db.Contacts.UpdateAsync(edit);
        var h = await db.Contacts.GetHistoryAsync(a.Id);
        h.Entries.Should().HaveCount(2).And.OnlyContain(e => e.RecordId == a.Id);
        (await db.Contacts.GetHistoryAsync(b.Id)).Entries.Should().HaveCount(1);
    }

    [Fact]
    public async Task Reevaluate_applies_new_required_field_rules()
    {
        using var db = await TestDb.CreateAsync();
        await db.Contacts.AddAsync(TestDb.Person("Ravi", "r@acme.com"));
        (await db.Contacts.QueryAsync(new(Quality: QualityFilter.Incomplete))).TotalCount.Should().Be(0);
        var s = db.Settings.Current.Clone(); s.RequiredFields = new() { "FullName", "Email", "Phone" };
        await db.Settings.SaveAsync(s);
        (await db.Contacts.ReevaluateQualityAsync()).Should().Be(1);
        (await db.Contacts.QueryAsync(new(Quality: QualityFilter.Incomplete))).TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Distinct_values_for_filters()
    {
        using var db = await TestDb.CreateAsync();
        await db.Contacts.AddAsync(new Contact { FullName = "A", Country = "India" });
        await db.Contacts.AddAsync(new Contact { FullName = "B", Country = "India" });
        await db.Contacts.AddAsync(new Contact { FullName = "C", Country = "UAE" });
        (await db.Contacts.GetDistinctAsync(ContactField.Country)).Should().Equal("India", "UAE");
    }
}
