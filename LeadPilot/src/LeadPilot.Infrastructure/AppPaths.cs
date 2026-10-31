namespace LeadPilot.Infrastructure;

/// <summary>Application-specific data folders. All user data lives under one root so uninstall can preserve it.</summary>
public sealed class AppPaths : LeadPilot.Application.Abstractions.IAppFolders
{
    public string Root { get; }
    public string Data => Path.Combine(Root, "Data");
    public string Backups => Path.Combine(Root, "Backups");
    public string Logs => Path.Combine(Root, "Logs");
    public string Exports => Path.Combine(Root, "Exports");
    public string DatabaseFile => Path.Combine(Data, "leadpilot.db");
    public string SecretsFile => Path.Combine(Data, "secrets.dat");
    public string LicenseFile => Path.Combine(Data, "license.key");
    public string TrialMarker => Path.Combine(Data, "install.dat");

    /// <summary>Uses <paramref name="root"/>, else the LEADPILOT_DATA_DIR environment variable (portable/test use), else %LOCALAPPDATA%\LeadPilotAI.</summary>
    public AppPaths(string? root = null)
    {
        var env = Environment.GetEnvironmentVariable("LEADPILOT_DATA_DIR");
        Root = root ?? (string.IsNullOrWhiteSpace(env) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LeadPilotAI") : env);
    }

    /// <summary>Creates all folders.</summary>
    public void EnsureCreated() { foreach (var d in new[] { Root, Data, Backups, Logs, Exports }) Directory.CreateDirectory(d); }
}
