namespace CRM_Master.Core.BusinessRules;

/// <summary>Website normalisation and validation.</summary>
public static class UrlRules
{
    /// <summary>
    /// Lower-cases scheme and host, adds https:// when no scheme is present, drops default ports, fragments and a root trailing slash.
    /// Returns null when the input is empty or cannot be a web address.
    /// </summary>
    public static string? Normalize(string? raw)
    {
        var t = TextCleaner.Clean(raw);
        if (t is null) return null;
        t = t.Replace(" ", "");
        if (!t.Contains("://", StringComparison.Ordinal)) t = "https://" + t.TrimStart('/');
        if (!Uri.TryCreate(t, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || string.IsNullOrEmpty(uri.Host))
            return null;
        var path = uri.AbsolutePath == "/" ? "" : uri.AbsolutePath.TrimEnd('/');
        var port = uri.IsDefaultPort ? "" : ":" + uri.Port;
        return $"{uri.Scheme}://{uri.Host.ToLowerInvariant()}{port}{path}{uri.Query}";
    }

    /// <summary>True when the text normalises to an http(s) URL whose host looks like a real domain (contains a dot and an alphabetic TLD).</summary>
    public static bool IsValid(string? raw)
    {
        var n = Normalize(raw);
        if (n is null) return false;
        var host = new Uri(n).Host;
        var dot = host.LastIndexOf('.');
        return dot > 0 && dot < host.Length - 2 && host[(dot + 1)..].All(char.IsLetter);
    }

    /// <summary>Host without "www." for matching two sites that differ only by prefix.</summary>
    public static string? HostKey(string? raw)
    {
        var n = Normalize(raw);
        if (n is null) return null;
        var host = new Uri(n).Host;
        return host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
    }
}
