using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Data.DbContext;

/// <summary>Creates short-lived <see cref="CrmDbContext"/> instances for one SQLite file (used by the app, engines and tests).</summary>
public sealed class SqliteContextFactory : IDbContextFactory<CrmDbContext>
{
    public SqliteContextFactory(string dbPath)
    {
        DbPath = Path.GetFullPath(dbPath);
        var dir = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath, ForeignKeys = true, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = true
        }.ToString();
    }

    public string DbPath { get; }
    public string ConnectionString { get; }

    public Task<CrmDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());

    public CrmDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlite(ConnectionString).Options;
        return new CrmDbContext(options);
    }
}

/// <summary>Design-time factory used only by <c>dotnet ef migrations</c>.</summary>
public sealed class DesignTimeFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<CrmDbContext>
{
    public CrmDbContext CreateDbContext(string[] args) =>
        new SqliteContextFactory(Path.Combine(Path.GetTempPath(), "crm-master-design.db")).CreateDbContext();
}
