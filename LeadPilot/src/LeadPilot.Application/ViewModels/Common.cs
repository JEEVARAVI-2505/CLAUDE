using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace LeadPilot.Application.ViewModels;

/// <summary>Screens reachable from the sidebar (plus Setup, shown on first run).</summary>
public enum PageKey { Setup, Dashboard, LeadFinder, LeadDatabase, LeadDetails, Duplicates, Enrichment, Qualification, FollowUps, Campaigns, Analytics, Assistant, Import, Export, Integrations, Settings, Backup, About }

/// <summary>Navigation request from a page view-model.</summary>
public interface INavigator { void Navigate(PageKey key, object? parameter = null); }

/// <summary>Sidebar entry.</summary>
public sealed record NavItem(PageKey Key, string Title, string Glyph, string Section);

/// <summary>Base for all screens: busy flag, status/error text and a safe async runner.</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _errorMessage = "";

    /// <summary>True when there is an error to show.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    /// <summary>True when there is a status line to show.</summary>
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    /// <summary>Called when the page is shown. <paramref name="parameter"/> carries context such as a lead id.</summary>
    public virtual Task OnNavigatedToAsync(object? parameter) => Task.CompletedTask;

    /// <summary>Logger used by <see cref="RunAsync"/>; set by derived classes that have one.</summary>
    protected ILogger? Log { get; set; }

    /// <summary>Runs work with the busy flag set; exceptions become a readable error instead of crashing the app.</summary>
    protected async Task RunAsync(Func<Task> work, string? busyStatus = null)
    {
        if (IsBusy) return;
        IsBusy = true; ErrorMessage = "";
        if (busyStatus != null) StatusMessage = busyStatus;
        try { await work().ConfigureAwait(true); }
        catch (OperationCanceledException) { StatusMessage = "Cancelled."; }
        catch (UnauthorizedAccessException ex) { ErrorMessage = ex.Message; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Log?.LogError(ex, "Operation failed in {Page}", GetType().Name);
            ErrorMessage = Friendly(ex);
        }
        finally { IsBusy = false; }
    }

    /// <summary>Message safe to show a customer (no stack traces).</summary>
    public static string Friendly(Exception ex) => ex switch
    {
        ArgumentException or InvalidOperationException or NotSupportedException or InvalidDataException or FileNotFoundException => ex.Message,
        IOException => "A file could not be read or written: " + ex.Message,
        HttpRequestException h => "A network request failed: " + (h.StatusCode != null ? $"HTTP {(int)h.StatusCode}" : h.Message),
        _ => "Something went wrong: " + ex.Message + " (details were written to the log)."
    };
}

/// <summary>One bar in a chart.</summary>
public sealed record ChartBar(string Label, int Value, double Fraction);

/// <summary>A titled list of bars (rendered as horizontal bars or columns).</summary>
public sealed class ChartData
{
    public string Title { get; init; } = "";
    public IReadOnlyList<ChartBar> Bars { get; init; } = Array.Empty<ChartBar>();
    public bool IsEmpty => Bars.Count == 0 || Bars.All(b => b.Value == 0);

    public static ChartData From(string title, IEnumerable<Models.KeyCount> items)
    {
        var list = items.ToList(); var max = Math.Max(1, list.Count == 0 ? 1 : list.Max(i => i.Count));
        return new ChartData { Title = title, Bars = list.Select(i => new ChartBar(i.Key, i.Count, i.Count / (double)max)).ToList() };
    }
}

/// <summary>KPI tile.</summary>
public sealed record KpiCard(string Title, string Value, string Hint, string Accent);

/// <summary>Convenience for combo boxes: an "All" option followed by enum names.</summary>
public static class FilterOptionsHelper
{
    public const string All = "All";
    public static List<string> WithAll<T>() where T : struct, Enum => new[] { All }.Concat(Enum.GetNames<T>()).ToList();
    public static T? Parse<T>(string? s) where T : struct, Enum => s != null && s != All && Enum.TryParse<T>(s, out var v) ? v : null;
}
