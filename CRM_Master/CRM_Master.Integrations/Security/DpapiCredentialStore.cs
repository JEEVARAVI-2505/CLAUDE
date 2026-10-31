using System.Security.Cryptography;
using System.Text;
using CRM_Master.Integrations.Interfaces;

namespace CRM_Master.Integrations.Security;

/// <summary>
/// Stores secrets encrypted with Windows DPAPI (current-user scope): only the same Windows user on the same machine can decrypt them.
/// On other platforms there is no safe store, so <see cref="SaveAsync"/> refuses instead of writing plain text.
/// </summary>
public sealed class DpapiCredentialStore : ICredentialStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CRM MASTER credential store v1");
    private readonly string _folder;
    public DpapiCredentialStore(string folder) => _folder = folder;

    public bool IsAvailable => OperatingSystem.IsWindows();

    private string PathFor(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Any(c => !(char.IsLetterOrDigit(c) || c is '.' or '-' or '_')))
            throw new ArgumentException("Credential keys may only contain letters, digits, '.', '-' and '_'.", nameof(key));
        return Path.Combine(_folder, key + ".bin");
    }

    public Task SaveAsync(string key, string secret, CancellationToken ct = default)
    {
        var path = PathFor(key);
        if (!IsAvailable) throw new PlatformNotSupportedException("Secure credential storage needs Windows (DPAPI). Credentials are never stored in plain text.");
        Directory.CreateDirectory(_folder);
        return File.WriteAllBytesAsync(path, Protect(secret), ct);
    }

    public async Task<string?> LoadAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);
        if (!IsAvailable || !File.Exists(path)) return null;
        try { return Unprotect(await File.ReadAllBytesAsync(path, ct)); }
        catch (CryptographicException) { return null; }   // written by another user / machine
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private static byte[] Protect(string secret)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        return ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), Entropy, DataProtectionScope.CurrentUser);
    }

    private static string Unprotect(byte[] data)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        return Encoding.UTF8.GetString(ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser));
    }
}
