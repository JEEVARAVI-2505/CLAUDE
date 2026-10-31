using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Engine.Automation;
using CRM_Master.Engine.BulkUpdate;
using CRM_Master.Engine.DuplicateDetection;
using CRM_Master.Engine.Import;
using CRM_Master.Engine.Validation;
using CRM_Master.Tests.Support;

namespace CRM_Master.Tests.Engine;

/// <summary>Runs the bundled sample datasets through the real pipeline - what a new user does on first launch.</summary>
public class SampleDataTests : IAsyncLifetime
{
    private TestDb _db = null!; private ImportService _import = null!; private static string Sample(string f) => Path.Combine(AppContext.BaseDirectory, "SampleData", f);
    public async Task InitializeAsync()
    {
        _db = await TestDb.CreateAsync();
        var auto = new AutomationService(_db.Factory, _db.Settings, _db.Backups); await auto.SeedDefaultsAsync();
        _import = new ImportService(_db.Factory, _db.Settings, _db.Backups, auto);
    }
    public Task DisposeAsync() { _db.Dispose(); return Task.CompletedTask; }

    private async Task<ImportResult> LoadSample()
    {
        var t = await TableReader.ReadAsync(Sample("sample-contacts.csv"), 50);
        return await _import.ImportAsync(Sample("sample-contacts.csv"), _import.DefaultOptions(ColumnMapper.Suggest(t)) with { DuplicatePolicy = ImportDuplicatePolicy.ImportAndFlag });
    }

    [Fact]
    public async Task All_columns_of_the_sample_are_mapped_automatically()
    {
        var map = ColumnMapper.Suggest(await TableReader.ReadAsync(Sample("sample-contacts.csv"), 50));
        map.Where(m => m.IsIgnored).Select(m => m.SourceHeader).Should().BeEmpty();
        map.Single(m => m.SourceHeader == "Mobile").Target.Should().Be(ContactField.Phone);
        map.Single(m => m.SourceHeader == "Alternate Phone").Target.Should().Be(ContactField.AlternatePhone);
        map.Single(m => m.SourceHeader == "Designation").Target.Should().Be(ContactField.JobTitle);
        map.Single(m => m.SourceHeader == "Created On").Target.Should().Be(ContactField.CreatedDate);
    }

    [Fact]
    public async Task Sample_imports_cleanly_and_the_messy_values_show_up_in_data_quality_numbers()
    {
        var r = await LoadSample();
        r.Status.Should().NotBe(ImportJobStatus.Failed, r.Error);
        r.Imported.Should().BeGreaterThan(100);
        var all = await _db.Contacts.GetAllAsync();
        all.Count(c => c.EmailState == ValidationState.Invalid).Should().BeGreaterThan(3);
        all.Count(c => c.PhoneState == ValidationState.Invalid).Should().BeGreaterThan(2);
        all.Count(c => c.IsIncomplete).Should().BeGreaterThan(3);
        all.Where(c => c.FullName != null).Should().OnlyContain(c => c.FullName == c.FullName!.Trim());
        all.Should().Contain(c => c.Phone != null && c.Phone.StartsWith("+91 "));
    }

    [Fact]
    public async Task Planted_duplicates_are_found_but_uncertain_ones_are_never_auto_merged()
    {
        await LoadSample();
        var dup = new DuplicateService(_db.Factory, _db.Settings, _db.Backups, _db.Contacts);
        var scan = await dup.ScanAsync(dup.DefaultOptions());
        scan.Clusters.Count(c => c.Class == MatchClass.Exact).Should().BeGreaterThanOrEqualTo(4);
        scan.Clusters.Should().Contain(c => c.Class != MatchClass.Exact);
        var before = await _db.Contacts.CountAsync();
        var (groups, _) = await dup.MergeAllExactAsync();
        groups.Should().BeGreaterThanOrEqualTo(4);
        (await _db.Contacts.CountAsync()).Should().BeLessThan(before).And.BeGreaterThan(before - 20);
        (await dup.GetGroupsAsync()).TotalCount.Should().BeGreaterThan(0);          // uncertain groups remain for review
    }

    [Fact]
    public async Task Sample_update_file_produces_a_reviewable_plan()
    {
        await LoadSample();
        var bulk = new BulkUpdateService(_db.Factory, _db.Settings, _db.Backups, _db.Contacts);
        var map = ColumnMapper.Suggest(await TableReader.ReadAsync(Sample("sample-update.csv"), 50));
        var plan = await bulk.PlanFromFileAsync(Sample("sample-update.csv"), map, BulkUpdatePolicy.ManualApproval, new[] { ContactField.City, ContactField.JobTitle, ContactField.Company });
        plan.IncomingRows.Should().Be(35);
        plan.Matched.Should().BeGreaterThanOrEqualTo(20);
        plan.Unmatched.Count.Should().BeGreaterThanOrEqualTo(5);
        plan.ApprovedFieldChanges.Should().Be(0);
        plan.ProposedFieldChanges.Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task Validation_reports_typos_with_suggestions_on_the_sample()
    {
        await LoadSample();
        var summary = ValidationEngine.ValidateAll(await _db.Contacts.GetAllAsync(), ValidationRules.FromSettings(_db.Settings.Current));
        summary.Issues.Should().Contain(i => i.Code == "EMAIL_TYPO" && i.Suggestion!.EndsWith("@gmail.com"));
        summary.Issues.Should().Contain(i => i.Code == "COUNTRY_UNKNOWN" && i.Suggestion == "India");
        summary.Issues.Should().Contain(i => i.Code == "PHONE_SUSPICIOUS");
        summary.RecordsWithErrors.Should().BeGreaterThan(5);
    }
}
