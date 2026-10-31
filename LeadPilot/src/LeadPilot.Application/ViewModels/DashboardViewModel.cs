using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;

namespace LeadPilot.Application.ViewModels;

/// <summary>KPI tiles and charts. Every figure is computed from the database on each visit.</summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IStatsRepository _stats;
    private readonly IClock _clock;
    private readonly INavigator _nav;
    private readonly ISettingsService _settings;

    [ObservableProperty] private bool _hasLeads;
    [ObservableProperty] private string _greeting = "";
    [ObservableProperty] private string _heroSubtitle = "";
    [ObservableProperty] private ChartData _industryChart = new();
    [ObservableProperty] private ChartData _locationChart = new();
    [ObservableProperty] private ChartData _sourceChart = new();
    [ObservableProperty] private ChartData _qualityChart = new();
    [ObservableProperty] private ChartData _monthlyChart = new();
    [ObservableProperty] private ChartData _pipelineChart = new();
    [ObservableProperty] private ChartData _conversionChart = new();

    public ObservableCollection<KpiCard> Kpis { get; } = new();

    public DashboardViewModel(IStatsRepository stats, IClock clock, INavigator nav, ISettingsService settings)
    { _stats = stats; _clock = clock; _nav = nav; _settings = settings; }

    public override Task OnNavigatedToAsync(object? parameter) => RefreshAsync();

    [RelayCommand]
    private Task Refresh() => RunAsync(async () =>
    {
        var s = await Task.Run(() => _stats.GetDashboard(_clock.UtcNow));
        Apply(s);
    });

    private Task RefreshAsync() => RefreshCommand.ExecuteAsync(null);

    /// <summary>Maps stats to tiles and charts (public for tests).</summary>
    public void Apply(DashboardStats s)
    {
        var name = _settings.Current.OwnerName;
        Greeting = string.IsNullOrWhiteSpace(name) ? "Welcome back" : $"Welcome back, {name.Split(' ')[0]}";
        HasLeads = s.TotalLeads > 0 || s.TotalDiscovered > 0;
        HeroSubtitle = !HasLeads ? "Let's find your first customers - it takes about a minute."
            : $"{s.HighPriorityLeads:N0} high-priority lead(s) · {s.PendingFollowUps:N0} open follow-up(s)" + (s.OverdueFollowUps > 0 ? $" · {s.OverdueFollowUps:N0} overdue" : "") + (s.DuplicateRecords > 0 ? $" · {s.DuplicateRecords:N0} duplicate pair(s) to review" : "");
        Kpis.Clear();
        void K(string t, int v, string hint, string accent) => Kpis.Add(new KpiCard(t, v.ToString("N0"), hint, accent));
        K("Prospects discovered", s.TotalDiscovered, "returned by discovery sources", "Blue");
        K("Leads stored", s.TotalLeads, "active leads in your database", "Blue");
        K("New leads", s.NewLeads, "not contacted yet", "Teal");
        K("Qualified leads", s.QualifiedLeads, "qualified, proposal or won", "Green");
        K("High-priority leads", s.HighPriorityLeads, "hot relevance, still open", "Orange");
        K("Duplicate records", s.DuplicateRecords, "pairs awaiting your review", s.DuplicateRecords > 0 ? "Amber" : "Gray");
        K("Incomplete records", s.IncompleteRecords, "missing phone, e-mail or website", s.IncompleteRecords > 0 ? "Amber" : "Gray");
        K("Pending follow-ups", s.PendingFollowUps, "open tasks", "Blue");
        K("Overdue follow-ups", s.OverdueFollowUps, "past their due date", s.OverdueFollowUps > 0 ? "Red" : "Gray");
        K("Converted leads", s.ConvertedLeads, "marked as won", "Green");
        IndustryChart = ChartData.From("Leads by industry", s.ByIndustry);
        LocationChart = ChartData.From("Leads by location", s.ByLocation);
        SourceChart = ChartData.From("Leads by source", s.BySource);
        QualityChart = ChartData.From("Lead quality (relevance)", s.Quality);
        MonthlyChart = ChartData.From("Leads added per month", s.MonthlyDiscovery);
        PipelineChart = ChartData.From("Sales pipeline", s.Pipeline);
        ConversionChart = ChartData.From("Leads won per month", s.ConversionTrend);
    }

    [RelayCommand] private void GoFinder() => _nav.Navigate(PageKey.LeadFinder);
    [RelayCommand] private void GoImport() => _nav.Navigate(PageKey.Import);
    [RelayCommand] private void GoDatabase() => _nav.Navigate(PageKey.LeadDatabase);
    [RelayCommand] private void GoFollowUps() => _nav.Navigate(PageKey.FollowUps);
    [RelayCommand] private void GoDuplicates() => _nav.Navigate(PageKey.Duplicates);
}
