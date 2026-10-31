using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;
using CRM_Master.Data.DbContext;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CRM_Master.Data.Services;

/// <summary>Consistent online backups (SQLite backup API - safe while the database is in use) and validated restores.</summary>
public sealed class BackupService : IBackupService
{
    private readonly SqliteContextFactory _factory;
    private readonly ISettingsService _settings;

    public BackupService(SqliteContextFactory factory, ISettingsService settings) { _factory = factory; _settings = settings; }

    private string BackupFolder => string.IsNullOrWhiteSpace(_settings.Current.BackupFolder) ? DatabasePaths.DefaultBackupFolder : _settings.Current.BackupFolder;

    public async Task<BackupRecord> CreateAsync(BackupKind kind, string? reason, CancellationToken ct = default)
    {
        Directory.CreateDirectory(BackupFolder);
        var file = Path.Combine(BackupFolder, $"crm-master-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{kind}.db");
        var record = await CreateAtAsync(file, reason, ct, kind);
        if (kind is BackupKind.Automatic or BackupKind.PreOperation) await PruneAsync(ct);
        return record;
    }

    public Task<BackupRecord> CreateAtAsync(string destinationFile, string? reason, CancellationToken ct = default) =>
        CreateAtAsync(destinationFile, reason, ct, BackupKind.Manual);

    private async Task<BackupRecord> CreateAtAsync(string destinationFile, string? reason, CancellationToken ct, BackupKind kind)
    {
        var dest = Path.GetFullPath(destinationFile);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        if (File.Exists(dest)) File.Delete(dest);

        await using (var ctx0 = await _factory.CreateDbContextAsync(ct))
        {
            var src = (SqliteConnection)ctx0.Database.GetDbConnection();
            await src.OpenAsync(ct);
            await using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = dest, Pooling = false }.ToString());
            await target.OpenAsync(ct);
            src.BackupDatabase(target);
        }
        SqliteConnection.ClearAllPools();

        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var record = new BackupRecord
        {
            FilePath = dest, Kind = kind, Reason = reason, SizeBytes = new FileInfo(dest).Length, ContactCount = await ctx.Contacts.CountAsync(ct)
        };
        ctx.Backups.Add(record);
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.Backup, "Database", null, null, null, summary: $"{kind} backup created ({record.ContactCount} contacts)" + (reason is null ? "" : $": {reason}")));
        await ctx.SaveChangesAsync(ct);
        return record;
    }

    public async Task<List<BackupRecord>> ListAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var rows = await ctx.Backups.AsNoTracking().OrderByDescending(b => b.CreatedAt).ToListAsync(ct);
        return rows.Where(r => File.Exists(r.FilePath)).ToList();
    }

    public async Task RestoreAsync(string backupFile, CancellationToken ct = default)
    {
        if (!File.Exists(backupFile)) throw new FileNotFoundException("The backup file was not found.", backupFile);
        await ValidateBackupAsync(backupFile, ct);

        var safety = await CreateAsync(BackupKind.PreRestore, "Automatic safety copy before restoring a backup", ct);
        SqliteConnection.ClearAllPools();
        foreach (var ext in new[] { "-wal", "-shm" }) { var f = _factory.DbPath + ext; if (File.Exists(f)) File.Delete(f); }
        File.Copy(backupFile, _factory.DbPath, overwrite: true);

        await DatabaseInitializer.InitializeAsync(_factory, ct);   // upgrades older backups to the current schema
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        ctx.Backups.Add(new BackupRecord { FilePath = safety.FilePath, Kind = BackupKind.PreRestore, Reason = safety.Reason, SizeBytes = safety.SizeBytes, ContactCount = safety.ContactCount, CreatedAt = safety.CreatedAt });
        ctx.AuditLogs.Add(Audit.Entry(AuditOperation.Restore, "Database", null, summary: $"Database restored from {Path.GetFileName(backupFile)}"));
        await ctx.SaveChangesAsync(ct);
    }

    private static async Task ValidateBackupAsync(string file, CancellationToken ct)
    {
        try
        {
            await using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            await c.OpenAsync(ct);
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = (string?)await cmd.ExecuteScalarAsync(ct);
            if (result != "ok") throw new InvalidDataException("The backup file failed the database integrity check and was not restored.");
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Contacts','AuditLogs','ApplicationSettings')";
            if (Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) != 3) throw new InvalidDataException("This file is not a CRM MASTER backup.");
        }
        catch (SqliteException ex) { throw new InvalidDataException("This file is not a valid CRM MASTER database backup: " + ex.Message, ex); }
        finally { SqliteConnection.ClearAllPools(); }
    }

    private async Task PruneAsync(CancellationToken ct)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        var old = await ctx.Backups.Where(b => b.Kind == BackupKind.Automatic || b.Kind == BackupKind.PreOperation)
            .OrderByDescending(b => b.CreatedAt).Skip(Math.Max(1, _settings.Current.BackupsToKeep)).ToListAsync(ct);
        foreach (var b in old) { try { if (File.Exists(b.FilePath)) File.Delete(b.FilePath); } catch (IOException) { continue; } ctx.Backups.Remove(b); }
        if (old.Count > 0) await ctx.SaveChangesAsync(ct);
    }
}
