using CRM_Master.Core.Entities;
using CRM_Master.Engine.Automation;
using CRM_Master.Tests.Support;

namespace CRM_Master.Tests.Engine;

public class RuleEvaluatorTests
{
    private static RuleCondition Cond(string f, ConditionOperator op, string? v = null) => new(f, op, v);
    private static bool Test(Contact c, RuleCondition cond) => RuleEvaluator.Test(cond, c, "91");

    [Fact]
    public void Condition_operators()
    {
        var c = new Contact { FullName = "Ravi Kumar", Email = "ravi@acme.com", Phone = "123", City = "Pune", Website = "acme.com" };
        Test(c, Cond("Company", ConditionOperator.IsEmpty)).Should().BeTrue();
        Test(c, Cond("Email", ConditionOperator.IsNotEmpty)).Should().BeTrue();
        Test(c, Cond("City", ConditionOperator.Equals, "pune")).Should().BeTrue();
        Test(c, Cond("City", ConditionOperator.NotEquals, "Delhi")).Should().BeTrue();
        Test(c, Cond("Email", ConditionOperator.Contains, "ACME")).Should().BeTrue();
        Test(c, Cond("Email", ConditionOperator.StartsWith, "ravi")).Should().BeTrue();
        Test(c, Cond("Email", ConditionOperator.EndsWith, ".com")).Should().BeTrue();
        Test(c, Cond("Email", ConditionOperator.MatchesRegex, @"^[a-z]+@acme\.com$")).Should().BeTrue();
        Test(c, Cond("Phone", ConditionOperator.IsInvalid)).Should().BeTrue();
        Test(c, Cond("Email", ConditionOperator.IsValid)).Should().BeTrue();
        Test(c, Cond("Website", ConditionOperator.IsValid)).Should().BeTrue();
        Test(c, Cond("Nonsense", ConditionOperator.IsEmpty)).Should().BeFalse();
        Test(c, Cond("Email", ConditionOperator.MatchesRegex, "([")).Should().BeFalse();      // bad regex never throws
    }

    [Fact]
    public void All_vs_any_matching_and_empty_condition_list()
    {
        var c = new Contact { FullName = "A", Email = "a@x.com" };
        var all = new RuleDefinition { Match = ConditionMatch.All, Conditions = { Cond("Email", ConditionOperator.IsNotEmpty), Cond("Phone", ConditionOperator.IsNotEmpty) } };
        var any = new RuleDefinition { Match = ConditionMatch.Any, Conditions = all.Conditions };
        RuleEvaluator.Matches(all, c, "91").Should().BeFalse();
        RuleEvaluator.Matches(any, c, "91").Should().BeTrue();
        RuleEvaluator.Matches(new RuleDefinition(), c, "91").Should().BeTrue();
    }

    [Fact]
    public void Actions_modify_the_contact_and_report_whether_anything_changed()
    {
        var c = new Contact { FullName = "RAVI KUMAR", Email = " R@X.COM ", Phone = "9812345670", Website = "ACME.com" };
        RuleEvaluator.Apply(new(ActionType.NormalizePhone), c, "91").Should().BeTrue();
        c.Phone.Should().Be("+91 98123 45670");
        RuleEvaluator.Apply(new(ActionType.NormalizePhone), c, "91").Should().BeFalse();        // already normalised
        RuleEvaluator.Apply(new(ActionType.NormalizeEmail), c, "91"); c.Email.Should().Be("r@x.com");
        RuleEvaluator.Apply(new(ActionType.ProperCaseNames), c, "91"); c.FullName.Should().Be("Ravi Kumar");
        RuleEvaluator.Apply(new(ActionType.NormalizeWebsite), c, "91"); c.Website.Should().Be("https://acme.com");
        RuleEvaluator.Apply(new(ActionType.FlagForReview), c, "91"); c.NeedsReview.Should().BeTrue();
        RuleEvaluator.Apply(new(ActionType.MarkIncomplete), c, "91"); c.IsIncomplete.Should().BeTrue();
        RuleEvaluator.Apply(new(ActionType.FlagDuplicate), c, "91"); c.IsDuplicateFlagged.Should().BeTrue();
        RuleEvaluator.Apply(new(ActionType.SetStatus, Value: "Customer"), c, "91"); c.Status.Should().Be("Customer");
        RuleEvaluator.Apply(new(ActionType.SetField, "City", "Pune"), c, "91"); c.City.Should().Be("Pune");
    }

    [Fact]
    public void Run_executes_only_rules_for_the_trigger_and_skips_broken_rules()
    {
        var def = new RuleDefinition { Actions = { new(ActionType.FlagForReview) } };
        var rules = new[]
        {
            new ParsedRule(1, "A", AutomationTrigger.ContactImported, def),
            new ParsedRule(2, "B", AutomationTrigger.ContactUpdated, def),
            new ParsedRule(3, "Broken", AutomationTrigger.ContactImported, def, "bad json"),
        };
        var c = new Contact();
        var res = RuleEvaluator.Run(rules, AutomationTrigger.ContactImported, c, "91");
        res.Fired.Select(f => f.Rule.Name).Should().Equal("A");
        c.NeedsReview.Should().BeTrue();
    }

    [Fact]
    public void Definition_round_trips_through_json()
    {
        var d = new RuleDefinition { Match = ConditionMatch.Any, Conditions = { Cond("Email", ConditionOperator.IsEmpty) }, Actions = { new(ActionType.SetField, "City", "X"), new(ActionType.MarkIncomplete) } };
        var back = RuleDefinition.Parse(RuleDefinition.SerializeConditions(d), RuleDefinition.SerializeActions(d));
        back.Match.Should().Be(ConditionMatch.Any);
        back.Conditions.Should().Equal(d.Conditions);
        back.Actions.Should().Equal(d.Actions);
    }

    [Fact]
    public void Validation_catches_incomplete_rules()
    {
        AutomationService.Validate("", AutomationTrigger.Manual, new()).Should().HaveCount(2);                                    // name + no action
        AutomationService.Validate("x", AutomationTrigger.ContactImported, new() { Actions = { new(ActionType.ProposeUpdate) } }).Should().ContainSingle().Which.Should().Contain("Existing contact matched");
        AutomationService.Validate("x", AutomationTrigger.Manual, new() { Conditions = { Cond("Email", ConditionOperator.Contains) }, Actions = { new(ActionType.FlagForReview) } }).Should().ContainSingle().Which.Should().Contain("needs a value");
        AutomationService.Validate("ok", AutomationTrigger.Manual, new() { Actions = { new(ActionType.FlagForReview) } }).Should().BeEmpty();
    }
}

public class AutomationServiceTests : IAsyncLifetime
{
    private TestDb _db = null!; private AutomationService _svc = null!;
    public async Task InitializeAsync() { _db = await TestDb.CreateAsync(); _svc = new AutomationService(_db.Factory, _db.Settings, _db.Backups); }
    public Task DisposeAsync() { _db.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Starter_rules_match_the_six_examples_and_seed_only_once()
    {
        await _svc.SeedDefaultsAsync(); await _svc.SeedDefaultsAsync();
        var rules = await _svc.ListAsync();
        rules.Should().HaveCount(6);
        rules.Where(r => r.IsEnabled).Select(r => r.Name).Should().BeEquivalentTo("Normalize phone on import", "Flag duplicate emails", "Missing email = incomplete", "Missing company = review");
        rules.Select(r => r.Trigger).Should().Contain(new[] { AutomationTrigger.ContactImported, AutomationTrigger.DuplicateDetected, AutomationTrigger.ExistingContactMatched, AutomationTrigger.ImportCompleted });
    }

    [Fact]
    public async Task Create_edit_disable_delete_rule()
    {
        var def = new RuleDefinition { Conditions = { new("City", ConditionOperator.Equals, "Pune") }, Actions = { new(ActionType.SetStatus, Value: "Local") } };
        var r = await _svc.SaveAsync(null, "Pune leads", "tag local", AutomationTrigger.ContactCreated, def, true);
        (await _svc.LoadEnabledAsync(default, AutomationTrigger.ContactCreated)).Should().ContainSingle();
        await _svc.SaveAsync(r.Id, "Pune leads v2", null, AutomationTrigger.ContactCreated, def, true);
        (await _svc.ListAsync()).Single().Name.Should().Be("Pune leads v2");
        await _svc.SetEnabledAsync(r.Id, false);
        (await _svc.LoadEnabledAsync(default, AutomationTrigger.ContactCreated)).Should().BeEmpty();
        await _svc.DeleteAsync(r.Id);
        (await _svc.ListAsync()).Should().BeEmpty();
        await ((Func<Task>)(() => _svc.SaveAsync(null, "", null, AutomationTrigger.Manual, new(), true))).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Master_switch_in_settings_disables_all_rules()
    {
        await _svc.SeedDefaultsAsync();
        var s = _db.Settings.Current.Clone(); s.AutomationEnabled = false; await _db.Settings.SaveAsync(s);
        (await _svc.LoadEnabledAsync(default, AutomationTrigger.ContactImported)).Should().BeEmpty();
    }

    [Fact]
    public async Task Rule_runs_on_user_created_contact_and_is_recorded_in_history()
    {
        var def = new RuleDefinition { Conditions = { new("Phone", ConditionOperator.IsNotEmpty) }, Actions = { new(ActionType.NormalizePhone) } };
        var rule = await _svc.SaveAsync(null, "Normalize on create", null, AutomationTrigger.ContactCreated, def, true);
        var c = await _db.Contacts.AddAsync(new Contact { FullName = "Ravi", Phone = "9812345670" });
        await _svc.ApplyToSavedContactAsync(c.Id, AutomationTrigger.ContactCreated, _db.Contacts);

        (await _db.Contacts.GetAsync(c.Id))!.Phone.Should().Be("+91 98123 45670");
        var hist = (await _svc.HistoryAsync(rule.Id)).Single();
        (hist.ContactId, hist.AffectedCount, hist.Status).Should().Be((c.Id, 1, OperationStatus.Success));
        (await _svc.ListAsync()).Single().RunCount.Should().Be(1);
        (await _db.Audit.QueryAsync(new(Operation: AuditOperation.AutomationExecution))).TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Manual_run_applies_rule_to_all_contacts_with_backup_audit_and_undo()
    {
        for (var i = 0; i < 30; i++) await _db.Contacts.AddAsync(new Contact { FullName = $"P{i}", Email = $"p{i}@x.com", City = i % 3 == 0 ? "Pune" : "Delhi" });
        var def = new RuleDefinition { Conditions = { new("City", ConditionOperator.Equals, "Pune") }, Actions = { new(ActionType.SetStatus, Value: "Local"), new(ActionType.FlagForReview) } };
        var rule = await _svc.SaveAsync(null, "Tag Pune", null, AutomationTrigger.Manual, def, true);

        var (matched, changed) = await _svc.RunNowAsync(rule.Id);
        (matched, changed).Should().Be((10, 10));
        (await _db.Contacts.QueryAsync(new(Status: "Local"))).TotalCount.Should().Be(10);
        (await _db.Contacts.QueryAsync(new(Quality: CRM_Master.Core.DTOs.QualityFilter.NeedsReview))).TotalCount.Should().Be(10);
        (await _db.Backups.ListAsync()).Should().ContainSingle(b => b.Kind == BackupKind.PreOperation);

        var again = await _svc.RunNowAsync(rule.Id);
        again.Changed.Should().Be(0);                                      // idempotent
        (await _svc.HistoryAsync(rule.Id)).Should().HaveCount(2);

        var batch = (await _db.Audit.QueryAsync(new(Operation: AuditOperation.AutomationExecution, EntityType: "Contact"))).Items.First().BatchId!;
        (await new CRM_Master.Engine.BatchUndoService(_db.Factory, _db.Settings).UndoAsync(batch)).Reverted.Should().Be(10);
        (await _db.Contacts.QueryAsync(new(Status: "Local"))).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Corrupt_rule_json_is_reported_not_thrown()
    {
        await using (var ctx = _db.Factory.CreateDbContext())
        { ctx.AutomationRules.Add(new AutomationRule { Name = "Bad", Trigger = AutomationTrigger.ContactImported, ConditionsJson = "{not json", ActionsJson = "[]" }); await ctx.SaveChangesAsync(); }
        var rules = await _svc.LoadEnabledAsync(default, AutomationTrigger.ContactImported);
        rules.Single().Error.Should().Contain("not valid");
        await ((Func<Task>)(() => _svc.RunNowAsync(rules[0].Id))).Should().ThrowAsync<InvalidOperationException>();
    }
}
