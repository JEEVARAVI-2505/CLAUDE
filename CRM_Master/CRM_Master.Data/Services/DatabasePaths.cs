using System.Text.Json;

namespace CRM_Master.Data.Services;

/// <summary>Resolves per-user storage locations (never the installation folder).</summary>
public static class DatabasePaths
{
    private const string EnvOverride = "CRM_MASTER_DATA";

    /// <summary>%LOCALAPPDATA%\CRM Master (or the folder in CRM_MASTER_DATA, for portable / test use).</summary>
    public static string AppDataFolder =>
        Environment.GetEnvironmentVariable(EnvOverride) is { Length: > 0 } o
            ? o
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CRM Master");

    public static string LogsFolder => Path.Combine(AppDataFolder, "Logs");
    public static string CredentialsFolder => Path.Combine(AppDataFolder, "Credentials");
    public static string DefaultBackupFolder => Path.Combine(AppDataFolder, "Backups");
    public static string DefaultExportFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) is { Length: > 0 } d ? d : AppDataFolder, "CRM Master Exports");
    private static string BootstrapFile => Path.Combine(AppDataFolder, "location.json");

    /// <summary>Database folder: the user-chosen one from location.json, else AppData\Data.</summary>
    public static string DatabaseFolder
    {
        get
        {
            try
            {
                if (File.Exists(BootstrapFile))
                {
                    var f = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(BootstrapFile))?.GetValueOrDefault("DatabaseFolder");
                    if (!string.IsNullOrWhiteSpace(f)) return f;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { /* fall back to default */ }
            return Path.Combine(AppDataFolder, "Data");
        }
    }

    public static string DatabaseFile => Path.Combine(DatabaseFolder, "crm_master.db");

    /// <summary>Remembers a new database folder; takes effect on next start.</summary>
    public static void SetDatabaseFolder(string folder)
    {
        Directory.CreateDirectory(AppDataFolder);
        File.WriteAllText(BootstrapFile, JsonSerializer.Serialize(new Dictionary<string, string> { ["DatabaseFolder"] = folder }));
    }
}
