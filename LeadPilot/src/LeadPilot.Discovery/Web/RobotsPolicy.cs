namespace LeadPilot.Discovery.Web;

/// <summary>robots.txt rules for one host (RFC 9309 semantics: longest matching rule wins, Allow beats Disallow on ties).</summary>
public sealed class RobotsRules
{
    private readonly List<(bool Allow, string Path)> _rules = new();
    public TimeSpan CrawlDelay { get; private set; } = TimeSpan.Zero;
    public bool DisallowAll { get; init; }

    public static RobotsRules AllowAll { get; } = new();
    public static RobotsRules BlockAll { get; } = new() { DisallowAll = true };

    /// <summary>Parses robots.txt text for the given product token (falls back to the * group).</summary>
    public static RobotsRules Parse(string text, string agent)
    {
        var groups = new List<(List<string> Agents, List<(bool, string)> Rules, double Delay)>();
        List<string>? curAgents = null; List<(bool, string)>? curRules = null; double delay = 0; var lastWasAgent = false;
        void Flush() { if (curAgents != null) groups.Add((curAgents, curRules!, delay)); }
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Split('#')[0].Trim();
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            var field = line[..idx].Trim().ToLowerInvariant(); var value = line[(idx + 1)..].Trim();
            if (field == "user-agent")
            {
                if (!lastWasAgent) { Flush(); curAgents = new(); curRules = new(); delay = 0; }
                curAgents!.Add(value.ToLowerInvariant()); lastWasAgent = true; continue;
            }
            lastWasAgent = false;
            if (curRules == null) continue;
            if (field == "disallow") curRules.Add((false, value));
            else if (field == "allow") curRules.Add((true, value));
            else if (field == "crawl-delay" && double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) delay = d;
        }
        Flush();
        var a = agent.ToLowerInvariant();
        var g = groups.FirstOrDefault(x => x.Agents.Any(n => n != "*" && a.Contains(n, StringComparison.Ordinal)));
        if (g.Agents == null) g = groups.FirstOrDefault(x => x.Agents.Contains("*"));
        var r = new RobotsRules();
        if (g.Agents != null) { r._rules.AddRange(g.Rules); r.CrawlDelay = TimeSpan.FromSeconds(Math.Min(10, g.Delay)); }
        return r;
    }

    /// <summary>Whether the path (with query) may be fetched.</summary>
    public bool IsAllowed(string pathAndQuery)
    {
        if (DisallowAll) return false;
        (bool Allow, int Len)? best = null;
        foreach (var (allow, rule) in _rules)
        {
            if (rule.Length == 0) continue; // empty Disallow = allow all
            if (!Matches(rule, pathAndQuery)) continue;
            if (best == null || rule.Length > best.Value.Len || (rule.Length == best.Value.Len && allow)) best = (allow, rule.Length);
        }
        return best?.Allow ?? true;
    }

    private static bool Matches(string rule, string path)
    {
        var anchored = rule.EndsWith('$'); if (anchored) rule = rule[..^1];
        var parts = rule.Split('*');
        var pos = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (i == 0) { if (!path.StartsWith(p, StringComparison.Ordinal)) return false; pos = p.Length; continue; }
            var at = path.IndexOf(p, pos, StringComparison.Ordinal);
            if (at < 0) return false;
            pos = at + p.Length;
        }
        return !anchored || pos == path.Length || (parts.Length > 1 && parts[^1].Length == 0);
    }
}
