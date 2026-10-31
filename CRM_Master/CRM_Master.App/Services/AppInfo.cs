using System.Reflection;
using System.Text.Json;

namespace CRM_Master.App.Services;

/// <summary>Product identity. Developer / support details come from an optional branding.json so nothing is invented.</summary>
public static class AppInfo
{
    public const string ProductName = "CRM MASTER";
    public const string Tagline = "Intelligent CRM Data Management & Automation Suite";
    public static string Version { get; } = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";

    private static readonly Lazy<Dictionary<string, string>> Branding = new(() =>
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "branding.json");
            if (File.Exists(path)) return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or JsonException) { /* fall back to placeholders */ }
        return new();
    });

    private static string Get(string key, string fallback) => Branding.Value.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

    public static string Developer => Get("Developer", "Developer details not configured - add them to branding.json before distribution.");
    public static string Support => Get("Support", "Support contact not configured - add it to branding.json before distribution.");
    public static string Licence => Get("Licence", "Licence terms not configured. This evaluation build is provided as-is, without warranty.");

    public static string ThirdPartyNotices
    {
        get
        {
            var path = Path.Combine(AppContext.BaseDirectory, "THIRD_PARTY_NOTICES.txt");
            return File.Exists(path) ? File.ReadAllText(path) : "Third-party notices file not found.";
        }
    }
}
