using System.Text.Json;
using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;
using CRM_Master.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Data.Services;

public sealed partial class ContactRepository
{
    public async Task<Contact> AddAsync(Contact contact, IDictionary<string, string?>? customValues = null, CancellationToken ct = default)
    {
        var options = _settings.Current.ToQualityOptions();
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var companies = await CompanyCache.LoadAsync(ctx, ct);

        contact.Id = 0;
        contact.CreatedAt = contact.UpdatedAt = DateTime.UtcNow;
        ContactPreparer.Prepare(ctx, contact, options, companies);
        ctx.Contacts.Add(contact);
        await ctx.SaveChangesAsync(ct);

        await UpsertCustomValuesAsync(ctx, contact.Id, customValues, ct);
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.ContactCreated, "Contact", contact.Id, null, ContactDiff.SnapshotJson(contact),
            summary: $"Created {contact.DisplayName}"));
        await ctx.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        ctx.Entry(contact).State = EntityState.Detached;
        return contact;
    }

    public async Task UpdateAsync(Contact updated, IDictionary<string, string?>? customValues = null, CancellationToken ct = default)
    {
        var options = _settings.Current.ToQualityOptions();
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var existing = await ctx.Contacts.FirstOrDefaultAsync(c => c.Id == updated.Id, ct)
            ?? throw new InvalidOperationException($"Contact #{updated.Id} no longer exists.");
        var before = existing.Snapshot();

        foreach (var f in Contact.Fields) existing.Set(f.Field, updated.Get(f.Field));
        existing.NeedsReview = updated.NeedsReview;
        existing.IsDuplicateFlagged = updated.IsDuplicateFlagged;
        var companies = await CompanyCache.LoadAsync(ctx, ct);
        ContactPreparer.Prepare(ctx, existing, options, companies);

        var changes = ContactDiff.Compute(before, existing);
        var customChanges = await UpsertCustomValuesAsync(ctx, existing.Id, customValues, ct);
        if (changes.Count == 0 && customChanges.Count == 0 && before.NeedsReview == existing.NeedsReview && before.IsDuplicateFlagged == existing.IsDuplicateFlagged)
        { await tx.RollbackAsync(ct); return; }

        existing.UpdatedAt = DateTime.UtcNow;
        var prev = changes.ToDictionary(c => c.Field.ToString(), c => c.Before);
        var next = changes.ToDictionary(c => c.Field.ToString(), c => c.After);
        foreach (var (name, (b, a)) in customChanges) { prev["Custom:" + name] = b; next["Custom:" + name] = a; }
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.ContactUpdated, "Contact", existing.Id, JsonSerializer.Serialize(prev), JsonSerializer.Serialize(next),
            summary: $"Updated {existing.DisplayName}: {string.Join(", ", prev.Keys)}"));
        await ctx.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<int> DeleteAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return 0;
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var batch = Audit.NewBatchId();
        var deleted = 0;
        foreach (var chunk in ids.Distinct().Chunk(500))
        {
            var contacts = await ctx.Contacts.Include(c => c.CustomValues).ThenInclude(v => v.CustomField).Where(c => chunk.Contains(c.Id)).ToListAsync(ct);
            foreach (var c in contacts)
            {
                var snapshot = ContactDiff.SnapshotJson(c);
                if (c.CustomValues.Count > 0)
                    snapshot = snapshot.TrimEnd('}') + "," + string.Join(",", c.CustomValues.Select(v => $"{JsonSerializer.Serialize("Custom:" + v.CustomField!.Name)}:{JsonSerializer.Serialize(v.Value)}")) + "}";
                ctx.AuditLogs.Add(Audit.Entry(AuditOperation.ContactDeleted, "Contact", c.Id, snapshot, null, summary: $"Deleted {c.DisplayName}", batchId: batch));
            }
            ctx.Contacts.RemoveRange(contacts);
            deleted += contacts.Count;
            await ctx.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return deleted;
    }

    public async Task<int> ReevaluateQualityAsync(CancellationToken ct = default)
    {
        var options = _settings.Current.ToQualityOptions();
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        await using var tx = await ctx.Database.BeginTransactionAsync(ct);
        var changed = 0;
        var lastId = 0;
        while (true)
        {
            var batch = await ctx.Contacts.Where(c => c.Id > lastId).OrderBy(c => c.Id).Take(2000).ToListAsync(ct);
            if (batch.Count == 0) break;
            foreach (var c in batch)
            {
                var (es, ps, inc, ek, pk) = (c.EmailState, c.PhoneState, c.IsIncomplete, c.EmailKey, c.PhoneKey);
                ContactKeys.Apply(c, options.DefaultCountryCode);
                ContactQuality.Evaluate(c, options);
                if (es != c.EmailState || ps != c.PhoneState || inc != c.IsIncomplete || ek != c.EmailKey || pk != c.PhoneKey) changed++;
            }
            lastId = batch[^1].Id;
            await ctx.SaveChangesAsync(ct);
            ctx.ChangeTracker.Clear();
        }
        await tx.CommitAsync(ct);
        return changed;
    }

    /// <summary>Creates missing custom fields and upserts values. Returns (before, after) per changed field name.</summary>
    private static async Task<Dictionary<string, (string?, string?)>> UpsertCustomValuesAsync(CrmDbContext ctx, int contactId, IDictionary<string, string?>? values, CancellationToken ct)
    {
        var changes = new Dictionary<string, (string?, string?)>();
        if (values is null) return changes;
        var fields = await ctx.CustomFields.ToListAsync(ct);
        var existing = await ctx.ContactCustomFieldValues.Where(v => v.ContactId == contactId).ToListAsync(ct);
        foreach (var (rawName, rawValue) in values)
        {
            var name = TextCleaner.Clean(rawName); if (name is null) continue;
            var value = TextCleaner.Clean(rawValue);
            var field = fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            if (field is null) { field = new CustomField { Name = name }; ctx.CustomFields.Add(field); fields.Add(field); await ctx.SaveChangesAsync(ct); }
            var row = existing.FirstOrDefault(v => v.CustomFieldId == field.Id);
            if (row is null) { if (value is null) continue; ctx.ContactCustomFieldValues.Add(new ContactCustomFieldValue { ContactId = contactId, CustomFieldId = field.Id, Value = value }); changes[name] = (null, value); }
            else if (value is null) { ctx.ContactCustomFieldValues.Remove(row); changes[name] = (row.Value, null); }
            else if (row.Value != value) { changes[name] = (row.Value, value); row.Value = value; }
        }
        return changes;
    }
}
