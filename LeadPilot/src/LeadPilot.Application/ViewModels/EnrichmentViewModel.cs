using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;

namespace LeadPilot.Application.ViewModels;

/// <summary>A lead queued for enrichment.</summary>
public sealed partial class EnrichmentItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected = true;
    [ObservableProperty] private string _result = "";
    public LeadRow Row { get; init; } = new();
    public string Missing => string.Join(", ", new[] { string.IsNullOrWhiteSpace(Row.Phone) ? "phone" : null, string.IsNullOrWhiteSpace(Row.Email) ? "e-mail" : null, string.IsNullOrWhiteSpace(Row.Website) ? "website" : null }.Where(x => x != null));
}

/// <summary>Fills gaps from each company's own website and runs contact checks. Applies only evidence-backed values.</summary>
public sealed partial class EnrichmentViewModel : ViewModelBase
{
    private readonly ILeadRepository _leads;
    private readonly EnrichmentService _enrich;
    private readonly VerificationService _verify;
    private readonly IAiClient _ai;
    private readonly IWebsiteFinder _finder;
    private readonly ICurrentUser _user;

    [ObservableProperty] private string _progressText = "";

    public ObservableCollection<EnrichmentItem> Items { get; } = new();
    public string AiStatus => _ai.IsAvailable ? $"AI assistance: {_ai.Name}. AI only suggests summaries – it never changes contact data." : "AI assistance: off. Enrichment uses rules and each company's own website only.";
    public string FinderStatus => _finder.IsConfigured ? "Website search: on (Brave Search, your key)." : "Website search: off – leads without a website cannot be enriched until you add one or set a search key in API Integrations.";
    public bool IsEmpty => Items.Count == 0;

    public EnrichmentViewModel(ILeadRepository leads, EnrichmentService enrich, VerificationService verify, IAiClient ai, IWebsiteFinder finder, ICurrentUser user)
    { _leads = leads; _enrich = enrich; _verify = verify; _ai = ai; _finder = finder; _user = user; }

    public override Task OnNavigatedToAsync(object? parameter) => LoadCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task Load() => RunAsync(async () =>
    {
        var rows = await Task.Run(() => _leads.Search(new LeadQuery { OnlyIncomplete = true, PageSize = 500, Sort = LeadSort.Score }).Rows);
        Items.Clear(); foreach (var r in rows) Items.Add(new EnrichmentItem { Row = r });
        OnPropertyChanged(nameof(IsEmpty)); OnPropertyChanged(nameof(AiStatus)); OnPropertyChanged(nameof(FinderStatus));
        StatusMessage = rows.Count == 0 ? "Every lead has a phone, e-mail and website." : $"{rows.Count} lead(s) have missing contact information.";
    });

    [RelayCommand] private void SelectAll() { foreach (var i in Items) i.IsSelected = true; }
    [RelayCommand] private void SelectNone() { foreach (var i in Items) i.IsSelected = false; }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EnrichSelectedAsync(CancellationToken ct)
    {
        if (!_user.Can(Permission.EditLeads)) { ErrorMessage = "You do not have permission to edit leads."; return; }
        var todo = Items.Where(i => i.IsSelected).ToList();
        if (todo.Count == 0) { ErrorMessage = "Select at least one lead."; return; }
        await RunAsync(async () =>
        {
            var n = 0; var filled = 0;
            foreach (var item in todo)
            {
                ct.ThrowIfCancellationRequested();
                ProgressText = $"Enriching {++n}/{todo.Count}: {item.Row.CompanyName}";
                var r = await _enrich.EnrichAsync(item.Row.Id, ct);
                item.Result = r.Summary + (r.Warnings.Count > 0 && r.Applied.Count > 0 ? $" ({r.Warnings[0]})" : "");
                if (r.Applied.Count > 0) filled++;
            }
            ProgressText = ""; StatusMessage = $"Done. {filled} of {todo.Count} lead(s) gained information. Every value is stored with its source page and time on the lead's Evidence tab.";
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifySelectedAsync(CancellationToken ct)
    {
        var todo = Items.Where(i => i.IsSelected).ToList();
        if (todo.Count == 0) { ErrorMessage = "Select at least one lead."; return; }
        await RunAsync(async () =>
        {
            var n = 0;
            foreach (var item in todo)
            {
                ct.ThrowIfCancellationRequested();
                ProgressText = $"Checking {++n}/{todo.Count}: {item.Row.CompanyName}";
                var r = await _verify.VerifyLeadAsync(item.Row.Id, true, ct);
                item.Result = r == null ? "" : string.Join(" ", r.Notes);
            }
            ProgressText = ""; StatusMessage = "Checks finished. 'Mail server found' means the domain can receive mail – it does not prove the mailbox exists.";
        });
    }
}
