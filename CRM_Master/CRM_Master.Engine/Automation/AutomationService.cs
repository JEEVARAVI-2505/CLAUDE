using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Engine.Automation;

/// <summary>Persistence, execution history and manual runs for automation rules.</summary>
public sealed class AutomationService
{
    private readonly IDbContextFactory<CrmDbContext> _factory;
    private readonly ISettingsService _settings;
    private readonly IBackupService? _backup;

    public AutomationService(IDbContextFactory<CrmDbContext> factory, ISettingsService settings, IBackupService? backup = null)
    { _factory = factory; _settings = settings; _backup = backup; }

    public bool Enabled => _settings.Current.AutomationEnabled;

    public async Task<List<AutomationRule>> ListAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.AutomationRules.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
    }

    /// <summary>Enabled rules for the given triggers (empty when automation is switched off in Settings).</summary>
    public async Task<List<ParsedRule>> LoadEnabledAsync(CancellationToken ct = default, params AutomationTrigger[] triggers)
    {
        if (!Enabled) return new();
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.AutomationRules.AsNoTracking().Where(r => r.IsEnabled);
        if (triggers.Length > 0) q = q.Where(r => triggers.Contains(r.Trigger));
        return (await q.OrderBy(r => r.Id).ToListAsync(ct)).Select(Parse).ToList();
    }

    public static ParsedRule Parse(AutomationRule r)
    {
        try { return new ParsedRule(r.Id, r.Name, r.Trigger, RuleDefinition.Parse(r.ConditionsJson, r.ActionsJson)); }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or ArgumentException)
        { return new ParsedRule(r.Id, r.Name, r.Trigger, new RuleDefinition(), "Rule definition is not valid: " + ex.Message); }
    }

    public static List<string> Validate(string name, AutomationTrigger trigger, RuleDefinition def)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add("Give the rule a name.");
        if (def.Actions.Count == 0) errors.Add("Add at least one action.");
        foreach (var c in def.Conditions)
        {
            if (!Enum.TryParse<ContactField>(c.Field, true, out _)) errors.Add($"Unknown field '{c.Field}'.");
            var needsValue = c.Operator is ConditionOperator.Equals or ConditionOperator.NotEquals or ConditionOperator.Contains or ConditionOperator.StartsWith or ConditionOperator.EndsWith or ConditionOperator.MatchesRegex;
            if (needsValue && string.IsNullOrWhiteSpace(c.Value)) errors.Add($"Condition on {c.Field} needs a value.");
        }
        foreach (var a in def.Actions)
        {
            if (a.Type == ActionType.SetField && !Enum.TryParse<ContactField>(a.Field, true, out _)) errors.Add("Set field: choose a field.");
            if (a.Type == ActionType.SetStatus && string.IsNullOrWhiteSpace(a.Value)) errors.Add("Set status: enter a status.");
            if (a.Type == ActionType.ProposeUpdate && trigger != AutomationTrigger.ExistingContactMatched) errors.Add("'Propose update' only works with the 'Existing contact matched' trigger.");
            if (a.Type == ActionType.GenerateImportReport && trigger != AutomationTrigger.ImportCompleted) errors.Add("'Generate import report' only works with the 'Import completed' trigger.");
        }
        return errors;
    }

    public async Task<AutomationRule> SaveAsync(int? id, string name, string? description, AutomationTrigger trigger, RuleDefinition def, bool enabled, CancellationToken ct = default)
    {
        var errors = Validate(name, trigger, def);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var rule = id is null ? new AutomationRule() : await ctx.AutomationRules.FirstAsync(r => r.Id == id, ct);
        rule.Name = name.Trim(); rule.Description = description; rule.Trigger = trigger; rule.IsEnabled = enabled;
        rule.ConditionsJson = RuleDefinition.SerializeConditions(def); rule.ActionsJson = RuleDefinition.SerializeActions(def);
        if (id is null) ctx.AutomationRules.Add(rule);
        await ctx.SaveChangesAsync(ct);
        return rule;
    }

    public async Task SetEnabledAsync(int id, bool enabled, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await ctx.AutomationRules.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsEnabled, enabled), ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await ctx.AutomationRules.Where(r => r.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<List<AutomationExecution>> HistoryAsync(int? ruleId = null, int take = 200, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.AutomationExecutions.AsNoTracking().Include(e => e.Rule).AsQueryable();
        if (ruleId is not null) q = q.Where(e => e.AutomationRuleId == ruleId);
        return await q.OrderByDescending(e => e.ExecutedAt).ThenByDescending(e => e.Id).Take(take).ToListAsync(ct);
    }

    /// <summary>Writes one execution row per rule (aggregated over many contacts) and a matching audit entry; updates rule counters.</summary>
    public async Task RecordAsync(AutomationTrigger trigger, IEnumerable<(ParsedRule Rule, int Contacts, int Changes)> summary, string? context, string? batchId = null, int? singleContactId = null, CancellationToken ct = default)
    {
        var rows = summary.Where(s => s.Contacts > 0).ToList();
        if (rows.Count == 0) return;
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        foreach (var (rule, contacts, changes) in rows)
        {
            var msg = $"{contacts} contact(s) matched, {changes} change(s)" + (context is null ? "" : $" - {context}");
            ctx.AutomationExecutions.Add(new AutomationExecution { AutomationRuleId = rule.Id, Trigger = trigger, AffectedCount = contacts, Message = msg, ContactId = singleContactId });
            ctx.AuditLogs.Add(Audit.Entry(AuditOperation.AutomationExecution, "AutomationRule", rule.Id, summary: $"Rule '{rule.Name}': {msg}", batchId: batchId));
            await ctx.AutomationRules.Where(r => r.Id == rule.Id).ExecuteUpdateAsync(s => s
                .SetProperty(r => r.LastRunAt, DateTime.UtcNow).SetProperty(r => r.RunCount, r => r.RunCount + 1), ct);
        }
        await ctx.SaveChangesAsync(ct);
    }

    /// <summary>Runs a rule's actions on a contact that was just created/edited by the user, then persists any resulting change.</summary>
    public async Task ApplyToSavedContactAsync(int contactId, AutomationTrigger trigger, IContactRepository repo, CancellationToken ct = default)
    {
        var rules = await LoadEnabledAsync(ct, trigger);
        if (rules.Count == 0) return;
        var contact = await repo.GetAsync(contactId, ct);
        if (contact is null) return;
        var cc = _settings.Current.DefaultCountryCode;
        var result = RuleEvaluator.Run(rules, trigger, contact, cc);
        if (result.Fired.Count == 0) return;
        await repo.UpdateAsync(contact, null, ct);
        await RecordAsync(trigger, result.Fired.Select(f => (f.Rule, 1, f.Changes)), $"contact #{contactId}", singleContactId: contactId, ct: ct);
    }

    /// <summary>Applies one rule (any trigger) to every contact. Returns (contacts matched, contacts changed). Takes a backup first when enabled.</summary>
    public async Task<(int Matched, int Changed)> RunNowAsync(int ruleId, IProgress<Core.DTOs.ProgressInfo>? progress = null, CancellationToken ct = default)
    {
        List<ParsedRule> rules;
        await using (var c0 = await _factory.CreateDbContextAsync(ct))
            rules = (await c0.AutomationRules.AsNoTracking().Where(r => r.Id == ruleId).ToListAsync(ct)).Select(Parse).ToList();
        if (rules.Count == 0) throw new InvalidOperationException("Rule not found.");
        if (rules[0].Error is not null) throw new InvalidOperationException(rules[0].Error);
        var rule = rules[0];
        var options = _settings.Current.ToQualityOptions();
        if (_backup is not null && _settings.Current.AutoBackupBeforeBulk) await _backup.CreateAsync(BackupKind.PreOperation, $"Before running rule '{rule.Name}'", ct);

        var batch = Audit.NewBatchId(); int matched = 0, changed = 0, lastId = 0;
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var total = await ctx.Contacts.CountAsync(ct); var done = 0;
        var companies = await CompanyCache.LoadAsync(ctx, ct);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = await ctx.Contacts.Where(c => c.Id > lastId).OrderBy(c => c.Id).Take(1000).ToListAsync(ct);
            if (chunk.Count == 0) break;
            foreach (var c in chunk)
            {
                if (!RuleEvaluator.Matches(rule.Definition, c, options.DefaultCountryCode)) continue;
                matched++;
                var before = c.Snapshot();
                var res = new ContactRuleResult();
                foreach (var a in rule.Definition.Actions) RuleEvaluator.Apply(a, c, options.DefaultCountryCode, res);
                ContactPreparer.Prepare(ctx, c, options, companies);
                var diff = ContactDiff.Compute(before, c);
                var flagsChanged = before.NeedsReview != c.NeedsReview || before.IsDuplicateFlagged != c.IsDuplicateFlagged || (before.IsIncomplete != c.IsIncomplete);
                if (diff.Count == 0 && !flagsChanged) continue;
                changed++; c.UpdatedAt = DateTime.UtcNow;
                ctx.AuditLogs.Add(Audit.Entry(AuditOperation.AutomationExecution, "Contact", c.Id, ContactDiff.ToJson(diff, true), ContactDiff.ToJson(diff, false),
                    summary: $"Rule '{rule.Name}' changed {c.DisplayName}", batchId: batch));
            }
            lastId = chunk[^1].Id; done += chunk.Count;
            await ctx.SaveChangesAsync(ct);
            ctx.ChangeTracker.Clear();
            progress?.Report(new Core.DTOs.ProgressInfo("Applying rule", done, total));
        }
        await tx.CommitAsync(ct);
        await RecordAsync(AutomationTrigger.Manual, new[] { (rule, matched, changed) }, "manual run", batch, ct: ct);
        return (matched, changed);
    }

    /// <summary>The six starter rules from the product brief. Only created when no rules exist yet.</summary>
    public async Task SeedDefaultsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        if (await ctx.AutomationRules.AnyAsync(ct)) return;
        void Add(string name, string desc, AutomationTrigger t, bool enabled, RuleDefinition d) => ctx.AutomationRules.Add(new AutomationRule
        { Name = name, Description = desc, Trigger = t, IsEnabled = enabled, ConditionsJson = RuleDefinition.SerializeConditions(d), ActionsJson = RuleDefinition.SerializeActions(d) });

        Add("Normalize phone on import", "When a new contact is imported, standardise the phone numbers.", AutomationTrigger.ContactImported, true,
            new() { Actions = { new(ActionType.NormalizePhone) } });
        Add("Flag duplicate emails", "When duplicate email addresses are detected, flag the records.", AutomationTrigger.DuplicateDetected, true,
            new() { Conditions = { new("Email", ConditionOperator.IsNotEmpty) }, Actions = { new(ActionType.FlagDuplicate) } });
        Add("Missing email = incomplete", "When an email is missing, classify the contact as incomplete.", AutomationTrigger.ContactImported, true,
            new() { Conditions = { new("Email", ConditionOperator.IsEmpty) }, Actions = { new(ActionType.MarkIncomplete) } });
        Add("Missing company = review", "When a company name is missing, flag the record for review.", AutomationTrigger.ContactImported, true,
            new() { Conditions = { new("Company", ConditionOperator.IsEmpty) }, Actions = { new(ActionType.FlagForReview) } });
        Add("Existing contact: propose update", "When an imported contact already exists, compare and propose an update (review it in Bulk Update).", AutomationTrigger.ExistingContactMatched, false,
            new() { Actions = { new(ActionType.ProposeUpdate) } });
        Add("Report after import", "When an import completes, generate an Excel import summary in your export folder.", AutomationTrigger.ImportCompleted, false,
            new() { Actions = { new(ActionType.GenerateImportReport) } });
        await ctx.SaveChangesAsync(ct);
    }
}
