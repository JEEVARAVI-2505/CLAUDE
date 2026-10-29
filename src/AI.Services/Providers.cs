using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BuildingStudio.AI.Services;

/// <summary>Text-completion provider. Implementations must never throw: return null when unavailable.</summary>
public interface IAiProvider
{
    string Name { get; }
    bool IsEnabled { get; }
    Task<string?> CompleteAsync(string prompt, bool json = false, CancellationToken ct = default);
}

/// <summary>AI switched off. Every feature falls back to deterministic code.</summary>
public sealed class NoAiProvider : IAiProvider
{
    public string Name => "Off";
    public bool IsEnabled => false;
    public Task<string?> CompleteAsync(string prompt, bool json = false, CancellationToken ct = default) => Task.FromResult<string?>(null);
}

/// <summary>Local model through the Ollama HTTP API (http://localhost:11434).</summary>
public sealed class OllamaProvider(HttpClient http, string model = "llama3.1", string baseUrl = "http://localhost:11434") : IAiProvider
{
    public string Name => $"Ollama ({model})";
    public bool IsEnabled => true;

    public async Task<string?> CompleteAsync(string prompt, bool json = false, CancellationToken ct = default)
    {
        try
        {
            var body = new Dictionary<string, object> { ["model"] = model, ["prompt"] = prompt, ["stream"] = false };
            if (json) body["format"] = "json";
            using var res = await http.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/api/generate", body, ct);
            if (!res.IsSuccessStatusCode) return null;
            var node = await res.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct);
            return node?["response"]?.GetValue<string>();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}

/// <summary>Google Gemini (free tier) through the REST API. The API key stays on the user's machine.</summary>
public sealed class GeminiProvider(HttpClient http, string apiKey, string model = "gemini-1.5-flash") : IAiProvider
{
    public string Name => $"Gemini ({model})";
    public bool IsEnabled => !string.IsNullOrWhiteSpace(apiKey);

    public async Task<string?> CompleteAsync(string prompt, bool json = false, CancellationToken ct = default)
    {
        if (!IsEnabled) return null;
        try
        {
            var body = new JsonObject
            {
                ["contents"] = new JsonArray(new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = prompt }) }),
            };
            if (json) body["generationConfig"] = new JsonObject { ["responseMimeType"] = "application/json" };
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
            using var res = await http.PostAsJsonAsync(url, body, ct);
            if (!res.IsSuccessStatusCode) return null;
            var node = await res.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct);
            return node?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}

public sealed class AiSettings
{
    /// <summary>"none", "ollama" or "gemini".</summary>
    public string Provider { get; set; } = "none";
    public string Model { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "http://localhost:11434";

    public IAiProvider Create(HttpClient http) => Provider.ToLowerInvariant() switch
    {
        "ollama" => new OllamaProvider(http, Model.Length > 0 ? Model : "llama3.1", BaseUrl),
        "gemini" => new GeminiProvider(http, ApiKey, Model.Length > 0 ? Model : "gemini-1.5-flash"),
        _ => new NoAiProvider(),
    };
}
