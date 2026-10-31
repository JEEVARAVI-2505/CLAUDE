using System.Diagnostics;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Engine.Automation;
using CRM_Master.Engine.Import;
using CRM_Master.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CRM_Master.Tests.Engine;

public class ImportServiceTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _out;
    private TestDb _db = null!;
    private ImportService _svc = null!;
    private AutomationService _auto = null!;
    public ImportServiceTests(ITestOutputHelper output) => _out = output;

    public async Task InitializeAsync()
    {
        _db = await TestDb.CreateAsync();
        _auto = new AutomationService(_db.Factory, _db.Settings, _db.Backups);
        _svc = new ImportService(_db.Factory, _db.Settings, _db.Backups, _auto);
    }
    public Task DisposeAsync() { _db.Dispose(); return Task.CompletedTask; }

    private async Task<ImportOptions> AutoMap(string path, ImportDuplicatePolicy? policy = null)
    {
        var t = await TableReader.ReadAsync(path, 50);
        var o = _svc.DefaultOptions(ColumnMapper.Suggest(t));
        return policy is null ? o : o with { DuplicatePolicy = policy.Value };
    }

    private static string[][] Sample() => new[]
    {
        Files.StandardHeader,
        new[] { "RAVI", "KUMAR", " Ravi.Kumar@Acme.COM ", "98765 43210", "Acme Pvt Ltd", "pune" },
        new[] { "asha", "rao", "asha@beta.in", "+91-98123-45671", "Beta Ltd", "Delhi" },
        new[] { "John", "Smith", "john@gamma.com", "9812345672", "", "Mumbai" },
    };

    [Fact]
    public async Task Csv_import_creates_cleaned_contacts_job_and_audit()
    {
        var path = Files.Csv(_db.Folder, "leads.csv", Sample());
        var r = await _svc.ImportAsync(path, await AutoMap(path));

        r.Status.Should().Be(ImportJobStatus.Completed);
        (r.Total, r.Imported, r.Failed, r.Duplicates).Should().Be((3, 3, 0, 0));
        var all = await _db.Contacts.GetAllAsync();
        var ravi = all.Single(c => c.Email == "ravi.kumar@acme.com");
        ravi.FullName.Should().Be("Ravi Kumar");               // composed + proper-cased
        ravi.Phone.Should().Be("+91 98765 43210");              // standardised
        ravi.PhoneKey.Should().Be("919876543210");
        ravi.ImportJobId.Should().Be(r.JobId);
        (await _svc.HistoryAsync()).Single().ImportedRows.Should().Be(3);
        (await _db.Audit.QueryAsync(new(Operation: AuditOperation.Import))).Items.Single().Summary.Should().Contain("Imported 3 of 3");
    }

    [Fact]
    public async Task Xlsx_import_works_and_keeps_numeric_phones_intact()
    {
        var rows = new object?[][]
        {
            new object?[] { "Name", "E-mail", "Mobile No", "Created" },
            new object?[] { "Priya Nair", "priya@x.com", 9812345670L, new DateTime(2023, 7, 1) },
        };
        var path = Files.Xlsx(_db.Folder, "leads.xlsx", rows);
        var r = await _svc.ImportAsync(path, await AutoMap(path));
        r.Imported.Should().Be(1);
        var c = (await _db.Contacts.GetAllAsync()).Single();
        c.Phone.Should().Be("+91 98123 45670");
        c.SourceCreatedDate.Should().Be(new DateTime(2023, 7, 1));
    }

    [Fact]
    public async Task Duplicate_emails_and_phones_inside_the_file_are_detected_in_any_format()
    {
        var path = Files.Csv(_db.Folder, "dups.csv", new[]
        {
            Files.StandardHeader,
            new[] { "A", "One", "same@x.com", "9812345670", "", "" },
            new[] { "B", "Two", "SAME@x.com", "", "", "" },                 // duplicate email (case differs)
            new[] { "C", "Three", "c@x.com", "+91 98123-45670", "", "" },   // duplicate phone (format differs)
            new[] { "D", "Four", "d@x.com", "9812345679", "", "" },
        });
        var r = await _svc.ImportAsync(path, await AutoMap(path));
        (r.Imported, r.Duplicates).Should().Be((2, 2));
        r.Status.Should().Be(ImportJobStatus.CompletedWithWarnings);
        var recs = await _svc.RecordsAsync(r.JobId, ImportRecordStatus.Duplicate);
        recs.Select(x => x.RowNumber).Should().Equal(3, 4);
        recs[0].Message.Should().Contain("row 2");
    }

    [Fact]
    public async Task Rows_matching_existing_contacts_are_skipped_by_default()
    {
        await _db.Contacts.AddAsync(TestDb.Person("Existing Person", "dup@x.com", "9812345670"));
        var path = Files.Csv(_db.Folder, "e.csv", new[] { Files.StandardHeader,
            new[] { "New", "Guy", "dup@x.com", "", "", "" }, new[] { "Phone", "Dup", "other@x.com", "09812345670", "", "" }, new[] { "Fresh", "One", "fresh@x.com", "", "", "" } });
        var r = await _svc.ImportAsync(path, await AutoMap(path));
        (r.Imported, r.Duplicates).Should().Be((1, 2));
        (await _db.Contacts.CountAsync()).Should().Be(2);
        (await _svc.RecordsAsync(r.JobId, ImportRecordStatus.Duplicate)).Should().OnlyContain(x => x.ContactId != null);
    }

    [Fact]
    public async Task Import_and_flag_policy_imports_duplicates_marked_for_review()
    {
        await _db.Contacts.AddAsync(TestDb.Person("Existing", "dup@x.com"));
        var path = Files.Csv(_db.Folder, "f.csv", new[] { Files.StandardHeader, new[] { "Dup", "Licate", "dup@x.com", "", "", "" } });
        var r = await _svc.ImportAsync(path, await AutoMap(path, ImportDuplicatePolicy.ImportAndFlag));
        r.Imported.Should().Be(1);
        var flagged = (await _db.Contacts.QueryAsync(new(Quality: QualityFilter.FlaggedDuplicate))).Items.Single();
        flagged.NeedsReview.Should().BeTrue();
    }

    [Fact]
    public async Task Propose_update_policy_stores_incoming_values_without_touching_the_existing_contact()
    {
        var existing = await _db.Contacts.AddAsync(TestDb.Person("Old Name", "dup@x.com", city: "Pune"));
        var path = Files.Csv(_db.Folder, "p.csv", new[] { new[] { "Name", "Email", "City" }, new[] { "New Name", "dup@x.com", "Mumbai" } });
        var r = await _svc.ImportAsync(path, await AutoMap(path, ImportDuplicatePolicy.ProposeUpdate));
        (r.Imported, r.Proposed).Should().Be((0, 1));
        var rec = (await _svc.RecordsAsync(r.JobId, ImportRecordStatus.Proposed)).Single();
        rec.ContactId.Should().Be(existing.Id);
        rec.RawJson.Should().Contain("Mumbai").And.Contain("New Name");
        (await _db.Contacts.GetAsync(existing.Id))!.City.Should().Be("Pune");
    }

    [Fact]
    public async Task Rows_without_any_identifying_data_fail_with_their_raw_values_kept()
    {
        var path = Files.Csv(_db.Folder, "i.csv", new[] { Files.StandardHeader,
            new[] { "", "", "", "", "Orphan Co", "Pune" }, new[] { "Ok", "Person", "ok@x.com", "", "", "" }, new[] { "", "", "", "", "", "" } });
        var r = await _svc.ImportAsync(path, await AutoMap(path));
        (r.Imported, r.Failed).Should().Be((1, 1));
        var failed = (await _svc.RecordsAsync(r.JobId, ImportRecordStatus.Failed)).Single();
        failed.RowNumber.Should().Be(2);
        failed.RawJson.Should().Contain("Orphan Co");
    }

    [Fact]
    public async Task Invalid_email_and_phone_are_imported_but_flagged_in_quality_counts()
    {
        var path = Files.Csv(_db.Folder, "q.csv", new[] { Files.StandardHeader, new[] { "Bad", "Data", "not-an-email", "123", "", "" } });
        var r = await _svc.ImportAsync(path, await AutoMap(path));
        r.Imported.Should().Be(1);
        var c = (await _db.Contacts.GetAllAsync()).Single();
        (c.EmailState, c.PhoneState).Should().Be((ValidationState.Invalid, ValidationState.Invalid));
    }

    [Fact]
    public async Task Missing_identifying_columns_fail_before_anything_is_written()
    {
        var path = Files.Csv(_db.Folder, "m.csv", new[] { new[] { "City", "Country" }, new[] { "Pune", "India" } });
        var r = await _svc.ImportAsync(path, await AutoMap(path));
        r.Status.Should().Be(ImportJobStatus.Failed);
        r.Error.Should().Contain("Name, Email or Phone");
        (await _db.Contacts.CountAsync()).Should().Be(0);
        (await _db.Audit.QueryAsync(new(Status: OperationStatus.Failed))).TotalCount.Should().Be(1);
    }

    [Theory]
    [InlineData("empty.csv", "")]
    [InlineData("blank.csv", "\n\n  \n")]
    public async Task Empty_files_fail_gracefully_and_are_logged(string name, string content)
    {
        var path = Path.Combine(_db.Folder, name); await File.WriteAllTextAsync(path, content);
        var opts = new ImportOptions(new[] { new ColumnMapping { SourceIndex = 0, SourceHeader = "Email", Target = ContactField.Email } });
        var r = await _svc.ImportAsync(path, opts);
        r.Status.Should().Be(ImportJobStatus.Failed);
        r.Error.Should().NotBeNullOrWhiteSpace();
        (await _svc.HistoryAsync()).Single().Status.Should().Be(ImportJobStatus.Failed);
    }

    [Fact]
    public async Task Corrupted_workbook_fails_gracefully_without_exception()
    {
        var path = Path.Combine(_db.Folder, "corrupt.xlsx"); await File.WriteAllBytesAsync(path, Enumerable.Range(0, 3000).Select(i => (byte)(i % 200)).ToArray());
        var opts = new ImportOptions(new[] { new ColumnMapping { SourceIndex = 0, SourceHeader = "Email", Target = ContactField.Email } });
        var r = await _svc.ImportAsync(path, opts);
        r.Status.Should().Be(ImportJobStatus.Failed);
        r.Error.Should().Contain("corrupted");
        (await _db.Contacts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Cancellation_rolls_back_everything()
    {
        var rows = new List<string[]> { new[] { "Name", "Email" } };
        rows.AddRange(Enumerable.Range(1, 5000).Select(i => new[] { $"Person {i}", $"p{i}@example{i}.com" }));
        var path = Files.Csv(_db.Folder, "cancel.csv", rows);
        using var cts = new CancellationTokenSource();
        var syncProgress = new SyncProgress(p => { if (p.Stage == "Importing" && p.Done >= 2000) cts.Cancel(); });

        var r = await _svc.ImportAsync(path, await AutoMap(path), syncProgress, cts.Token);
        r.Status.Should().Be(ImportJobStatus.Cancelled);
        (await _db.Contacts.CountAsync()).Should().Be(0);
        (await _db.Audit.QueryAsync(new(Status: OperationStatus.Cancelled))).TotalCount.Should().Be(1);
    }

    private sealed class SyncProgress : IProgress<ProgressInfo>
    {
        private readonly Action<ProgressInfo> _a; public SyncProgress(Action<ProgressInfo> a) => _a = a; public void Report(ProgressInfo v) => _a(v);
    }

    [Fact]
    public async Task Custom_columns_become_custom_fields_with_values()
    {
        var path = Files.Csv(_db.Folder, "c.csv", new[] { new[] { "Name", "Email", "Budget" }, new[] { "A B", "a@x.com", "50000" } });
        var t = await TableReader.ReadAsync(path);
        var map = ColumnMapper.Suggest(t); map.Single(m => m.SourceHeader == "Budget").CustomFieldName = "Budget";
        var r = await _svc.ImportAsync(path, _svc.DefaultOptions(map));
        r.Imported.Should().Be(1);
        var c = (await _db.Contacts.GetAllAsync()).Single();
        (await _db.Contacts.GetCustomValuesAsync(c.Id)).Should().Contain("Budget", "50000");
    }

    [Fact]
    public async Task Automation_rules_run_during_import_and_are_recorded()
    {
        await _auto.SeedDefaultsAsync();
        var path = Files.Csv(_db.Folder, "r.csv", Sample());
        await _svc.ImportAsync(path, await AutoMap(path));
        var john = (await _db.Contacts.GetAllAsync()).Single(c => c.FullName == "John Smith");
        john.NeedsReview.Should().BeTrue();                      // "Missing company = review"
        var history = await _auto.HistoryAsync();
        history.Should().Contain(h => h.Rule!.Name == "Missing company = review" && h.AffectedCount == 1);
        history.Should().Contain(h => h.Rule!.Name == "Normalize phone on import");
    }

    [Fact]
    public async Task Backup_is_taken_before_importing_into_a_non_empty_database()
    {
        await _db.Contacts.AddAsync(TestDb.Person("Existing", "e@x.com"));
        var path = Files.Csv(_db.Folder, "b.csv", Sample());
        await _svc.ImportAsync(path, await AutoMap(path));
        (await _db.Backups.ListAsync()).Should().ContainSingle(b => b.Kind == BackupKind.PreOperation && b.ContactCount == 1);
    }

    [Fact]
    public async Task Preview_reports_each_outcome_without_writing_data()
    {
        await _db.Contacts.AddAsync(TestDb.Person("Existing", "exists@x.com"));
        var path = Files.Csv(_db.Folder, "pv.csv", new[] { new[] { "Name", "Email" },
            new[] { "New One", "new@x.com" }, new[] { "Exists", "EXISTS@x.com" }, new[] { "Twin", "new@x.com" }, new[] { "", "" }, new[] { "Bad Mail", "bad" } });
        var pv = await _svc.PreviewAsync(path, await AutoMap(path));
        (pv.Total, pv.Ready, pv.Warnings, pv.DuplicateExisting, pv.DuplicateInFile).Should().Be((4, 1, 1, 1, 1));   // fully blank rows never reach the classifier
        pv.Sample.Should().HaveCount(4);
        (await _db.Contacts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Cleaning_on_import_can_be_disabled()
    {
        var path = Files.Csv(_db.Folder, "n.csv", new[] { new[] { "Name", "Email", "Phone" }, new[] { "RAVI KUMAR", "R@X.COM", "98765 43210" } });
        var r = await _svc.ImportAsync(path, (await AutoMap(path)) with { CleanOnImport = false });
        r.Imported.Should().Be(1);
        var c = (await _db.Contacts.GetAllAsync()).Single();
        (c.FullName, c.Email, c.Phone).Should().Be(("RAVI KUMAR", "R@X.COM", "98765 43210"));
        c.EmailKey.Should().Be("r@x.com");   // keys are always normalised so duplicate checks still work
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task Fifty_thousand_rows_import_in_a_single_transaction()
    {
        var rows = new List<string[]> { new[] { "First Name", "Last Name", "Email", "Mobile", "Company", "City" } };
        for (var i = 0; i < 50_000; i++)
            rows.Add(new[] { $"First{i}", $"Last{i}", $"user{i}@company{i % 2000}.com", (9000000000L + i).ToString(), $"Company {i % 2000} Pvt Ltd", $"City{i % 50}" });
        var path = Files.Csv(_db.Folder, "50k.csv", rows);

        var sw = Stopwatch.StartNew();
        var r = await _svc.ImportAsync(path, await AutoMap(path));
        _out.WriteLine($"Imported {r.Imported:N0} rows in {sw.Elapsed.TotalSeconds:F1}s");
        r.Imported.Should().Be(50_000);
        r.Failed.Should().Be(0);
        (await _db.Contacts.CountAsync()).Should().Be(50_000);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(2));

        var sw2 = Stopwatch.StartNew();
        var page = await _db.Contacts.QueryAsync(new(Search: "user4999", PageSize: 100));
        _out.WriteLine($"Search in 50k contacts: {sw2.ElapsedMilliseconds} ms ({page.TotalCount} hits)");
        sw2.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));

        await using var ctx = _db.Factory.CreateDbContext();
        (await ctx.Companies.CountAsync()).Should().Be(2000);
    }
}
