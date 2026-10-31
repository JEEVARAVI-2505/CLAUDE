using CRM_Master.Core.Entities;
using CRM_Master.Data.DbContext;
using CRM_Master.Data.Services;
using Microsoft.Data.Sqlite;

namespace CRM_Master.Tests.Support;

/// <summary>A real, migrated SQLite database in a temp folder with the standard services wired up.</summary>
public sealed class TestDb : IDisposable
{
    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "crm-master-tests", Guid.NewGuid().ToString("N"));
    public SqliteContextFactory Factory { get; }
    public SettingsService Settings { get; }
    public ContactRepository Contacts { get; }
    public AuditService Audit { get; }
    public BackupService Backups { get; }

    private TestDb()
    {
        Directory.CreateDirectory(Folder);
        Factory = new SqliteContextFactory(Path.Combine(Folder, "test.db"));
        Settings = new SettingsService(Factory);
        Contacts = new ContactRepository(Factory, Settings);
        Audit = new AuditService(Factory);
        Backups = new BackupService(Factory, Settings);
    }

    public static async Task<TestDb> CreateAsync()
    {
        var db = new TestDb();
        await DatabaseInitializer.InitializeAsync(db.Factory);
        await db.Settings.LoadAsync();
        var s = db.Settings.Current.Clone();
        s.BackupFolder = Path.Combine(db.Folder, "backups");
        s.ExportFolder = Path.Combine(db.Folder, "exports");
        await db.Settings.SaveAsync(s);
        return db;
    }

    public string PathIn(string name) => Path.Combine(Folder, name);

    public static Contact Person(string name, string? email = null, string? phone = null, string? company = null, string? city = null) =>
        new() { FullName = name, Email = email, Phone = phone, CompanyName = company, City = city };

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(Folder, true); } catch (IOException) { /* temp folder; best effort */ }
    }
}
