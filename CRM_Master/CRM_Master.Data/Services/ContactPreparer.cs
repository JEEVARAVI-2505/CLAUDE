using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.Entities;
using CRM_Master.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Data.Services;

/// <summary>Find-or-create lookup for companies (keyed by normalised name) within one unit of work.</summary>
public sealed class CompanyCache
{
    private readonly Dictionary<string, Company> _byKey;
    private CompanyCache(Dictionary<string, Company> map) => _byKey = map;

    public static async Task<CompanyCache> LoadAsync(CrmDbContext ctx, CancellationToken ct = default) =>
        new(await ctx.Companies.ToDictionaryAsync(c => c.NameKey, ct));

    public Company? GetOrAdd(CrmDbContext ctx, string? name)
    {
        var display = TextCleaner.Clean(name);
        if (display is null) return null;
        var key = CompanyRules.Key(display);
        if (key.Length == 0) return null;
        if (_byKey.TryGetValue(key, out var existing)) return existing;
        var company = new Company { Name = display, NameKey = key };
        ctx.Companies.Add(company);
        _byKey[key] = company;
        return company;
    }
}

/// <summary>Brings a contact into a storable state: lookup keys, quality flags, company link.</summary>
public static class ContactPreparer
{
    public static void Prepare(CrmDbContext ctx, Contact c, QualityOptions options, CompanyCache companies)
    {
        ContactKeys.Apply(c, options.DefaultCountryCode);
        ContactQuality.Evaluate(c, options);
        var company = companies.GetOrAdd(ctx, c.CompanyName);
        if (company is null) { c.Company = null; c.CompanyId = null; }
        else if (company.Id != 0) { c.CompanyId = company.Id; c.Company = null; }
        else c.Company = company;
    }
}
