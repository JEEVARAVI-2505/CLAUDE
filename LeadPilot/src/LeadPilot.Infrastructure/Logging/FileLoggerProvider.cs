using Microsoft.Extensions.Logging;

namespace LeadPilot.Infrastructure.Logging;

/// <summary>Writes one log file per day under the Logs folder and deletes files older than 14 days. Never logs secrets or lead contents.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _folder;
    private readonly object _lock = new();

    public FileLoggerProvider(string folder)
    {
        _folder = folder; Directory.CreateDirectory(folder);
        try { foreach (var f in Directory.GetFiles(folder, "leadpilot-*.log").Where(f => File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddDays(-14))) File.Delete(f); }
        catch (IOException) { /* ignore */ }
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);
    public void Dispose() { }

    private void Write(string line)
    {
        lock (_lock)
        {
            try { File.AppendAllText(Path.Combine(_folder, $"leadpilot-{DateTime.UtcNow:yyyyMMdd}.log"), line + Environment.NewLine); }
            catch (IOException) { /* logging must never crash the app */ }
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _p; private readonly string _cat;
        public FileLogger(FileLoggerProvider p, string cat) { _p = p; _cat = cat; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            _p.Write($"{DateTime.UtcNow:O} [{logLevel}] {_cat}: {formatter(state, exception)}{(exception == null ? "" : " | " + exception.GetType().Name + ": " + exception.Message)}");
        }
    }
}
