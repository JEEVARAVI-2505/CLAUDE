using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LeadPilot.Application.Abstractions;

namespace LeadPilot.AI;

/// <summary>Used when AI is off. Every feature has a rule-based path, so this simply reports "not available".</summary>
public sealed class NullAiClient : IAiClient
{
    public bool IsAvailable => false;
    public string Name => "Off (rule-based)";
    public Task<string?> CompleteAsync(string system, string prompt, CancellationToken ct) => Task.FromResult<string?>(null);
}

/// <summary>Local model through an Ollama server (default http://localhost:11434). Nothing leaves the computer.</summary>
public sealed class OllamaClient : IAiClient
{
    private readonly HttpClient _http; private readonly string _endpoint; private readonly string _model;
    public OllamaClient(HttpClient http, string? endpoint, string? model)
    { _http = http; _endpoint = (string.IsNullOrWhiteSpace(endpoint) ? "http://localhost:11434" : endpoint!).TrimEnd('/'); _model = string.IsNullOrWhiteSpace(model) ? "llama3.2" : model!; }

    public bool IsAvailable => true;
    public string Name => $"Ollama ({_model}, local)";

    public async Task<string?> CompleteAsync(string system, string prompt, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct); cts.CancelAfter(TimeSpan.FromSeconds(90));
        var body = new { model = _model, stream = false, messages = new object[] { new { role = "system", content = system }, new { role = "user", content = prompt } }, options = new { temperature = 0.2 } };
        using var resp = await _http.PostAsJsonAsync(_endpoint + "/api/chat", body, cts.Token).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"Ollama answered HTTP {(int)resp.StatusCode}", null, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false));
        return doc.RootElement.TryGetProperty("message", out var m) && m.TryGetProperty("content", out var c) ? c.GetString() : null;
    }
}

/// <summary>Any OpenAI-compatible chat endpoint with the customer's own API key. Prompts (which can contain lead text) are sent to that provider.</summary>
public sealed class OpenAiCompatibleClient : IAiClient
{
    private readonly HttpClient _http; private readonly string _endpoint; private readonly string _model; private readonly string _key;
    public OpenAiCompatibleClient(HttpClient http, string? endpoint, string? model, string key)
    { _http = http; _endpoint = (string.IsNullOrWhiteSpace(endpoint) ? "https://api.openai.com/v1" : endpoint!).TrimEnd('/'); _model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model!; _key = key; }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_key);
    public string Name => $"{new Uri(_endpoint).Host} ({_model}, your API key)";

    public async Task<string?> CompleteAsync(string system, string prompt, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct); cts.CancelAfter(TimeSpan.FromSeconds(60));
        using var req = new HttpRequestMessage(HttpMethod.Post, _endpoint + "/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { model = _model, temperature = 0.2, messages = new object[] { new { role = "system", content = system }, new { role = "user", content = prompt } } }), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _key);
        using var resp = await _http.SendAsync(req, cts.Token).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"AI provider answered HTTP {(int)resp.StatusCode}", null, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false));
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }
}

/// <summary>Chooses the configured client at call time so Settings changes apply without a restart.</summary>
public sealed class SwitchableAiClient : IAiClient
{
    private readonly ISettingsService _settings; private readonly ISecretStore _secrets; private readonly HttpClient _http;
    public SwitchableAiClient(ISettingsService settings, ISecretStore secrets, HttpClient http) { _settings = settings; _secrets = secrets; _http = http; }

    private IAiClient Resolve()
    {
        var s = _settings.Current;
        return s.AiProvider switch
        {
            "Ollama" => new OllamaClient(_http, s.AiEndpoint, s.AiModel),
            "OpenAI-compatible" when !string.IsNullOrWhiteSpace(_secrets.Get("ai:key")) => new OpenAiCompatibleClient(_http, s.AiEndpoint, s.AiModel, _secrets.Get("ai:key")!),
            _ => new NullAiClient()
        };
    }

    public bool IsAvailable => Resolve().IsAvailable;
    public string Name => Resolve().Name;
    public Task<string?> CompleteAsync(string system, string prompt, CancellationToken ct) => Resolve().CompleteAsync(system, prompt, ct);
}
