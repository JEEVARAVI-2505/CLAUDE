using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Data.DbContext;

/// <summary>Creates / upgrades the database by applying EF Core migrations.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IDbContextFactory<CrmDbContext> factory, CancellationToken ct = default)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);
        await ctx.Database.MigrateAsync(ct);
        // WAL gives crash-safe, concurrent-reader behaviour; busy_timeout avoids spurious "database is locked".
        await ctx.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
        await ctx.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", ct);
    }
}
