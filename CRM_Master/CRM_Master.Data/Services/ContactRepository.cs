using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Data.Services;

/// <summary>SQLite-backed contact store (read side). See ContactRepository.Write.cs for mutations.</summary>
public sealed partial class ContactRepository : IContactRepository
{
    private readonly IDbContextFactory<CrmDbContext> _factory;
    private readonly ISettingsService _settings;

    public ContactRepository(IDbContextFactory<CrmDbContext> factory, ISettingsService settings)
    {
        _factory = factory; _settings = settings;
    }

    internal static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static IQueryable<Contact> Filter(IQueryable<Contact> s, ContactQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Status)) s = s.Where(c => c.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Country)) s = s.Where(c => c.Country == q.Country);
        s = q.Quality switch
        {
            QualityFilter.InvalidEmail => s.Where(c => c.EmailState == ValidationState.Invalid),
            QualityFilter.InvalidPhone => s.Where(c => c.PhoneState == ValidationState.Invalid),
            QualityFilter.MissingEmail => s.Where(c => c.EmailState == ValidationState.Missing),
            QualityFilter.MissingPhone => s.Where(c => c.PhoneState == ValidationState.Missing),
            QualityFilter.Incomplete => s.Where(c => c.IsIncomplete),
            QualityFilter.NeedsReview => s.Where(c => c.NeedsReview),
            QualityFilter.FlaggedDuplicate => s.Where(c => c.IsDuplicateFlagged),
            QualityFilter.Clean => s.Where(c => !c.IsIncomplete && c.EmailState != ValidationState.Invalid && c.PhoneState != ValidationState.Invalid && !c.NeedsReview && !c.IsDuplicateFlagged),
            _ => s
        };
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            foreach (var raw in q.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var term = "%" + EscapeLike(raw) + "%";
                var digits = new string(raw.Where(char.IsDigit).ToArray());
                var digitPattern = digits.Length >= 3 ? "%" + digits + "%" : null;
                s = s.Where(c =>
                    EF.Functions.Like(c.FullName ?? "", term, "\\") || EF.Functions.Like(c.FirstName ?? "", term, "\\") ||
                    EF.Functions.Like(c.LastName ?? "", term, "\\") || EF.Functions.Like(c.Email ?? "", term, "\\") ||
                    EF.Functions.Like(c.Phone ?? "", term, "\\") || EF.Functions.Like(c.CompanyName ?? "", term, "\\") ||
                    EF.Functions.Like(c.City ?? "", term, "\\") || EF.Functions.Like(c.JobTitle ?? "", term, "\\") ||
                    (digitPattern != null && (EF.Functions.Like(c.PhoneKey ?? "", digitPattern) || EF.Functions.Like(c.AlternatePhoneKey ?? "", digitPattern))));
            }
        }
        return s;
    }

    private static IQueryable<Contact> Sort(IQueryable<Contact> s, ContactQuery q)
    {
        var column = ContactQuery.SortableColumns.Contains(q.SortBy) ? q.SortBy : "UpdatedAt";
        var d = q.Descending;
        IOrderedQueryable<Contact> o = column switch
        {
            "FullName" => d ? s.OrderByDescending(c => c.FullName ?? c.FirstName) : s.OrderBy(c => c.FullName ?? c.FirstName),
            "Company" => d ? s.OrderByDescending(c => c.CompanyName) : s.OrderBy(c => c.CompanyName),
            "Email" => d ? s.OrderByDescending(c => c.Email) : s.OrderBy(c => c.Email),
            "Phone" => d ? s.OrderByDescending(c => c.Phone) : s.OrderBy(c => c.Phone),
            "City" => d ? s.OrderByDescending(c => c.City) : s.OrderBy(c => c.City),
            "Country" => d ? s.OrderByDescending(c => c.Country) : s.OrderBy(c => c.Country),
            "Status" => d ? s.OrderByDescending(c => c.Status) : s.OrderBy(c => c.Status),
            "CreatedAt" => d ? s.OrderByDescending(c => c.CreatedAt) : s.OrderBy(c => c.CreatedAt),
            _ => d ? s.OrderByDescending(c => c.UpdatedAt) : s.OrderBy(c => c.UpdatedAt),
        };
        return o.ThenBy(c => c.Id); // stable paging
    }

    public async Task<PagedResult<Contact>> QueryAsync(ContactQuery query, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var filtered = Filter(ctx.Contacts.AsNoTracking(), query);
        var total = await filtered.CountAsync(ct);
        var size = Math.Clamp(query.PageSize, 1, 5000);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
        var page = Math.Clamp(query.Page, 1, pages);
        var items = await Sort(filtered, query).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<Contact>(items, total, page, size);
    }

    public async Task<List<int>> QueryIdsAsync(ContactQuery query, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await Sort(Filter(ctx.Contacts.AsNoTracking(), query), query).Select(c => c.Id).ToListAsync(ct);
    }

    public async Task<Contact?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.Contacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Contact>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var list = ids.Distinct().ToList();
        var result = new List<Contact>(list.Count);
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        foreach (var chunk in list.Chunk(500))   // stay below SQLite's variable limit
            result.AddRange(await ctx.Contacts.AsNoTracking().Where(c => chunk.Contains(c.Id)).ToListAsync(ct));
        return result;
    }

    public async Task<List<Contact>> GetAllAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.Contacts.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.Contacts.CountAsync(ct);
    }

    public async Task<ContactHistory> GetHistoryAsync(int contactId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var entries = await ctx.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Contact" && a.RecordId == contactId)
            .OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id).Take(500).ToListAsync(ct);
        var jobId = await ctx.Contacts.Where(c => c.Id == contactId).Select(c => c.ImportJobId).FirstOrDefaultAsync(ct);
        var job = jobId is null ? null : await ctx.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        return new ContactHistory(entries, job);
    }

    public async Task<List<string>> GetDistinctAsync(ContactField field, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var q = ctx.Contacts.AsNoTracking();
        IQueryable<string?> values = field switch
        {
            ContactField.Status => q.Select(c => (string?)c.Status),
            ContactField.Country => q.Select(c => c.Country),
            ContactField.LeadSource => q.Select(c => c.LeadSource),
            ContactField.City => q.Select(c => c.City),
            ContactField.State => q.Select(c => c.State),
            ContactField.Company => q.Select(c => c.CompanyName),
            _ => throw new ArgumentOutOfRangeException(nameof(field), "Distinct values are only offered for category-like fields.")
        };
        return await values.Where(v => v != null && v != "").Distinct().OrderBy(v => v).Take(500).Select(v => v!).ToListAsync(ct);
    }

    public async Task<Dictionary<string, string?>> GetCustomValuesAsync(int contactId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.ContactCustomFieldValues.AsNoTracking().Where(v => v.ContactId == contactId)
            .Select(v => new { v.CustomField!.Name, v.Value }).ToDictionaryAsync(x => x.Name, x => x.Value, ct);
    }

    public async Task<List<CustomField>> GetCustomFieldsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        return await ctx.CustomFields.AsNoTracking().Where(f => f.IsActive).OrderBy(f => f.Name).ToListAsync(ct);
    }
}
