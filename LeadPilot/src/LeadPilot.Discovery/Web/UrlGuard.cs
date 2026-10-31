using System.Net;
using System.Net.Sockets;

namespace LeadPilot.Discovery.Web;

/// <summary>Blocks requests to private/loopback/link-local addresses so a crafted website value cannot probe the user's own network (SSRF).</summary>
public static class UrlGuard
{
    /// <summary>True when the host resolves only to public addresses.</summary>
    public static async Task<bool> IsPublicHostAsync(string host, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(host) || host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            var addrs = IPAddress.TryParse(host, out var lit) ? new[] { lit } : await Dns.GetHostAddressesAsync(host, ct).ConfigureAwait(false);
            return addrs.Length > 0 && addrs.All(IsPublic);
        }
        catch (SocketException) { return false; }
    }

    /// <summary>True for globally routable unicast addresses.</summary>
    public static bool IsPublic(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.None)) return false;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return false;
            var b6 = ip.GetAddressBytes();
            return (b6[0] & 0xFE) != 0xFC; // fc00::/7 unique local
        }
        var b = ip.GetAddressBytes();
        return !(b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254) || (b[0] == 100 && b[1] is >= 64 and <= 127) || b[0] == 0 || b[0] >= 224);
    }
}
