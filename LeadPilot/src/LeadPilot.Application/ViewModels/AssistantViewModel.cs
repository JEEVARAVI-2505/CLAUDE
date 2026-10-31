using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;

namespace LeadPilot.Application.ViewModels;

/// <summary>One chat bubble.</summary>
public sealed class ChatMessage
{
    public bool IsUser { get; init; }
    public string Text { get; init; } = "";
    public IReadOnlyList<LeadRow> Leads { get; init; } = Array.Empty<LeadRow>();
    public DiscoveryRequest? Discovery { get; init; }
    public OutreachDraft? Draft { get; init; }
    public bool HasLeads => Leads.Count > 0;
    public bool HasDiscovery => Discovery != null;
    public bool HasDraft => Draft != null;
    public string DraftText => Draft == null ? "" : (Draft.Subject.Length > 0 ? "Subject: " + Draft.Subject + "\n\n" : "") + Draft.Body;
}

/// <summary>Assistant that answers from the local database. It never invents records; live searches go through discovery sources.</summary>
public sealed partial class AssistantViewModel : ViewModelBase
{
    private readonly AssistantService _assistant;
    private readonly ISettingsService _settings;
    private readonly ScoringService _scoring;
    private readonly IAiClient _ai;
    private readonly INavigator _nav;
    private readonly IDialogService _dlg;

    [ObservableProperty] private string _input = "";

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public IReadOnlyList<string> Examples { get; } = new[]
    {
        "Find manufacturing companies in Chennai", "Show my top 20 prospects", "Find companies with missing contact information", "Identify duplicate records",
        "Show leads that need follow-up", "Generate an introduction email", "Summarize my lead database", "Show the most successful lead source"
    };
    public string ModeText => _ai.IsAvailable ? $"Understanding extended by {_ai.Name}. Answers still come only from your data." : "Rule-based mode (no AI model needed). Answers come only from your data.";

    public AssistantViewModel(AssistantService assistant, ISettingsService settings, ScoringService scoring, IAiClient ai, INavigator nav, IDialogService dlg)
    { _assistant = assistant; _settings = settings; _scoring = scoring; _ai = ai; _nav = nav; _dlg = dlg; }

    public override Task OnNavigatedToAsync(object? parameter)
    {
        if (Messages.Count == 0) Messages.Add(new ChatMessage { Text = "Hi! " + AssistantService.HelpText });
        return Task.CompletedTask;
    }

    private SenderProfile Me() => new() { SenderName = _settings.Current.OwnerName, SenderCompany = _settings.Current.CompanyName, Offer = _scoring.GetActiveProfile().Product };

    [RelayCommand]
    private Task Send() => RunAsync(async () =>
    {
        var q = Input.Trim(); if (q.Length == 0) return;
        Input = "";
        Messages.Add(new ChatMessage { IsUser = true, Text = q });
        var r = await _assistant.AskAsync(q, Me());
        Messages.Add(new ChatMessage { Text = r.Text, Leads = r.Leads.Take(20).ToList(), Discovery = r.SuggestedDiscovery, Draft = r.Draft });
    });

    [RelayCommand] private Task Ask(string? example) { if (example == null) return Task.CompletedTask; Input = example; return Send(); }
    [RelayCommand] private void OpenLead(LeadRow? row) { if (row != null) _nav.Navigate(PageKey.LeadDetails, row.Id); }
    [RelayCommand] private void RunDiscovery(DiscoveryRequest? req) { if (req != null) _nav.Navigate(PageKey.LeadFinder, req); }
    [RelayCommand] private void Copy(string? text) { if (!string.IsNullOrEmpty(text)) _dlg.CopyToClipboard(text); }
    [RelayCommand] private void Clear() { Messages.Clear(); Messages.Add(new ChatMessage { Text = AssistantService.HelpText }); }
}
