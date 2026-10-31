using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LeadPilot.Infrastructure.Licensing;

/// <summary>Signed claims inside a licence key.</summary>
public sealed class LicensePayload
{
    public string Tier { get; set; } = "Single";
    public string Holder { get; set; } = "";
    public string? MachineId { get; set; }
    public int MaxLeads { get; set; } = int.MaxValue;
    public int MaxUsers { get; set; } = 1;
    public DateTime IssuedUtc { get; set; }
    public DateTime? ExpiresUtc { get; set; }
}

/// <summary>Offline licence format: <c>LPAI1-&lt;base64url payload&gt;.&lt;base64url ECDSA-P256 signature&gt;</c>. Only the vendor holds the private key.</summary>
public static class LicenseCodec
{
    public const string Prefix = "LPAI1-";

    public static string Sign(LicensePayload payload, ECDsa privateKey)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var sig = privateKey.SignData(bytes, HashAlgorithmName.SHA256);
        return Prefix + B64(bytes) + "." + B64(sig);
    }

    public static bool TryVerify(string key, ECDsa publicKey, out LicensePayload? payload, out string error)
    {
        payload = null; error = "";
        try
        {
            key = key.Trim();
            if (!key.StartsWith(Prefix, StringComparison.Ordinal)) { error = "This is not a LeadPilot AI licence key."; return false; }
            var parts = key[Prefix.Length..].Split('.');
            if (parts.Length != 2) { error = "The licence key is malformed."; return false; }
            var data = FromB64(parts[0]); var sig = FromB64(parts[1]);
            if (!publicKey.VerifyData(data, sig, HashAlgorithmName.SHA256)) { error = "The licence signature is not valid."; return false; }
            payload = JsonSerializer.Deserialize<LicensePayload>(data);
            if (payload == null) { error = "The licence key is empty."; return false; }
            return true;
        }
        catch (Exception e) when (e is FormatException or JsonException or CryptographicException or ArgumentException)
        {
            error = "The licence key could not be read."; return false;
        }
    }

    public static ECDsa LoadPublicKey(string pem) { var k = ECDsa.Create(); k.ImportFromPem(pem); return k; }

    private static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromB64(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
