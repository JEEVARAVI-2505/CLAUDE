using CRM_Master.Core.Entities;
using CRM_Master.Engine;
using CRM_Master.Engine.Cleaning;
using CRM_Master.Engine.Validation;
using CRM_Master.Tests.Support;

namespace CRM_Master.Tests.Engine;

public class ValidationTests
{
    private static ValidationRules Rules() => new();
    private static List<ValidationIssue> Check(Contact c) => ValidationEngine.ValidateContact(c, Rules());
    private static bool Has(List<ValidationIssue> l, string code) => l.Any(i => i.Code == code);

    [Fact]
    public void Clean_record_has_no_errors_or_warnings()
    {
        var issues = Check(new Contact { FullName = "Ravi Kumar", Email = "ravi@acme.com", Phone = "+91 98123 45670", CompanyName = "Acme", Website = "https://acme.com", Country = "India" });
        issues.Should().BeEmpty();
    }

    [Fact]
    public void Missing_required_fields_are_errors()
    {
        var issues = Check(new Contact { City = "Pune" });
        issues.Where(i => i.Code == "REQUIRED_MISSING").Select(i => i.Field).Should().BeEquivalentTo(new ContactField?[] { ContactField.FullName, ContactField.Email });
        issues.Should().OnlyContain(i => i.Severity == Severity.Error);
    }

    [Fact]
    public void Invalid_email_has_reason_and_typo_suggestion()
    {
        var i = Check(new Contact { FullName = "A", Email = "ravi@gmial.com" }).Single(x => x.Code == "EMAIL_TYPO");
        i.Suggestion.Should().Be("ravi@gmail.com");
        var bad = Check(new Contact { FullName = "A", Email = "ravi@@x.com" }).Single(x => x.Code == "EMAIL_INVALID");
        bad.Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void Syntactically_valid_placeholder_disposable_and_role_emails_are_flagged_but_never_called_verified()
    {
        Has(Check(new Contact { FullName = "A", Email = "test@test.com" }), "EMAIL_PLACEHOLDER").Should().BeTrue();
        Has(Check(new Contact { FullName = "A", Email = "x@mailinator.com" }), "EMAIL_DISPOSABLE").Should().BeTrue();
        var role = Check(new Contact { FullName = "A", Email = "info@acme.com" }).Single(x => x.Code == "EMAIL_ROLE");
        role.Severity.Should().Be(Severity.Info);
    }

    [Fact]
    public void Phone_checks_invalid_suspicious_and_standardisable()
    {
        Has(Check(new Contact { FullName = "A", Email = "a@x.com", Phone = "12345" }), "PHONE_INVALID").Should().BeTrue();
        Has(Check(new Contact { FullName = "A", Email = "a@x.com", Phone = "9999999999" }), "PHONE_SUSPICIOUS").Should().BeTrue();
        Check(new Contact { FullName = "A", Email = "a@x.com", Phone = "9812345670" }).Single(x => x.Code == "PHONE_FORMAT").Suggestion.Should().Be("+91 98123 45670");
    }

    [Fact]
    public void Name_checks()
    {
        Has(Check(new Contact { FullName = "john@x.com", Email = "a@x.com" }), "NAME_IS_EMAIL").Should().BeTrue();
        Has(Check(new Contact { FullName = "John2 Smith", Email = "a@x.com" }), "NAME_DIGITS").Should().BeTrue();
        Has(Check(new Contact { FullName = "test", Email = "a@x.com" }), "NAME_PLACEHOLDER").Should().BeTrue();
        Check(new Contact { FullName = "RAVI KUMAR", Email = "a@x.com" }).Single(x => x.Code == "NAME_CASE").Suggestion.Should().Be("Ravi Kumar");
    }

    [Fact]
    public void Website_company_date_and_country_checks()
    {
        var c = new Contact { FullName = "A", Email = "a@x.com", Website = "not a site", CompanyName = "N/A", SourceCreatedDate = DateTime.UtcNow.AddYears(3), Country = "Indai" };
        var issues = Check(c);
        Has(issues, "WEBSITE_INVALID").Should().BeTrue();
        Has(issues, "COMPANY_PLACEHOLDER").Should().BeTrue();
        Has(issues, "DATE_IMPLAUSIBLE").Should().BeTrue();
        issues.Single(i => i.Code == "COUNTRY_UNKNOWN").Suggestion.Should().Be("India");
    }

    [Fact]
    public void Rules_can_be_switched_off()
    {
        var c = new Contact { FullName = "Ana Lee", Email = "bad", Phone = "1" };
        var off = new ValidationRules { CheckEmail = false, CheckPhone = false, Required = new() };
        ValidationEngine.ValidateContact(c, off).Should().BeEmpty();
    }

    [Fact]
    public void Dataset_validation_finds_shared_identifiers_and_summarises()
    {
        var list = new List<Contact>
        {
            Keyed(1, "A One", "shared@x.com", "9812345670"), Keyed(2, "B Two", "SHARED@x.com", "9812345671"),
            Keyed(3, "C Three", "c@x.com", "98123 45671"), Keyed(4, "D Four", "bad", null), Keyed(5, "E Five", "e@x.com", "9812345675"),
        };
        var s = ValidationEngine.ValidateAll(list, Rules());
        s.TotalContacts.Should().Be(5);
        s.Issues.Count(i => i.Code == "DUPLICATE_EMAIL").Should().Be(2);
        s.Issues.Count(i => i.Code == "DUPLICATE_PHONE").Should().Be(2);
        s.RecordsWithErrors.Should().Be(1);                      // contact 4: invalid email
        s.RecordsWithWarnings.Should().Be(3);       // contacts 1-3 share identifiers
        s.ValidRecords.Should().Be(1);              // contact 5 has only an informational suggestion
        s.ByCode.Should().ContainKey("EMAIL_INVALID");
    }

    private static Contact Keyed(int id, string name, string? email, string? phone)
    {
        var c = new Contact { Id = id, FullName = name, Email = email, Phone = phone };
        CRM_Master.Core.BusinessRules.ContactKeys.Apply(c, "91"); return c;
    }
}

public class CleaningTests : IAsyncLifetime
{
    private TestDb _db = null!; private CleaningService _svc = null!; private BatchUndoService _undo = null!;
    public async Task InitializeAsync()
    {
        _db = await TestDb.CreateAsync();
        _svc = new CleaningService(_db.Factory, _db.Settings, _db.Backups, _db.Contacts);
        _undo = new BatchUndoService(_db.Factory, _db.Settings);
    }
    public Task DisposeAsync() { _db.Dispose(); return Task.CompletedTask; }

    private static Contact Messy() => new() { FullName = "  RAVI   KUMAR ", Email = " Ravi@Acme.COM ", Phone = "98765 43210", Website = "ACME.com/", CompanyName = "  Acme   Ltd ", City = " pune " };

    [Fact]
    public void Analysis_proposes_each_fix_with_before_after_and_reason_without_writing()
    {
        var c = Messy(); c.Id = 1;
        var report = CleaningEngine.Analyze(new[] { c }, new CleaningOptions());
        var byField = report.Changes.ToDictionary(x => x.Field);
        byField[ContactField.FullName].After.Should().Be("Ravi Kumar");
        byField[ContactField.FullName].Reason.Should().Contain("extra spaces").And.Contain("capitalisation");
        byField[ContactField.Email].After.Should().Be("ravi@acme.com");
        byField[ContactField.Phone].After.Should().Be("+91 98765 43210");
        byField[ContactField.Website].After.Should().Be("https://acme.com");
        byField[ContactField.Company].After.Should().Be("Acme Ltd");
        byField[ContactField.City].After.Should().Be("pune");   // only whitespace fixed; city casing is not touched
        report.RecordsAffected.Should().Be(1);
        c.FullName.Should().Be("  RAVI   KUMAR ");   // original untouched
    }

    [Fact]
    public void Steps_can_be_disabled()
    {
        var c = Messy(); c.Id = 1;
        var report = CleaningEngine.Analyze(new[] { c }, new CleaningOptions { ProperCaseNames = false, NormalizeEmail = false, NormalizePhone = false, NormalizeWebsite = false });
        report.Changes.Select(x => x.Field).Should().BeEquivalentTo(new[] { ContactField.FullName, ContactField.Email, ContactField.Company, ContactField.City });
        report.Changes.Single(x => x.Field == ContactField.FullName).After.Should().Be("RAVI KUMAR");
    }

    [Fact]
    public void Issues_identify_empty_malformed_and_suspicious_values()
    {
        var list = new List<Contact>
        {
            new() { Id = 1, FullName = "A", Email = "nope", Phone = "123" },
            new() { Id = 2, FullName = "B", Email = "test@test.com", Phone = "9999999999", SourceCreatedDate = DateTime.UtcNow.AddYears(2) },
            new() { Id = 3, FirstName = "J0hn" },
        };
        var r = CleaningEngine.Analyze(list, new CleaningOptions());
        r.Issues.Should().Contain(i => i.ContactId == 1 && i.Kind == CleaningIssueKind.MalformedEmail);
        r.Issues.Should().Contain(i => i.ContactId == 1 && i.Kind == CleaningIssueKind.MalformedPhone);
        r.Issues.Should().Contain(i => i.ContactId == 2 && i.Kind == CleaningIssueKind.SuspiciousValue && i.Field == ContactField.Email);
        r.Issues.Should().Contain(i => i.ContactId == 2 && i.Kind == CleaningIssueKind.SuspiciousValue && i.Field == ContactField.Phone);
        r.Issues.Should().Contain(i => i.ContactId == 2 && i.Kind == CleaningIssueKind.InvalidDate);
        r.Issues.Should().Contain(i => i.ContactId == 3 && i.Kind == CleaningIssueKind.EmptyField && i.Field == ContactField.Email);
        r.Issues.Should().Contain(i => i.ContactId == 3 && i.Detail.Contains("digits"));
        r.ErrorCount.Should().Be(3);   // bad email, bad phone, impossible date (placeholders count as suspicious, not errors)
    }

    [Fact]
    public void Exact_duplicates_are_found_after_cleaning_and_lowest_id_is_kept()
    {
        var list = new List<Contact>
        {
            new() { Id = 5, FullName = "ravi kumar", Email = "r@x.com" }, new() { Id = 3, FullName = "RAVI KUMAR ", Email = "R@X.com" },
            new() { Id = 9, FullName = "Ravi Kumar", Email = "other@x.com" },
        };
        var r = CleaningEngine.Analyze(list, new CleaningOptions { RemoveExactDuplicates = true });
        r.ExactDuplicateGroups.Should().ContainSingle().Which.Should().Equal(3, 5);
        r.RemovableDuplicates.Should().Be(1);
    }

    [Fact]
    public async Task Apply_updates_records_backs_up_first_and_logs_previous_values()
    {
        var a = await _db.Contacts.AddAsync(Messy());
        var report = await _svc.AnalyzeAsync(_svc.DefaultOptions());
        var res = await _svc.ApplyAsync(report, _svc.DefaultOptions(), removeDuplicates: false);

        res.ContactsUpdated.Should().Be(1);
        File.Exists(res.BackupPath!).Should().BeTrue();
        var c = (await _db.Contacts.GetAsync(a.Id))!;
        (c.FullName, c.Email, c.Phone, c.Website).Should().Be(("Ravi Kumar", "ravi@acme.com", "+91 98765 43210", "https://acme.com"));
        c.PhoneKey.Should().Be("919876543210");
        var log = (await _db.Audit.QueryAsync(new(Operation: AuditOperation.DataCleaning, RecordId: a.Id))).Items.Single();
        log.PreviousValue.Should().Contain("  RAVI   KUMAR ").And.Contain("98765 43210");
    }

    [Fact]
    public async Task Apply_never_overwrites_a_value_edited_after_the_analysis()
    {
        var a = await _db.Contacts.AddAsync(new Contact { FullName = "RAVI KUMAR", Email = "r@x.com" });
        var report = await _svc.AnalyzeAsync(_svc.DefaultOptions());
        var edit = (await _db.Contacts.GetAsync(a.Id))!; edit.FullName = "Ravi K."; await _db.Contacts.UpdateAsync(edit);
        var res = await _svc.ApplyAsync(report, _svc.DefaultOptions(), false);
        res.SkippedBecauseChanged.Should().Be(1);
        (await _db.Contacts.GetAsync(a.Id))!.FullName.Should().Be("Ravi K.");
    }

    [Fact]
    public async Task Removing_exact_duplicates_logs_full_records_and_undo_restores_everything()
    {
        await _db.Contacts.AddAsync(new Contact { FullName = "ravi kumar", Email = "r@x.com", City = "Pune" });
        await _db.Contacts.AddAsync(new Contact { FullName = "Ravi Kumar", Email = "R@x.com", City = "Pune" });
        var opts = _svc.DefaultOptions(); opts.RemoveExactDuplicates = true;
        var report = await _svc.AnalyzeAsync(opts);
        report.RemovableDuplicates.Should().Be(1);

        var res = await _svc.ApplyAsync(report, opts, removeDuplicates: true);
        res.DuplicatesRemoved.Should().Be(1);
        (await _db.Contacts.CountAsync()).Should().Be(1);
        (await _db.Audit.QueryAsync(new(BatchId: res.BatchId))).Items.Should().Contain(a => a.Summary!.StartsWith("Removed exact duplicate") && a.PreviousValue!.Contains("r@x.com"));

        (await _undo.CanUndoAsync(res.BatchId)).Should().BeTrue();
        var undo = await _undo.UndoAsync(res.BatchId);
        undo.Restored.Should().Be(1);
        (await _db.Contacts.CountAsync()).Should().Be(2);
        (await _undo.CanUndoAsync(res.BatchId)).Should().BeFalse();
        await ((Func<Task>)(() => _undo.UndoAsync(res.BatchId))).Should().ThrowAsync<InvalidOperationException>().WithMessage("*already*");
    }

    [Fact]
    public async Task Undo_reverts_field_changes_but_leaves_later_edits_alone()
    {
        var a = await _db.Contacts.AddAsync(new Contact { FullName = "RAVI KUMAR", Email = " R@X.com ", City = "pune" });
        var b = await _db.Contacts.AddAsync(new Contact { FullName = "ASHA RAO", Email = "a@x.com" });
        var report = await _svc.AnalyzeAsync(_svc.DefaultOptions());
        var res = await _svc.ApplyAsync(report, _svc.DefaultOptions(), false);
        res.ContactsUpdated.Should().Be(2);

        var later = (await _db.Contacts.GetAsync(b.Id))!; later.FullName = "Asha R. Rao"; await _db.Contacts.UpdateAsync(later);
        var undo = await _undo.UndoAsync(res.BatchId);
        undo.Reverted.Should().Be(1); undo.Conflicts.Should().Be(1);
        (await _db.Contacts.GetAsync(a.Id))!.FullName.Should().Be("RAVI KUMAR");
        (await _db.Contacts.GetAsync(a.Id))!.Email.Should().Be(" R@X.com ");
        (await _db.Contacts.GetAsync(b.Id))!.FullName.Should().Be("Asha R. Rao");   // not clobbered
    }
}
