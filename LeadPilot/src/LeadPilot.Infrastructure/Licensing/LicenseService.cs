using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Services;

namespace LeadPilot.Infrastructure.Licensing;

/// <summary>
/// Offline licence validation, kept apart from application logic. Trial: 14 days from first run, 200 leads, 1 user.
/// Paid keys are signed by the vendor (ECDSA P-256); the app contains only the public key, so keys cannot be forged from the installed files.
/// The trial start is stored in two places and the earlier date wins; like any offline trial this deters casual resets, not determined ones.
/// </summary>
public sealed class LicenseService : ILicenseService
{
    public const int TrialDays = 14, TrialMaxLeads = 200;
    private readonly AppPaths _paths;
    private readonly IClock _clock;
    private readonly ISettingsService _settings;
    private readonly string? _publicKeyPem;
    private LicenseInfo? _cached;

    /// <summary>Creates the service; <paramref name="publicKeyPem"/> overrides the embedded vendor key (tests).</summary>
    public LicenseService(AppPaths paths, IClock clock, ISettingsService settings, string? publicKeyPem = null)
    {
        _paths = paths; _clock = clock; _settings = settings;
        _publicKeyPem = publicKeyPem ?? LoadEmbeddedKey();
    }

    public string MachineId { get; } = ComputeMachineId();

    public LicenseInfo Current => _cached ??= Evaluate();

    public LicenseInfo Activate(string licenseKey)
    {
        var info = Validate(licenseKey.Trim());
        if (info.IsValid) { Directory.CreateDirectory(_paths.Data); File.WriteAllText(_paths.LicenseFile, licenseKey.Trim()); }
        _cached = info.IsValid ? info : Evaluate();
        return info;
    }

    private LicenseInfo Evaluate()
    {
        if (File.Exists(_paths.LicenseFile))
        {
            var info = Validate(File.ReadAllText(_paths.LicenseFile).Trim());
            if (info.IsValid) return info;
            var trial = Trial();
            return trial.IsValid ? trial with { Message = info.Message + " Running in trial mode." } : info;
        }
        return Trial();
    }

    private LicenseInfo Validate(string key)
    {
        if (string.IsNullOrWhiteSpace(_publicKeyPem) || !_publicKeyPem.Contains("BEGIN PUBLIC KEY", StringComparison.Ordinal))
            return Invalid("This build has no vendor licence key embedded, so paid licences cannot be validated.");
        using var pub = LicenseCodec.LoadPublicKey(_publicKeyPem);
        if (!LicenseCodec.TryVerify(key, pub, out var p, out var err)) return Invalid(err);
        var now = _clock.UtcNow;
        if (p!.ExpiresUtc is { } exp && exp < now) return Invalid($"This licence expired on {exp:yyyy-MM-dd}.");
        if (!string.IsNullOrEmpty(p.MachineId) && !string.Equals(p.MachineId, MachineId, StringComparison.OrdinalIgnoreCase))
            return Invalid("This licence is registered to a different computer.");
        return new LicenseInfo(p.Tier, p.Holder, p.MaxLeads, p.MaxUsers, p.ExpiresUtc, true, $"{p.Tier} licence for {p.Holder}.", false);
    }

    private LicenseInfo Trial()
    {
        var now = _clock.UtcNow;
        DateTime? fromMarker = null;
        try { if (File.Exists(_paths.TrialMarker) && DateTime.TryParse(File.ReadAllText(_paths.TrialMarker), null, System.Globalization.DateTimeStyles.RoundtripKind, out var d)) fromMarker = d.ToUniversalTime(); }
        catch (IOException) { /* treated as missing */ }
        var s = _settings.Current;
        var start = new[] { fromMarker, s.TrialStartedUtc }.Where(x => x != null).Select(x => x!.Value).DefaultIfEmpty(now).Min();
        if (fromMarker == null) { try { Directory.CreateDirectory(_paths.Data); File.WriteAllText(_paths.TrialMarker, start.ToString("o")); } catch (IOException) { /* ignore */ } }
        if (s.TrialStartedUtc != start) { s.TrialStartedUtc = start; _settings.Save(s); }
        var ends = start.AddDays(TrialDays);
        return now <= ends
            ? new LicenseInfo("Trial", "Free trial", TrialMaxLeads, 1, ends, true, $"Free trial – {Math.Max(0, (int)Math.Ceiling((ends - now).TotalDays))} day(s) left, up to {TrialMaxLeads} leads.", true)
            : new LicenseInfo("Trial", "Free trial", 0, 1, ends, false, $"Your free trial ended on {ends:yyyy-MM-dd}. Enter a licence key in About & Licensing to continue adding leads. Your data stays available for viewing and export.", true);
    }

    private static LicenseInfo Invalid(string msg) => new("None", "", 0, 1, null, false, msg, false);

    private static string? LoadEmbeddedKey()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("LeadPilot.Infrastructure.Licensing.vendor-public-key.pem");
        if (s == null) return null;
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }

    private static string ComputeMachineId()
    {
        string raw;
        try { raw = RawMachineId(); } catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException) { raw = ""; }
        if (string.IsNullOrWhiteSpace(raw)) raw = Environment.MachineName + Environment.UserName;
        var h = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("LeadPilotAI|" + raw)))[..16];
        return string.Join('-', Enumerable.Range(0, 4).Select(i => h.Substring(i * 4, 4)));
    }

    private static string RawMachineId()
    {
        if (OperatingSystem.IsWindows()) return WindowsGuid();
        return File.Exists("/etc/machine-id") ? File.ReadAllText("/etc/machine-id").Trim() : "";
    }

    [SupportedOSPlatform("windows")]
    private static string WindowsGuid()
    {
        using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        return k?.GetValue("MachineGuid") as string ?? "";
    }
}
