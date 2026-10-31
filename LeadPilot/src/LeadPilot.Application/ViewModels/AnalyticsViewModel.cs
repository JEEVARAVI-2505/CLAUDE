using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;

namespace LeadPilot.Application.ViewModels;

public sealed record SourcePerfRow(string Source, int Leads, int Qualified, int Won, string AverageScore, string WinRate);
public sealed record UsageRow(string When, string Provider, string Operation, string Result, int Results);

/// <summary>Source performance, funnel and external-API usage – all from stored data.</summary>
public sealed partial class AnalyticsViewModel : ViewModelBase
{
    private readonly IStatsRepository _stats;
    private readonly ISystemRepository _sys;
    private readonly IClock _clock;

    [ObservableProperty] private ChartData _pipeline = new();
    [ObservableProperty] private ChartData _monthly = new();
    [ObservableProperty] private ChartData _conversion = new();
    [ObservableProperty] private ChartData _sources = new();
    [ObservableProperty] private string _headline = "";

    public ObservableCollection<SourcePerfRow> SourceRows { get; } = new();
    public ObservableCollection<UsageRow> Usage { get; } = new();

    public AnalyticsViewModel(IStatsRepository stats, ISystemRepository sys, IClock clock) { _stats = stats; _sys = sys; _clock = clock; }

    public override Task OnNavigatedToAsync(object? parameter) => RefreshCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task Refresh() => RunAsync(async () =>
    {
        var (s, perf, usage) = await Task.Run(() => (_stats.GetDashboard(_clock.UtcNow), _stats.GetSourcePerformance(), _sys.GetApiUsage(50)));
        Pipeline = ChartData.From("Sales pipeline", s.Pipeline); Monthly = ChartData.From("Leads added per month", s.MonthlyDiscovery);
        Conversion = ChartData.From("Leads won per month", s.ConversionTrend); Sources = ChartData.From("Leads by source", s.BySource);
        var rate = s.TotalLeads == 0 ? 0 : s.ConvertedLeads / (double)s.TotalLeads;
        Headline = s.TotalLeads == 0 ? "No leads yet." : $"{s.TotalLeads:N0} leads · {s.QualifiedLeads:N0} qualified or further · {s.ConvertedLeads:N0} won ({rate:P1} of all leads)";
        SourceRows.Clear();
        foreach (var p in perf) SourceRows.Add(new SourcePerfRow(p.Source, p.Leads, p.Qualified, p.Won, p.AverageScore.ToString("0.#"), p.Leads == 0 ? "–" : $"{p.Won / (double)p.Leads:P0}"));
        Usage.Clear();
        foreach (var u in usage) Usage.Add(new UsageRow(u.At.ToLocalTime().ToString("dd MMM HH:mm"), u.Provider, u.Operation, u.Success ? "OK" : "Failed: " + u.ErrorMessage, u.ResultCount));
    });
}
