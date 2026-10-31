using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Engine;
using CRM_Master.Engine.BulkUpdate;
using CRM_Master.Engine.Import;
using CRM_Master.Tests.Support;

namespace CRM_Master.Tests.Engine;

public class BulkUpdatePlannerTests
{
    private static Contact Existing(int id, string? email, string? phone = null, string? city = null, string? company = null, string? name = "Ravi Kumar")
    { var c = new Contact { Id = id, FullName = name, Email = email, Phone = phone, City = city, CompanyName = company }; ContactKeys.Apply(c, "91"); return c; }
    private static IncomingContact In(int row, string? email, string? phone = null, string? city = null, string? company = null, string? name = null, int? pin = null)
        => new(row, new Contact { FullName = name, Email = email, Phone = phone, City = city, CompanyName = company }, pin);

    private static readonly ContactField[] Sel = { ContactField.City, ContactField.Phone, ContactField.Company };
    private static BulkUpdatePlan Plan(BulkUpdatePolicy p, Contact[] ex, params IncomingContact[] inc) => BulkUpdatePlanner.Plan(ex, inc, p, Sel);
    private static FieldProposal F(BulkUpdatePlan plan, ContactField f, int rec = 0) => plan.Records[rec].Fields.Single(x => x.Field == f);

    [Fact]
    public void Matches_by_email_then_phone_and_reports_unmatched()
    {
        var ex = new[] { Existing(1, "a@x.com", "9812345670"), Existing(2, "b@x.com", "9812345671") };
        var plan = Plan(BulkUpdatePolicy.AllSelectedFields, ex, In(2, "A@X.com", city: "Pune"), In(3, "other@x.com", "09812345671", city: "Delhi"), In(4, "nobody@x.com", city: "X"));
        plan.Matched.Should().Be(2);
        plan.Records.Select(r => (r.ContactId, r.MatchedBy)).Should().Equal((1, "Email"), (2, "Phone"));
        plan.Unmatched.Should().ContainSingle().Which.RowNumber.Should().Be(4);
    }

    [Fact]
    public void Empty_fields_only_fills_blanks_and_keeps_existing_values()
    {
        var plan = Plan(BulkUpdatePolicy.EmptyFieldsOnly, new[] { Existing(1, "a@x.com", city: "Pune") }, In(2, "a@x.com", phone: "9812345670", city: "Mumbai"));
        F(plan, ContactField.Phone).Kind.Should().Be(ProposalKind.FillEmpty);
        F(plan, ContactField.City).Kind.Should().Be(ProposalKind.KeptExisting);
        plan.ProposedFieldChanges.Should().Be(1);
    }

    [Fact]
    public void All_selected_fields_overwrites_only_selected_and_only_with_non_blank_values()
    {
        var plan = Plan(BulkUpdatePolicy.AllSelectedFields, new[] { Existing(1, "a@x.com", city: "Pune", company: "Old Co") }, In(2, "a@x.com", city: "Mumbai", company: null));
        F(plan, ContactField.City).Kind.Should().Be(ProposalKind.Overwrite);
        F(plan, ContactField.Company).Kind.Should().Be(ProposalKind.MissingIncoming);
        plan.Records[0].Fields.Select(f => f.Field).Should().BeEquivalentTo(Sel);     // unselected fields are not even considered
        plan.Records[0].Fields.Should().OnlyContain(f => !f.IsActionable || f.Approved);
    }

    [Fact]
    public void Identical_values_after_normalisation_are_not_changes()
    {
        var plan = Plan(BulkUpdatePolicy.AllSelectedFields, new[] { Existing(1, "a@x.com", "+91 98123 45670", "Pune", "Acme Pvt Ltd") }, In(2, "a@x.com", "09812345670", "PUNE", "ACME Limited"));
        plan.Records[0].Fields.Should().OnlyContain(f => f.Kind == ProposalKind.Same);
        plan.WithChanges.Should().Be(0); plan.Unchanged.Should().Be(1);
    }

    [Fact]
    public void Preserve_existing_considers_all_fields_but_never_changes_a_value()
    {
        var plan = BulkUpdatePlanner.Plan(new[] { Existing(1, "a@x.com", city: "Pune") }, new[] { In(2, "a@x.com", city: "Mumbai", phone: "9812345670", name: "Different Name") },
            BulkUpdatePolicy.PreserveExisting, new[] { ContactField.City });
        plan.Records[0].Fields.Count.Should().Be(Contact.Fields.Count);
        plan.Records[0].Actionable.Should().OnlyContain(f => f.Kind == ProposalKind.FillEmpty);
        F(plan, ContactField.City).Kind.Should().Be(ProposalKind.KeptExisting);
        F(plan, ContactField.FullName).Kind.Should().Be(ProposalKind.KeptExisting);
    }

    [Fact]
    public void Only_if_valid_rejects_invalid_incoming_values()
    {
        var ex = new[] { Existing(1, "a@x.com", "9812345670") };
        var bulk = BulkUpdatePlanner.Plan(ex, new[] { In(2, "a@x.com", phone: "123") }, BulkUpdatePolicy.OnlyIfValid, new[] { ContactField.Phone });
        F(bulk, ContactField.Phone).Kind.Should().Be(ProposalKind.RejectedInvalid);
        var ok = BulkUpdatePlanner.Plan(ex, new[] { In(2, "a@x.com", phone: "9812345699") }, BulkUpdatePolicy.OnlyIfValid, new[] { ContactField.Phone });
        F(ok, ContactField.Phone).Kind.Should().Be(ProposalKind.Overwrite);
        var blankFill = BulkUpdatePlanner.Plan(new[] { Existing(1, "a@x.com") }, new[] { In(2, "a@x.com", phone: "bad") }, BulkUpdatePolicy.OnlyIfValid, new[] { ContactField.Phone });
        F(blankFill, ContactField.Phone).Kind.Should().Be(ProposalKind.RejectedInvalid);   // invalid values are not even used to fill blanks
    }

    [Fact]
    public void Skip_conflicts_skips_the_whole_record_but_not_clean_ones()
    {
        var ex = new[] { Existing(1, "a@x.com", city: "Pune"), Existing(2, "b@x.com") };
        var plan = Plan(BulkUpdatePolicy.SkipConflicts, ex, In(2, "a@x.com", city: "Mumbai", phone: "9812345670"), In(3, "b@x.com", city: "Delhi"));
        plan.Records[0].Skipped.Should().BeTrue();
        plan.Records[0].SkipReason.Should().Contain("City");
        plan.Records[0].Actionable.Should().BeEmpty();                  // even the phone fill is held back
        plan.Records[1].Skipped.Should().BeFalse();
        plan.Records[1].HasChanges.Should().BeTrue();
    }

    [Fact]
    public void Manual_approval_proposes_everything_but_approves_nothing()
    {
        var plan = Plan(BulkUpdatePolicy.ManualApproval, new[] { Existing(1, "a@x.com", city: "Pune") }, In(2, "a@x.com", city: "Mumbai", phone: "9812345670"));
        plan.ProposedFieldChanges.Should().Be(2);
        plan.ApprovedFieldChanges.Should().Be(0);
    }

    [Fact]
    public void A_pinned_match_from_import_overrides_key_lookup_and_first_row_wins_for_duplicates()
    {
        var ex = new[] { Existing(1, "a@x.com"), Existing(2, "b@x.com") };
        var plan = Plan(BulkUpdatePolicy.AllSelectedFields, ex, In(2, "zzz@x.com", city: "Pinned", pin: 2), In(3, "b@x.com", city: "Later row"));
        plan.Records.Should().ContainSingle();
        F(plan, ContactField.City).Incoming.Should().Be("Pinned");
    }
}

public class BulkUpdateServiceTests : IAsyncLifetime
{
    private TestDb _db = null!; private BulkUpdateService _svc = null!; private ImportService _import = null!;
    public async Task InitializeAsync()
    {
        _db = await TestDb.CreateAsync();
        _svc = new BulkUpdateService(_db.Factory, _db.Settings, _db.Backups, _db.Contacts);
        _import = new ImportService(_db.Factory, _db.Settings, _db.Backups);
    }
    public Task DisposeAsync() { _db.Dispose(); return Task.CompletedTask; }

    private async Task SeedThousand()
    {
        var rows = new List<string[]> { new[] { "Name", "Email", "Phone", "City", "Company" } };
        for (var i = 0; i < 1000; i++) rows.Add(new[] { $"Person {i}", $"p{i}@x.com", (9100000000L + i).ToString(), i % 2 == 0 ? "Pune" : "", i % 4 == 0 ? "" : $"Co{i}" });
        var path = Files.Csv(_db.Folder, "base.csv", rows);
        var t = await TableReader.ReadAsync(path, 20);
        (await _import.ImportAsync(path, _import.DefaultOptions(ColumnMapper.Suggest(t)))).Imported.Should().Be(1000);
    }

    private async Task<(string path, List<ColumnMapping> map)> Incoming200()
    {
        // 150 existing contacts (ids 0..149) with changed city/company, 50 brand-new people
        var rows = new List<string[]> { new[] { "Email", "City", "Company" } };
        for (var i = 0; i < 150; i++) rows.Add(new[] { $"P{i}@X.com", "Mumbai", $"NewCo{i}" });
        for (var i = 0; i < 50; i++) rows.Add(new[] { $"stranger{i}@x.com", "Delhi", "Z" });
        var path = Files.Csv(_db.Folder, "update.csv", rows);
        var map = ColumnMapper.Suggest(await TableReader.ReadAsync(path, 20));
        return (path, map);
    }

    [Fact]
    public async Task Thousand_existing_two_hundred_incoming_end_to_end()
    {
        await SeedThousand();
        var (path, map) = await Incoming200();
        var fields = new[] { ContactField.City, ContactField.Company };

        var plan = await _svc.PlanFromFileAsync(path, map, BulkUpdatePolicy.AllSelectedFields, fields);
        (plan.IncomingRows, plan.Matched, plan.Unmatched.Count).Should().Be((200, 150, 50));
        plan.WithChanges.Should().Be(150);
        // person i: city is "Pune" for even i (conflict -> overwrite), blank for odd (fill); company blank for i%4==0 (fill) else Co{i} (overwrite)
        F(plan, 0, ContactField.City).Kind.Should().Be(ProposalKind.Overwrite);
        F(plan, 1, ContactField.City).Kind.Should().Be(ProposalKind.FillEmpty);
        F(plan, 0, ContactField.Company).Kind.Should().Be(ProposalKind.FillEmpty);
        F(plan, 1, ContactField.Company).Kind.Should().Be(ProposalKind.Overwrite);

        // field-level approval: reject all company overwrites
        foreach (var f in plan.Records.SelectMany(r => r.Fields).Where(f => f.Field == ContactField.Company && f.Kind == ProposalKind.Overwrite)) f.Approved = false;
        var approved = plan.ApprovedFieldChanges;

        var res = await _svc.ApplyAsync(plan);
        res.FieldsChanged.Should().Be(approved);
        res.ContactsUpdated.Should().Be(150);
        File.Exists(res.BackupPath!).Should().BeTrue();

        var p0 = (await _db.Contacts.QueryAsync(new(Search: "p0@x.com"))).Items.Single();
        (p0.City, p0.CompanyName).Should().Be(("Mumbai", "NewCo0"));        // filled blank company, overwrote city
        var p1 = (await _db.Contacts.QueryAsync(new(Search: "p1@x.com"))).Items.Single();
        (p1.City, p1.CompanyName).Should().Be(("Mumbai", "Co1"));            // company overwrite was rejected
        (await _db.Contacts.QueryAsync(new(Search: "p500@x.com"))).Items.Single().City.Should().Be("Pune");   // untouched

        // previous and new values are recorded for every change
        var audit = (await _db.Audit.QueryAsync(new(Operation: AuditOperation.BulkUpdate, RecordId: p0.Id))).Items.Single();
        audit.PreviousValue.Should().Contain("\"City\":\"Pune\"").And.Contain("\"Company\":null");
        audit.NewValue.Should().Contain("\"City\":\"Mumbai\"").And.Contain("NewCo0");
        (await _db.Audit.QueryAsync(new(BatchId: res.BatchId))).TotalCount.Should().Be(151);   // 150 contacts + batch summary

        // the whole batch can be undone
        var undo = await new BatchUndoService(_db.Factory, _db.Settings).UndoAsync(res.BatchId);
        undo.Reverted.Should().Be(150);
        (await _db.Contacts.QueryAsync(new(Search: "p0@x.com"))).Items.Single().City.Should().Be("Pune");
    }

    private static FieldProposal F(BulkUpdatePlan p, int idx, ContactField f) => p.Records[idx].Fields.Single(x => x.Field == f);

    [Fact]
    public async Task Nothing_is_written_until_apply_and_unapproved_plans_change_nothing()
    {
        await SeedThousand();
        var (path, map) = await Incoming200();
        var plan = await _svc.PlanFromFileAsync(path, map, BulkUpdatePolicy.ManualApproval, new[] { ContactField.City });
        (await _db.Audit.QueryAsync(new(Operation: AuditOperation.BulkUpdate))).TotalCount.Should().Be(0);
        var res = await _svc.ApplyAsync(plan);          // nothing approved
        (res.ContactsUpdated, res.BackupPath).Should().Be((0, null));
        (await _db.Contacts.QueryAsync(new(Search: "Mumbai"))).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Concurrent_edit_after_planning_is_not_overwritten()
    {
        var c = await _db.Contacts.AddAsync(new Contact { FullName = "Ravi", Email = "r@x.com", City = "Pune" });
        var path = Files.Csv(_db.Folder, "u.csv", new[] { new[] { "Email", "City" }, new[] { "r@x.com", "Mumbai" } });
        var plan = await _svc.PlanFromFileAsync(path, ColumnMapper.Suggest(await TableReader.ReadAsync(path)), BulkUpdatePolicy.AllSelectedFields, new[] { ContactField.City });
        var edit = (await _db.Contacts.GetAsync(c.Id))!; edit.City = "Chennai"; await _db.Contacts.UpdateAsync(edit);
        var res = await _svc.ApplyAsync(plan);
        res.SkippedBecauseChanged.Should().Be(1);
        (await _db.Contacts.GetAsync(c.Id))!.City.Should().Be("Chennai");
    }

    [Fact]
    public async Task Conflicting_updates_with_skip_policy_change_nothing()
    {
        await _db.Contacts.AddAsync(new Contact { FullName = "Ravi", Email = "r@x.com", City = "Pune" });
        var path = Files.Csv(_db.Folder, "u.csv", new[] { new[] { "Email", "City" }, new[] { "r@x.com", "Mumbai" } });
        var plan = await _svc.PlanFromFileAsync(path, ColumnMapper.Suggest(await TableReader.ReadAsync(path)), BulkUpdatePolicy.SkipConflicts, new[] { ContactField.City });
        plan.Skipped.Should().Be(1);
        (await _svc.ApplyAsync(plan)).ContactsUpdated.Should().Be(0);
    }

    [Fact]
    public async Task Missing_identifying_column_is_rejected_with_a_clear_message()
    {
        var path = Files.Csv(_db.Folder, "u.csv", new[] { new[] { "City" }, new[] { "Mumbai" } });
        var act = () => _svc.PlanFromFileAsync(path, ColumnMapper.Suggest(TableReader.Read(path)), BulkUpdatePolicy.AllSelectedFields, new[] { ContactField.City });
        await act.Should().ThrowAsync<ImportException>().WithMessage("*Name, Email or Phone*");
    }

    [Fact]
    public async Task Plans_can_be_built_from_proposals_stored_by_an_import()
    {
        var existing = await _db.Contacts.AddAsync(new Contact { FullName = "Old Name", Email = "dup@x.com", City = "Pune" });
        var path = Files.Csv(_db.Folder, "p.csv", new[] { new[] { "Name", "Email", "City" }, new[] { "New Name", "dup@x.com", "Mumbai" } });
        var imp = await _import.ImportAsync(path, _import.DefaultOptions(ColumnMapper.Suggest(await TableReader.ReadAsync(path))) with { DuplicatePolicy = ImportDuplicatePolicy.ProposeUpdate });
        var plan = await _svc.PlanFromImportProposalsAsync(imp.JobId, BulkUpdatePolicy.AllSelectedFields, new[] { ContactField.City, ContactField.FullName });
        plan.Matched.Should().Be(1);
        F(plan, 0, ContactField.City).Incoming.Should().Be("Mumbai");
        (await _svc.ApplyAsync(plan)).ContactsUpdated.Should().Be(1);
        (await _db.Contacts.GetAsync(existing.Id))!.FullName.Should().Be("New Name");
    }
}
