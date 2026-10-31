using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LeadPilot.Application.Abstractions;

namespace LeadPilot.Infrastructure.Security;

/// <summary>
/// Stores API keys encrypted at rest. On Windows each value is protected with DPAPI (current-user scope, so other Windows
/// accounts and copied files cannot read it). On other platforms (development/tests) AES-GCM with a key file is used.
/// </summary>
public sealed class SecretStore : ISecretStore
{
    private readonly string _file;
    private readonly bool _useDpapi;
    private readonly object _lock = new();

    public SecretStore(string file, bool? useDpapi = null)
    {
        _file = file; _useDpapi = useDpapi ?? OperatingSystem.IsWindows();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
    }

    public string? Get(string name)
    {
        lock (_lock)
        {
            var d = Load();
            if (!d.TryGetValue(name, out var enc)) return null;
            try { return Decrypt(Convert.FromBase64String(enc)); }
            catch (Exception e) when (e is CryptographicException or FormatException) { return null; }
        }
    }

    public void Set(string name, string? value)
    {
        lock (_lock)
        {
            var d = Load();
            if (string.IsNullOrEmpty(value)) d.Remove(name); else d[name] = Convert.ToBase64String(Encrypt(value));
            File.WriteAllText(_file, JsonSerializer.Serialize(d));
        }
    }

    private Dictionary<string, string> Load()
    {
        if (!File.Exists(_file)) return new();
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_file)) ?? new(); }
        catch (JsonException) { return new(); }
    }

    private byte[] Encrypt(string v) => _useDpapi ? Dpapi(Encoding.UTF8.GetBytes(v), true) : Aes(Encoding.UTF8.GetBytes(v));
    private string Decrypt(byte[] b) => Encoding.UTF8.GetString(_useDpapi ? Dpapi(b, false) : AesDecrypt(b));

    [SupportedOSPlatform("windows")]
    private static byte[] DpapiWin(byte[] data, bool protect) =>
        protect ? ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser) : ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);

    private static byte[] Dpapi(byte[] data, bool protect) =>
        OperatingSystem.IsWindows() ? DpapiWin(data, protect) : throw new PlatformNotSupportedException("DPAPI is only available on Windows.");

    private byte[] Key()
    {
        var keyFile = _file + ".key";
        if (File.Exists(keyFile)) return Convert.FromBase64String(File.ReadAllText(keyFile));
        var k = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(keyFile, Convert.ToBase64String(k));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(keyFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return k;
    }

    private byte[] Aes(byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(12); var tag = new byte[16]; var ct = new byte[plain.Length];
        using var aes = new AesGcm(Key(), 16);
        aes.Encrypt(nonce, plain, ct, tag);
        return nonce.Concat(tag).Concat(ct).ToArray();
    }

    private byte[] AesDecrypt(byte[] blob)
    {
        var nonce = blob[..12]; var tag = blob[12..28]; var ct = blob[28..]; var plain = new byte[ct.Length];
        using var aes = new AesGcm(Key(), 16);
        aes.Decrypt(nonce, ct, tag, plain);
        return plain;
    }
}
