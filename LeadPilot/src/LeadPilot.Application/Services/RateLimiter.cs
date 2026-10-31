namespace LeadPilot.Application.Services;

/// <summary>Enforces a minimum interval between calls per key (e.g. per host or provider).</summary>
public sealed class RateLimiter
{
    private readonly Dictionary<string, DateTime> _next = new();
    private readonly object _lock = new();

    /// <summary>Waits until <paramref name="minInterval"/> has passed since the previous call for the same key.</summary>
    public async Task WaitAsync(string key, TimeSpan minInterval, CancellationToken ct)
    {
        if (minInterval <= TimeSpan.Zero) return;
        TimeSpan delay;
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var start = _next.TryGetValue(key, out var n) && n > now ? n : now;
            _next[key] = start + minInterval;
            delay = start - now;
        }
        if (delay > TimeSpan.Zero) await Task.Delay(delay, ct).ConfigureAwait(false);
    }
}
