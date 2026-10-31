using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Duplicates;
using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Application.Services;
using LeadPilot.Application.Text;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

/// <summary>A discovered prospect awaiting the user's decision to save it.</summary>
public sealed partial class DiscoveryItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    public ProspectCandidate Candidate { get; init; } = new();
    public int Score { get; init; }
    public LeadTemperature Temperature { get; init; }
    public string Explanation { get; init; } = "";
    public string? KnownAs { get; init; }
    public string Name => Candidate.Name;
    public string Phone => Candidate.Phone ?? "";
    public string Email => Candidate.Email ?? "";
    public string Website => Candidate.Website ?? "";
    public string Industry => Candidate.Industry ?? "";
    public string Location => string.Join(", ", new[] { Candidate.City, Candidate.Region }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public string Source => Candidate.SourceName;
    public string SourceUrl => Candidate.SourceUrl ?? "";
    public bool IsKnown => KnownAs != null;
}

/// <summary>"Enter your market → Find leads": four required inputs, optional filters, live discovery and save.</summary>
public sealed partial class LeadFinderViewModel : ViewModelBase
{
    private readonly DiscoveryOrchestrator _orch;
    private readonly LeadIngestService _ingest;
    private readonly ScoringService _scoring;
    private readonly ILeadRepository _leads;
    private readonly IWorkRepository _work;
    private readonly ISettingsService _settings;
    private readonly ICurrentUser _user;
    private readonly INavigator _nav;

    [ObservableProperty] private string _product = "";
    [ObservableProperty] private string _targetCustomer = "";
    [ObservableProperty] private string _location = "";
    [ObservableProperty] private int _count = 25;
    [ObservableProperty] private bool _showAdvanced;
    [ObservableProperty] private string _industry = "";
    [ObservableProperty] private string _companySize = "";
    [ObservableProperty] private string _businessCategory = "";
    [ObservableProperty] private string _jobTitles = "";
    [ObservableProperty] private bool _requireWebsite;
    [ObservableProperty] private string _excludedCompanies = "";
    [ObservableProperty] private int _minScore;
    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private bool _hasRun;
    [ObservableProperty] private string _summary = "";
    [ObservableProperty] private string _saveSummary = "";
    [ObservableProperty] private int _selectedCount;

    public ObservableCollection<DiscoveryItem> Results { get; } = new();
    public ObservableCollection<ProviderRunResult> Providers { get; } = new();
    public ObservableCollection<string> Notes { get; } = new();
    public bool HasResults => Results.Count > 0;
    public bool NoResultsAfterRun => HasRun && Results.Count == 0;

    public LeadFinderViewModel(DiscoveryOrchestrator orch, LeadIngestService ingest, ScoringService scoring, ILeadRepository leads, IWorkRepository work, ISettingsService settings, ICurrentUser user, INavigator nav)
    { _orch = orch; _ingest = ingest; _scoring = scoring; _leads = leads; _work = work; _settings = settings; _user = user; _nav = nav; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        if (_scoring.GetActiveProfile() is { } p && string.IsNullOrWhiteSpace(Product) && string.IsNullOrWhiteSpace(TargetCustomer))
        { Product = p.Product; TargetCustomer = p.TargetCustomer; Location = p.Location; }
        if (parameter is DiscoveryRequest r)
        {
            if (!string.IsNullOrWhiteSpace(r.Product)) Product = r.Product;
            TargetCustomer = r.TargetCustomer; Location = r.Location; Count = r.Count;
            if (string.IsNullOrWhiteSpace(Product)) ErrorMessage = "Enter the product or service you offer, then press Find leads.";
            else await FindLeadsCommand.ExecuteAsync(null);
        }
    }

    partial void OnSelectedCountChanged(int value) => OnPropertyChanged(nameof(CanSave));
    public bool CanSave => SelectedCount > 0;

    private DiscoveryRequest BuildRequest() => new()
    {
        Product = Product.Trim(), TargetCustomer = TargetCustomer.Trim(), Location = Location.Trim(), Count = Count,
        Industry = Blank(Industry), CompanySize = Blank(CompanySize), BusinessCategory = Blank(BusinessCategory), JobTitles = Blank(JobTitles), RequireWebsite = RequireWebsite, MinScore = MinScore,
        ExcludedCompanies = ExcludedCompanies.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
    };

    private static string? Blank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task FindLeadsAsync(CancellationToken ct)
    {
        if (!_user.Can(Permission.RunDiscovery)) { ErrorMessage = "Your role cannot run discovery."; return; }
        var req = BuildRequest();
        if (req.Validate() is { } bad) { ErrorMessage = bad; return; }
        await RunAsync(async () =>
        {
            Results.Clear(); Providers.Clear(); Notes.Clear(); SaveSummary = ""; Summary = ""; HasRun = false;
            _scoring.SetActiveProfile(new ScoringProfile { Product = req.Product, TargetCustomer = req.TargetCustomer, Location = req.Location, Industry = req.Industry, CompanySize = req.CompanySize });
            var progress = new Progress<string>(m => ProgressText = m);
            var run = await _orch.RunAsync(req, progress, ct);
            var profile = _scoring.GetActiveProfile();
            var known = new DuplicateIndex(_leads.GetMatchProfiles());
            var region = _settings.Current.DefaultRegion;
            var items = new List<DiscoveryItem>();
            foreach (var c in run.Candidates)
            {
                var sc = LeadScorer.Score(profile, FactsOf(c, region), _settings.Current.Scoring);
                if (sc.Score < req.MinScore) continue;
                var probe = MatchProfile.From(0, "", c.Name, c.Website, new[] { c.Phone }, new[] { c.Email }, c.Address, c.City, region);
                var dup = known.Find(probe).FirstOrDefault(m => m.Confidence >= DuplicateConfidence.Medium);
                items.Add(new DiscoveryItem { Candidate = c, Score = sc.Score, Temperature = sc.Temperature, Explanation = sc.Explain(), KnownAs = dup == null ? null : $"Looks like a lead you already have ({dup.Reasons.FirstOrDefault()})", IsSelected = dup == null });
            }
            foreach (var i in items.OrderByDescending(i => i.Score).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
            {
                i.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(DiscoveryItem.IsSelected)) UpdateSelected(); };
                Results.Add(i);
            }
            foreach (var p in run.Providers) { Providers.Add(p); foreach (var w in p.Warnings) Notes.Add($"{p.DisplayName}: {w}"); }
            HasRun = true; UpdateSelected();
            OnPropertyChanged(nameof(HasResults)); OnPropertyChanged(nameof(NoResultsAfterRun));
            Summary = run.Providers.Count == 0 ? "No discovery source is enabled. Turn one on in API Integrations."
                : !run.AnyProviderSucceeded ? "No source could be reached: " + string.Join("; ", run.Providers.Select(p => $"{p.DisplayName} – {p.Error}")) + ". Nothing was added. You can still import files or add leads manually."
                : Results.Count == 0 ? "The sources answered but returned no matching prospects. Try a broader customer type or a larger area." 
                : $"Found {Results.Count} prospect(s). Review them, then save the ones you want. Nothing is stored until you press Save.";
            ProgressText = "";
        }, "Searching…");
    }

    private void UpdateSelected() { SelectedCount = Results.Count(r => r.IsSelected); OnPropertyChanged(nameof(HasResults)); }

    private static LeadFacts FactsOf(ProspectCandidate c, string region) => new()
    {
        Name = c.Name, Industry = c.Industry, Services = c.Services, Description = c.Description, Address = c.Address, City = c.City, Region = c.Region, Country = c.Country, CompanySize = c.CompanySize,
        HasValidPhone = Normalizer.ParsePhone(c.Phone, region).IsValid, HasValidEmail = Normalizer.IsValidEmailFormat(c.Email), HasWebsite = Normalizer.NormalizeUrl(c.Website) != null,
        HasContactPerson = !string.IsNullOrWhiteSpace(c.ContactName), IntentEvidence = c.IntentEvidence
    };

    [RelayCommand] private void SelectAll() { foreach (var r in Results) r.IsSelected = true; }
    [RelayCommand] private void SelectNone() { foreach (var r in Results) r.IsSelected = false; }
    [RelayCommand] private void ToggleAdvanced() => ShowAdvanced = !ShowAdvanced;

    [RelayCommand]
    private Task SaveSelected() => RunAsync(async () =>
    {
        var chosen = Results.Where(r => r.IsSelected).ToList();
        if (chosen.Count == 0) { ErrorMessage = "Select at least one prospect."; return; }
        var req = BuildRequest();
        var outcomes = await Task.Run(() =>
        {
            var cmp = _work.AddCampaign(new Campaign { Name = $"{req.Location} – {req.TargetCustomer} ({DateTime.Now:d MMM yyyy})", Product = req.Product, TargetCustomer = req.TargetCustomer, Location = req.Location, TargetCount = req.Count, Status = CampaignStatus.Active });
            var ctx = new IngestContext { CampaignId = cmp, UserId = _user.UserId, AssignedUserId = _user.UserId, Profile = _scoring.GetActiveProfile() };
            return _ingest.IngestMany(chosen.Select(c => c.Candidate), ctx);
        });
        var created = outcomes.Count(o => o.Status == IngestStatus.Created);
        var linked = outcomes.Count(o => o.Status == IngestStatus.DuplicateLinked);
        var skipped = outcomes.Where(o => o.Status is IngestStatus.Skipped or IngestStatus.Invalid).ToList();
        SaveSummary = $"Saved {created} new lead(s)." + (linked > 0 ? $" {linked} were already in your database (source added)." : "") + (skipped.Count > 0 ? $" {skipped.Count} not saved: {skipped[0].Message}" : "");
        foreach (var c in chosen) c.IsSelected = false;
    }, "Saving leads…");

    [RelayCommand] private void OpenDatabase() => _nav.Navigate(PageKey.LeadDatabase);
}
