using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

public sealed record CampaignRow(Campaign Campaign, int LeadCount)
{
    public string Name => Campaign.Name;
    public string Target => $"{Campaign.TargetCustomer} · {Campaign.Location}";
    public string Status => Campaign.Status.ToString();
    public string Created => Campaign.CreatedAt.ToLocalTime().ToString("dd MMM yyyy");
}

/// <summary>Campaigns group leads found for one product and market.</summary>
public sealed partial class CampaignViewModel : ViewModelBase
{
    private readonly IWorkRepository _work;
    private readonly ICurrentUser _user;
    private readonly INavigator _nav;
    private readonly IDialogService _dlg;

    [ObservableProperty] private CampaignRow? _selected;
    [ObservableProperty] private int _editId;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _product = "";
    [ObservableProperty] private string _targetCustomer = "";
    [ObservableProperty] private string _location = "";
    [ObservableProperty] private int _targetCount = 50;
    [ObservableProperty] private string _status = nameof(CampaignStatus.Active);
    [ObservableProperty] private string _notes = "";

    public ObservableCollection<CampaignRow> Campaigns { get; } = new();
    public IReadOnlyList<string> StatusChoices { get; } = Enum.GetNames<CampaignStatus>();
    public bool IsEmpty => Campaigns.Count == 0;

    public CampaignViewModel(IWorkRepository work, ICurrentUser user, INavigator nav, IDialogService dlg) { _work = work; _user = user; _nav = nav; _dlg = dlg; }

    public override Task OnNavigatedToAsync(object? parameter) => RefreshCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task Refresh() => RunAsync(async () =>
    {
        var rows = await Task.Run(() => _work.GetCampaigns().Select(c => new CampaignRow(c, _work.LeadCountForCampaign(c.Id))).ToList());
        Campaigns.Clear(); foreach (var r in rows) Campaigns.Add(r);
        OnPropertyChanged(nameof(IsEmpty));
    });

    partial void OnSelectedChanged(CampaignRow? value)
    {
        if (value == null) return;
        var c = value.Campaign;
        EditId = c.Id; Name = c.Name; Product = c.Product; TargetCustomer = c.TargetCustomer; Location = c.Location; TargetCount = c.TargetCount; Status = c.Status.ToString(); Notes = c.Notes ?? "";
    }

    [RelayCommand] private void New() { Selected = null; EditId = 0; Name = Product = TargetCustomer = Location = Notes = ""; TargetCount = 50; Status = nameof(CampaignStatus.Active); }

    [RelayCommand]
    private Task Save() => RunAsync(async () =>
    {
        if (!_user.Can(Permission.EditLeads)) throw new UnauthorizedAccessException("You do not have permission to edit campaigns.");
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Enter a campaign name."; return; }
        var c = new Campaign { Id = EditId, Name = Name.Trim(), Product = Product.Trim(), TargetCustomer = TargetCustomer.Trim(), Location = Location.Trim(), TargetCount = TargetCount, Status = Enum.Parse<CampaignStatus>(Status), Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim() };
        await Task.Run(() => { if (c.Id == 0) _work.AddCampaign(c); else _work.UpdateCampaign(c); });
        EditId = c.Id; StatusMessage = "Campaign saved.";
        var rows = await Task.Run(() => _work.GetCampaigns().Select(x => new CampaignRow(x, _work.LeadCountForCampaign(x.Id))).ToList());
        Campaigns.Clear(); foreach (var r in rows) Campaigns.Add(r);
        OnPropertyChanged(nameof(IsEmpty));
    });

    [RelayCommand]
    private Task Delete() => RunAsync(async () =>
    {
        if (Selected == null) return;
        if (!_user.Can(Permission.DeleteLeads)) throw new UnauthorizedAccessException("You do not have permission to delete campaigns.");
        if (!_dlg.Confirm("Delete campaign", "Delete this campaign? Its leads are kept, just no longer grouped.")) return;
        var id = Selected.Campaign.Id;
        await Task.Run(() => _work.DeleteCampaign(id));
        var rows = await Task.Run(() => _work.GetCampaigns().Select(x => new CampaignRow(x, _work.LeadCountForCampaign(x.Id))).ToList());
        Campaigns.Clear(); foreach (var r in rows) Campaigns.Add(r);
        New(); OnPropertyChanged(nameof(IsEmpty));
    });

    [RelayCommand] private void ViewLeads() { if (Selected != null) _nav.Navigate(PageKey.LeadDatabase, Selected.Campaign.Id); }
    [RelayCommand] private void FindMore()
    {
        if (Selected == null) return;
        _nav.Navigate(PageKey.LeadFinder, new Models.DiscoveryRequest { Product = Selected.Campaign.Product, TargetCustomer = Selected.Campaign.TargetCustomer, Location = Selected.Campaign.Location, Count = Math.Max(10, Selected.Campaign.TargetCount) });
    }
}
