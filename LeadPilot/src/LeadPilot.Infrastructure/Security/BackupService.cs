using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LeadPilot.Application.Abstractions;

namespace LeadPilot.Infrastructure.Security;

/// <summary>
/// Backs up the database as a zip (manifest + consistent SQLite copy). With a password the zip is encrypted with AES-256-GCM,
/// key derived by PBKDF2-SHA256 (600 000 iterations, random salt). Restore verifies integrity and keeps a safety copy first.
/// </summary>
public sealed class BackupService : IBackupService
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("LPBK1");
    private const int Iterations = 600_000;
    private readonly IDatabaseFile _db;
    private readonly AppPaths _paths;
    private readonly IClock _clock;

    public BackupService(IDatabaseFile db, AppPaths paths, IClock clock) { _db = db; _paths = paths; _clock = clock; }

    public string CreateBackup(string destinationFolder, string? password)
    {
        Directory.CreateDirectory(destinationFolder);
        var temp = Path.Combine(Path.GetTempPath(), "lpbk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var dbCopy = Path.Combine(temp, "leadpilot.db");
            _db.BackupTo(dbCopy);
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                zip.CreateEntryFromFile(dbCopy, "leadpilot.db", CompressionLevel.Optimal);
                var e = zip.CreateEntry("manifest.json");
                using var w = new StreamWriter(e.Open());
                w.Write(JsonSerializer.Serialize(new { Product = "LeadPilot AI", CreatedUtc = _clock.UtcNow, SchemaVersion = _db.SchemaVersion(), Encrypted = !string.IsNullOrEmpty(password) }));
            }
            var name = $"LeadPilot_Backup_{_clock.UtcNow:yyyyMMdd_HHmmss}{(string.IsNullOrEmpty(password) ? "" : "_encrypted")}.lpbackup";
            var target = Path.Combine(destinationFolder, name);
            File.WriteAllBytes(target, string.IsNullOrEmpty(password) ? ms.ToArray() : Encrypt(ms.ToArray(), password));
            return target;
        }
        finally { try { Directory.Delete(temp, true); } catch (IOException) { /* best effort */ } }
    }

    public void Restore(string backupFile, string? password)
    {
        var raw = File.ReadAllBytes(backupFile);
        byte[] zipBytes;
        if (raw.AsSpan().StartsWith(Magic))
        {
            if (string.IsNullOrEmpty(password)) throw new CryptographicException("This backup is encrypted. Enter its password.");
            zipBytes = Decrypt(raw, password);
        }
        else zipBytes = raw;

        var temp = Path.Combine(Path.GetTempPath(), "lpres-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var extracted = Path.Combine(temp, "leadpilot.db");
            try
            {
                using var zip = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
                var entry = zip.GetEntry("leadpilot.db") ?? throw new InvalidDataException("The backup does not contain a database.");
                if (entry.Length > 4L * 1024 * 1024 * 1024) throw new InvalidDataException("The backup is unreasonably large.");
                entry.ExtractToFile(extracted, true);
            }
            catch (InvalidDataException) { throw; }
            var check = _db.IntegrityCheck(extracted);
            if (check != "ok") throw new InvalidDataException("The backup failed its integrity check: " + check);

            Directory.CreateDirectory(_paths.Backups);
            var safety = Path.Combine(_paths.Backups, $"pre-restore_{_clock.UtcNow:yyyyMMdd_HHmmss}.db");
            _db.BackupTo(safety);
            _db.ReplaceWith(extracted);
        }
        finally { try { Directory.Delete(temp, true); } catch (IOException) { /* best effort */ } }
    }

    private static byte[] Encrypt(byte[] plain, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16); var nonce = RandomNumberGenerator.GetBytes(12); var tag = new byte[16]; var ct = new byte[plain.Length];
        using var aes = new AesGcm(Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32), 16);
        aes.Encrypt(nonce, plain, ct, tag, Magic);
        return Magic.Concat(salt).Concat(nonce).Concat(tag).Concat(ct).ToArray();
    }

    private static byte[] Decrypt(byte[] blob, string password)
    {
        if (blob.Length < Magic.Length + 44) throw new InvalidDataException("The backup file is truncated.");
        var o = Magic.Length;
        var salt = blob[o..(o + 16)]; var nonce = blob[(o + 16)..(o + 28)]; var tag = blob[(o + 28)..(o + 44)]; var ct = blob[(o + 44)..]; var plain = new byte[ct.Length];
        try
        {
            using var aes = new AesGcm(Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32), 16);
            aes.Decrypt(nonce, ct, tag, plain, Magic);
        }
        catch (CryptographicException) { throw new CryptographicException("Wrong password, or the backup file is damaged."); }
        return plain;
    }
}
