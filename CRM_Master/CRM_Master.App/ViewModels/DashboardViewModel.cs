using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CRM_Master.App.Controls;
using CRM_Master.App.Services;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Core.Interfaces;

namespace CRM_Master.App.ViewModels;

/// <summary>One KPI tile. Kind: neutral | good | warn | bad.</summary>
public sealed record StatCard(string Title, string Value, string Hint, string Kind);

/// <summary>Dashboard: every number is read from the SQLite database each time the page opens.</summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboard;
    public DashboardViewModel(UiServices ui, IDashboardService dashboard) : base(ui) => _dashboard = dashboard;

    public ObservableCollection<StatCard> Cards { get; } = new();
    public ObservableCollection<TimePoint> Growth { get; } = new();
    public ObservableCollection<BarItem> Quality { get; } = new();
    public ObservableCollection<ImportJob> RecentImports { get; } = new();
    public ObservableCollection<AuditLog> RecentAutomation { get; } = new();
    public ObservableCollection<AuditLog> RecentErrors { get; } = new();

    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private bool _hasLoaded;

    public override Task ActivateAsync() => RefreshAsync();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        DashboardStats? s = null;
        await RunAsync("Loading dashboard", async () => s = await _dashboard.GetStatsAsync());
        if (s is null) return;

        IsEmpty = s.IsEmpty; HasLoaded = true;
        string N(int v) => v.ToString("N0");
        Cards.Clear();
        Cards.Add(new("Total contacts", N(s.TotalContacts), "In your local database", "neutral"));
        Cards.Add(new("New contacts imported", N(s.NewContactsImported), "Last 30 days", "good"));
        Cards.Add(new("Duplicate records", N(s.DuplicateRecords), "Open duplicate groups + flagged", s.DuplicateRecords > 0 ? "warn" : "good"));
        Cards.Add(new("Invalid email addresses", N(s.InvalidEmails), "Syntax problems only", s.InvalidEmails > 0 ? "bad" : "good"));
        Cards.Add(new("Invalid phone numbers", N(s.InvalidPhones), "Format problems only", s.InvalidPhones > 0 ? "bad" : "good"));
        Cards.Add(new("Incomplete records", N(s.IncompleteRecords), "Missing required fields", s.IncompleteRecords > 0 ? "warn" : "good"));
        Cards.Add(new("Updated records", N(s.UpdatedRecords), "Last 30 days", "neutral"));
        Cards.Add(new("Failed operations", N(s.FailedOperations), "Last 30 days", s.FailedOperations > 0 ? "bad" : "good"));

        Replace(Growth, s.Growth);
        Quality.Clear();
        Quality.Add(new("Clean records", s.CleanRecords, "good"));
        Quality.Add(new("Incomplete", s.IncompleteRecords, "warn"));
        Quality.Add(new("Invalid email", s.InvalidEmails, "bad"));
        Quality.Add(new("Invalid phone", s.InvalidPhones, "bad"));
        Quality.Add(new("Duplicates", s.DuplicateRecords, "warn"));
        Replace(RecentImports, s.RecentImports); Replace(RecentAutomation, s.RecentAutomation); Replace(RecentErrors, s.RecentErrors);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items) { target.Clear(); foreach (var i in items) target.Add(i); }

    [RelayCommand] private void Go(Module m) => Ui.Navigation.Navigate(m);
}
