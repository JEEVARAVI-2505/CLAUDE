using System.Security.Cryptography;
using LeadPilot.Infrastructure.Licensing;

namespace LeadPilot.LicenseTool;

/// <summary>
/// Vendor-side tool (never shipped to customers).
///   keygen --out DIR                      create private.pem + public.pem (keep private.pem secret and OFF the repository)
///   sign --key private.pem --holder NAME --tier Single|Professional|Business [--users N] [--max-leads N] [--days N] [--machine ID]
///   verify --pub public.pem --license KEY
/// </summary>
public static class Program
{
    public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0) { error.WriteLine("Usage: keygen | sign | verify (see source header)"); return 2; }
        var o = ParseOptions(args.Skip(1).ToArray());
        try
        {
            switch (args[0])
            {
                case "keygen":
                {
                    var dir = Required(o, "out"); Directory.CreateDirectory(dir);
                    using var k = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                    var priv = Path.Combine(dir, "private.pem"); var pub = Path.Combine(dir, "public.pem");
                    if (File.Exists(priv)) { error.WriteLine("private.pem already exists - refusing to overwrite."); return 1; }
                    File.WriteAllText(priv, k.ExportPkcs8PrivateKeyPem()); File.WriteAllText(pub, k.ExportSubjectPublicKeyInfoPem());
                    output.WriteLine($"Created {priv} (SECRET - back it up, never commit it) and {pub}.");
                    output.WriteLine("Copy public.pem over src/LeadPilot.Infrastructure/Licensing/vendor-public-key.pem and rebuild before shipping.");
                    return 0;
                }
                case "sign":
                {
                    using var k = ECDsa.Create(); k.ImportFromPem(File.ReadAllText(Required(o, "key")));
                    var tier = Required(o, "tier");
                    if (tier is not ("Single" or "Professional" or "Business")) throw new ArgumentException("tier must be Single, Professional or Business");
                    var defaultUsers = tier switch { "Single" => 1, "Professional" => 3, _ => 10 };
                    var p = new LicensePayload
                    {
                        Tier = tier, Holder = Required(o, "holder"), MachineId = o.GetValueOrDefault("machine"), IssuedUtc = DateTime.UtcNow,
                        MaxUsers = int.Parse(o.GetValueOrDefault("users") ?? defaultUsers.ToString()), MaxLeads = int.Parse(o.GetValueOrDefault("max-leads") ?? int.MaxValue.ToString()),
                        ExpiresUtc = o.TryGetValue("days", out var d) ? DateTime.UtcNow.AddDays(int.Parse(d)) : null
                    };
                    if (tier == "Single" && string.IsNullOrEmpty(p.MachineId)) throw new ArgumentException("Single-PC licences need --machine (the customer's computer ID from About & Licensing).");
                    output.WriteLine(LicenseCodec.Sign(p, k));
                    return 0;
                }
                case "verify":
                {
                    using var pub = LicenseCodec.LoadPublicKey(File.ReadAllText(Required(o, "pub")));
                    if (!LicenseCodec.TryVerify(Required(o, "license"), pub, out var payload, out var err)) { error.WriteLine("INVALID: " + err); return 1; }
                    output.WriteLine($"VALID  tier={payload!.Tier} holder={payload.Holder} users={payload.MaxUsers} expires={(payload.ExpiresUtc?.ToString("yyyy-MM-dd") ?? "never")} machine={(payload.MachineId ?? "any")}");
                    return 0;
                }
                default: error.WriteLine("Unknown command " + args[0]); return 2;
            }
        }
        catch (Exception e) when (e is ArgumentException or FormatException or IOException or CryptographicException or KeyNotFoundException) { error.WriteLine("Error: " + e.Message); return 1; }
    }

    private static Dictionary<string, string> ParseOptions(string[] a)
    {
        var d = new Dictionary<string, string>();
        for (var i = 0; i + 1 < a.Length; i += 2) d[a[i].TrimStart('-')] = a[i + 1];
        return d;
    }

    private static string Required(Dictionary<string, string> o, string k) => o.TryGetValue(k, out var v) ? v : throw new ArgumentException($"--{k} is required");
}
