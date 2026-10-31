using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Application.Services;

namespace LeadPilot.Application.ViewModels;

/// <summary>Scoring weights, thresholds and target profile, plus the ranked list with per-lead explanations.</summary>
public sealed partial class QualificationViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly ScoringService _scoring;
    private readonly ILeadRepository _leads;
    private readonly IStatsRepository _stats;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly INavigator _nav;

    [ObservableProperty] private double _wIndustry;
    [ObservableProperty] private double _wLocation;
    [ObservableProperty] private double _wSize;
    [ObservableProperty] private double _wProduct;
    [ObservableProperty] private double _wContact;
    [ObservableProperty] private double _wIntent;
    [ObservableProperty] private int _hot;
    [ObservableProperty] private int _warm;
    [ObservableProperty] private int _cold;
    [ObservableProperty] private string _product = "";
    [ObservableProperty] private string _targetCustomer = "";
    [ObservableProperty] private string _location = "";
    [ObservableProperty] private string _companySize = "";
    [ObservableProperty] private LeadRow? _selectedLead;
    [ObservableProperty] private string _selectedExplanation = "";
    [ObservableProperty] private ChartData _quality = new();

    public ObservableCollection<LeadRow> Top { get; } = new();
    public string Disclaimer => ScoreResult.Disclaimer;
    public double WeightTotal => WIndustry + WLocation + WSize + WProduct + WContact + WIntent;

    public QualificationViewModel(ISettingsService settings, ScoringService scoring, ILeadRepository leads, IStatsRepository stats, IClock clock, ICurrentUser user, INavigator nav)
    { _settings = settings; _scoring = scoring; _leads = leads; _stats = stats; _clock = clock; _user = user; _nav = nav; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        Load(_settings.Current.Scoring);
        var p = _scoring.GetActiveProfile();
        Product = p.Product; TargetCustomer = p.TargetCustomer; Location = p.Location; CompanySize = p.CompanySize ?? "";
        await RefreshCommand.ExecuteAsync(null);
    }

    private void Load(ScoringWeights w)
    {
        WIndustry = w.Industry; WLocation = w.Location; WSize = w.CompanySize; WProduct = w.ProductRelevance; WContact = w.ContactCompleteness; WIntent = w.IntentEvidence;
        Hot = w.HotThreshold; Warm = w.WarmThreshold; Cold = w.ColdThreshold;
    }

    partial void OnWIndustryChanged(double value) => OnPropertyChanged(nameof(WeightTotal));
    partial void OnWLocationChanged(double value) => OnPropertyChanged(nameof(WeightTotal));
    partial void OnWSizeChanged(double value) => OnPropertyChanged(nameof(WeightTotal));
    partial void OnWProductChanged(double value) => OnPropertyChanged(nameof(WeightTotal));
    partial void OnWContactChanged(double value) => OnPropertyChanged(nameof(WeightTotal));
    partial void OnWIntentChanged(double value) => OnPropertyChanged(nameof(WeightTotal));
    partial void OnSelectedLeadChanged(LeadRow? value) => SelectedExplanation = value == null ? "" : (_leads.GetLead(value.Id)?.ScoreExplanation ?? "");

    [RelayCommand]
    private Task Refresh() => RunAsync(async () =>
    {
        var (rows, stats) = await Task.Run(() => (_leads.Search(new LeadQuery { Sort = LeadSort.Score, PageSize = 100 }).Rows, _stats.GetDashboard(_clock.UtcNow)));
        Top.Clear(); foreach (var r in rows) Top.Add(r);
        Quality = ChartData.From("Quality distribution", stats.Quality);
        SelectedLead = Top.FirstOrDefault();
    });

    [RelayCommand]
    private Task SaveAndRescore() => RunAsync(async () =>
    {
        if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to change scoring.");
        if (WeightTotal <= 0) { ErrorMessage = "At least one weight must be above zero."; return; }
        if (!(Hot > Warm && Warm > Cold && Cold >= 0 && Hot <= 100)) { ErrorMessage = "Thresholds must satisfy Hot > Warm > Cold ≥ 0 and Hot ≤ 100."; return; }
        var s = _settings.Current;
        s.Scoring = new ScoringWeights { Industry = WIndustry, Location = WLocation, CompanySize = WSize, ProductRelevance = WProduct, ContactCompleteness = WContact, IntentEvidence = WIntent, HotThreshold = Hot, WarmThreshold = Warm, ColdThreshold = Cold };
        _settings.Save(s);
        _scoring.SetActiveProfile(new ScoringProfile { Product = Product.Trim(), TargetCustomer = TargetCustomer.Trim(), Location = Location.Trim(), CompanySize = string.IsNullOrWhiteSpace(CompanySize) ? null : CompanySize.Trim() });
        var n = await Task.Run(() => _scoring.RescoreAll());
        StatusMessage = $"Weights saved and {n} lead(s) re-scored.";
        var rows = await Task.Run(() => _leads.Search(new LeadQuery { Sort = LeadSort.Score, PageSize = 100 }).Rows);
        Top.Clear(); foreach (var r in rows) Top.Add(r);
        Quality = ChartData.From("Quality distribution", (await Task.Run(() => _stats.GetDashboard(_clock.UtcNow))).Quality);
        SelectedLead = Top.FirstOrDefault();
    }, "Re-scoring…");

    [RelayCommand] private void ResetDefaults() => Load(new ScoringWeights());
    [RelayCommand] private void OpenSelected() { if (SelectedLead != null) _nav.Navigate(PageKey.LeadDetails, SelectedLead.Id); }
}
