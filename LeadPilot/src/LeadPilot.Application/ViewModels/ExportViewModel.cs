using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Domain;

namespace LeadPilot.Application.ViewModels;

/// <summary>Export the whole database or a slice to Excel, CSV or PDF.</summary>
public sealed partial class ExportViewModel : ViewModelBase
{
    private readonly ILeadRepository _leads;
    private readonly IExportService _export;
    private readonly IStatsRepository _stats;
    private readonly ISystemRepository _sys;
    private readonly ICurrentUser _user;
    private readonly IDialogService _dlg;
    private readonly IAppFolders _folders;
    private readonly IClock _clock;

    [ObservableProperty] private string _scope = "All leads";
    [ObservableProperty] private string _format = "Excel (.xlsx)";
    [ObservableProperty] private string _lastExport = "";

    public IReadOnlyList<string> Scopes { get; } = new[] { "All leads", "Hot leads", "Warm and hot leads", "Needs follow-up", "Incomplete records", "Won leads" };
    public IReadOnlyList<string> Formats { get; } = new[] { "Excel (.xlsx)", "CSV (.csv)", "PDF report (.pdf)" };

    public ExportViewModel(ILeadRepository leads, IExportService export, IStatsRepository stats, ISystemRepository sys, ICurrentUser user, IDialogService dlg, IAppFolders folders, IClock clock)
    { _leads = leads; _export = export; _stats = stats; _sys = sys; _user = user; _dlg = dlg; _folders = folders; _clock = clock; }

    private LeadQuery Query() => Scope switch
    {
        "Hot leads" => new LeadQuery { Temperature = LeadTemperature.Hot, Sort = LeadSort.Score },
        "Warm and hot leads" => new LeadQuery { MinScore = 50, Sort = LeadSort.Score },
        "Needs follow-up" => new LeadQuery { OnlyNeedsFollowUp = true, Sort = LeadSort.NextFollowUp, Descending = false },
        "Incomplete records" => new LeadQuery { OnlyIncomplete = true },
        "Won leads" => new LeadQuery { Status = LeadStatus.Won },
        _ => new LeadQuery { Sort = LeadSort.LeadId, Descending = false }
    };

    [RelayCommand]
    private Task Run() => RunAsync(async () =>
    {
        if (!_user.Can(Permission.Export)) throw new UnauthorizedAccessException("You do not have permission to export.");
        var ext = Format.Contains("csv") ? ".csv" : Format.Contains("pdf") ? ".pdf" : ".xlsx";
        Directory.CreateDirectory(_folders.Exports);
        var path = _dlg.PickSaveFile("Export leads", $"{Format}|*{ext}", $"leadpilot-{Scope.ToLowerInvariant().Replace(' ', '-')}-{_clock.UtcNow:yyyyMMdd}{ext}");
        if (path == null) return;
        var rows = await Task.Run(() => _leads.Export(Query()));
        await Task.Run(() =>
        {
            if (ext == ".csv") _export.ExportCsv(rows, path); else if (ext == ".pdf") _export.ExportPdf(rows, path, $"LeadPilot AI – {Scope}"); else _export.ExportXlsx(rows, path);
            _sys.AddAudit(new AuditEntry { At = _clock.UtcNow, UserId = _user.UserId, Action = "export", Entity = "Leads", Details = $"{rows.Count} lead(s) as {ext}" });
        });
        LastExport = $"Exported {rows.Count:N0} lead(s) to {path}";
        StatusMessage = LastExport;
    }, "Exporting…");

    [RelayCommand]
    private Task SummaryPdf() => RunAsync(async () =>
    {
        if (!_user.Can(Permission.Export)) throw new UnauthorizedAccessException("You do not have permission to export.");
        var path = _dlg.PickSaveFile("Save summary report", "PDF (*.pdf)|*.pdf", $"leadpilot-summary-{_clock.UtcNow:yyyyMMdd}.pdf");
        if (path == null) return;
        var s = await Task.Run(() => _stats.GetDashboard(_clock.UtcNow));
        var lines = new List<string>
        {
            $"Leads stored: {s.TotalLeads}   New: {s.NewLeads}   Qualified or further: {s.QualifiedLeads}   Won: {s.ConvertedLeads}",
            $"High-priority: {s.HighPriorityLeads}   Incomplete records: {s.IncompleteRecords}   Possible duplicates: {s.DuplicateRecords}",
            $"Open follow-ups: {s.PendingFollowUps}   Overdue: {s.OverdueFollowUps}   Prospects returned by discovery sources: {s.TotalDiscovered}", "",
            "Leads by industry:"
        };
        lines.AddRange(s.ByIndustry.Select(k => $"  {k.Key}: {k.Count}")); lines.Add(""); lines.Add("Leads by source:"); lines.AddRange(s.BySource.Select(k => $"  {k.Key}: {k.Count}"));
        lines.Add(""); lines.Add("Pipeline:"); lines.AddRange(s.Pipeline.Select(k => $"  {k.Key}: {k.Count}"));
        lines.Add(""); lines.Add("Relevance scores show how well a lead matches your target market. They are not evidence of buying intent.");
        await Task.Run(() => _export.ExportTextReportPdf("LeadPilot AI – summary report", lines, path));
        StatusMessage = "Summary saved to " + path;
    });

    [RelayCommand] private void OpenFolder() { Directory.CreateDirectory(_folders.Exports); _dlg.OpenUrl(_folders.Exports); }
}
