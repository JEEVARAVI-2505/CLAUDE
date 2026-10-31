using LeadPilot.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace LeadPilot.Data;

/// <summary>Physical database operations used by backup/restore.</summary>
public sealed class DatabaseFile : IDatabaseFile
{
    private readonly SqliteDb _db;
    public DatabaseFile(SqliteDb db) { _db = db; }

    public string Path => _db.Path;
    public void BackupTo(string destination) => _db.BackupTo(destination);
    public int SchemaVersion() => _db.SchemaVersion();

    public string IntegrityCheck(string file)
    {
        try
        {
            using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            c.Open();
            var res = SqliteDb.Scalar<string>(c, null, "PRAGMA integrity_check") ?? "unknown";
            var version = SqliteDb.Scalar<long>(c, null, "PRAGMA user_version");
            if (version > Migrations.All.Max(m => m.Version)) return $"The backup was made by a newer version of LeadPilot AI (schema {version}).";
            if (SqliteDb.Scalar<long>(c, null, "SELECT COUNT(*) FROM sqlite_master WHERE name IN ('Leads','Businesses')") != 2) return "The file is not a LeadPilot AI database.";
            return res;
        }
        catch (SqliteException e) { return e.Message; }
    }

    public void ReplaceWith(string sourceFile)
    {
        SqliteDb.ReleaseFiles();
        foreach (var ext in new[] { "-wal", "-shm" }) { var f = _db.Path + ext; if (File.Exists(f)) File.Delete(f); }
        File.Copy(sourceFile, _db.Path, true);
        _db.Migrate();
    }
}
