using System.Text.RegularExpressions;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Domain;

namespace LeadPilot.Application.Services;

public enum AssistantIntent { FindCompanies, TopProspects, MissingContact, Duplicates, FollowUps, DraftEmail, Summary, BestSource, Help }

/// <summary>Assistant answer. Everything shown comes from the database; <see cref="SuggestedDiscovery"/> lets the UI start a real provider search.</summary>
public sealed class AssistantReply
{
    public AssistantIntent Intent { get; init; }
    public string Text { get; init; } = "";
    public IReadOnlyList<LeadRow> Leads { get; init; } = Array.Empty<LeadRow>();
    public DiscoveryRequest? SuggestedDiscovery { get; init; }
    public OutreachDraft? Draft { get; init; }
}

/// <summary>Command-style assistant. Intents are recognised by rules (works offline); an optional AI model can classify unrecognised text but never produces data.</summary>
public sealed partial class AssistantService
{
    private readonly ILeadRepository _leads;
    private readonly IWorkRepository _work;
    private readonly IDuplicateRepository _dups;
    private readonly IStatsRepository _stats;
    private readonly OutreachDrafter _drafter;
    private readonly ISettingsService _settings;
    private readonly IAiClient _ai;
    private readonly IClock _clock;

    public AssistantService(ILeadRepository leads, IWorkRepository work, IDuplicateRepository dups, IStatsRepository stats, OutreachDrafter drafter, ISettingsService settings, IAiClient ai, IClock clock)
    { _leads = leads; _work = work; _dups = dups; _stats = stats; _drafter = drafter; _settings = settings; _ai = ai; _clock = clock; }

    public const string HelpText = "I can: find companies (\"Find manufacturing companies in Chennai\"), show top prospects (\"Show my top 20 prospects\"), list leads with missing contact information, identify duplicate records, show leads that need follow-up, generate an introduction email (\"Generate an introduction email for LD-000012\"), summarise your lead database, and show your most successful lead source.";

    [GeneratedRegex(@"\bLD-\d{3,}\b", RegexOptions.IgnoreCase)] private static partial Regex LeadCodeRx();
    [GeneratedRegex(@"\btop\s+(\d{1,3})\b", RegexOptions.IgnoreCase)] private static partial Regex TopRx();
    [GeneratedRegex(@"\b(?:find|search(?: for)?|discover|get me|look for)\s+(?:me\s+)?(?:some\s+)?(?<what>.+?)\s+(?:in|near|around|at)\s+(?<where>[\p{L}][\p{L} .'-]*)$", RegexOptions.IgnoreCase)] private static partial Regex FindRx();

    public static AssistantIntent Classify(string q)
    {
        var s = q.ToLowerInvariant();
        if (s.Contains("duplicate")) return AssistantIntent.Duplicates;
        if (s.Contains("follow") && (s.Contains("up") || s.Contains("need"))) return AssistantIntent.FollowUps;
        if (s.Contains("missing") && (s.Contains("contact") || s.Contains("email") || s.Contains("phone") || s.Contains("information"))) return AssistantIntent.MissingContact;
        if ((s.Contains("email") || s.Contains("whatsapp") || s.Contains("message")) && (s.Contains("generate") || s.Contains("write") || s.Contains("draft") || s.Contains("introduction") || s.Contains("intro"))) return AssistantIntent.DraftEmail;
        if (s.Contains("source") && (s.Contains("success") || s.Contains("best") || s.Contains("top") || s.Contains("most"))) return AssistantIntent.BestSource;
        if (s.Contains("summar") || s.Contains("overview") || s.Contains("how many leads")) return AssistantIntent.Summary;
        if (s.Contains("prospect") && (s.Contains("top") || s.Contains("best") || s.Contains("show")) || s.Contains("top ") && s.Contains("lead")) return AssistantIntent.TopProspects;
        if (FindRx().IsMatch(q.Trim().TrimEnd('.', '?', '!'))) return AssistantIntent.FindCompanies;
        return AssistantIntent.Help;
    }

    public async Task<AssistantReply> AskAsync(string question, SenderProfile me, CancellationToken ct = default)
    {
        var intent = Classify(question);
        if (intent == AssistantIntent.Help && _ai.IsAvailable) intent = await ClassifyWithAiAsync(question, ct).ConfigureAwait(false);
        switch (intent)
        {
            case AssistantIntent.TopProspects:
            {
                var n = TopRx().Match(question) is { Success: true } m ? int.Parse(m.Groups[1].Value) : 10;
                var rows = _leads.Search(new LeadQuery { Sort = LeadSort.Score, Descending = true, PageSize = Math.Clamp(n, 1, 100) }).Rows;
                return new() { Intent = intent, Leads = rows, Text = rows.Count == 0 ? "You have no leads yet. Use AI Lead Finder or the Import Centre to add some." : $"Your top {rows.Count} lead(s) by relevance score. Scores show fit with your target market, not buying intent." };
            }
            case AssistantIntent.MissingContact:
            {
                var rows = _leads.Search(new LeadQuery { OnlyIncomplete = true, PageSize = 50, Sort = LeadSort.Score }).Rows;
                return new() { Intent = intent, Leads = rows, Text = rows.Count == 0 ? "Every lead has a phone, e-mail and website." : $"{rows.Count} lead(s) are missing a phone, e-mail or website. Run AI Enrichment to try the companies' own websites." };
            }
            case AssistantIntent.Duplicates:
            {
                var d = _dups.GetMatches(DuplicateDecision.Pending);
                var text = d.Count == 0 ? "No pending duplicates. Use Duplicate Manager → Scan to re-check." : $"{d.Count} possible duplicate pair(s):\n" + string.Join('\n', d.Take(10).Select(x => $"• {x.A.LeadCode} {x.A.CompanyName} ↔ {x.B.LeadCode} {x.B.CompanyName} ({x.Match.Confidence}, {x.Match.Score:P0}: {x.Match.Reasons})"));
                return new() { Intent = intent, Text = text, Leads = d.Take(10).SelectMany(x => new[] { x.A, x.B }).ToList() };
            }
            case AssistantIntent.FollowUps:
            {
                var rows = _leads.Search(new LeadQuery { OnlyNeedsFollowUp = true, Sort = LeadSort.NextFollowUp, Descending = false, PageSize = 50 }).Rows;
                return new() { Intent = intent, Leads = rows, Text = rows.Count == 0 ? "Nothing is due for follow-up." : $"{rows.Count} lead(s) have a follow-up due or overdue." };
            }
            case AssistantIntent.Summary:
            {
                var s = _stats.GetDashboard(_clock.UtcNow);
                return new() { Intent = intent, Text = $"You have {s.TotalLeads} lead(s): {s.NewLeads} new, {s.QualifiedLeads} qualified or further, {s.HighPriorityLeads} high priority, {s.ConvertedLeads} won. {s.IncompleteRecords} record(s) are incomplete, {s.DuplicateRecords} possible duplicate pair(s), {s.PendingFollowUps} open follow-up(s) of which {s.OverdueFollowUps} overdue. {s.TotalDiscovered} prospect(s) were returned by discovery sources so far." };
            }
            case AssistantIntent.BestSource:
            {
                var perf = _stats.GetSourcePerformance();
                if (perf.Count == 0) return new() { Intent = intent, Text = "No leads yet, so no source statistics." };
                var best = perf[0];
                var basis = best.Won > 0 ? $"{best.Won} won" : best.Qualified > 0 ? $"{best.Qualified} qualified" : $"{best.Leads} lead(s) (nothing qualified or won yet)";
                return new() { Intent = intent, Text = $"Best source so far: {best.Source} – {basis}, average score {best.AverageScore:0.#}.\n" + string.Join('\n', perf.Take(5).Select(p => $"• {p.Source}: {p.Leads} leads, {p.Qualified} qualified+, {p.Won} won, avg score {p.AverageScore:0.#}")) };
            }
            case AssistantIntent.DraftEmail:
            {
                var code = LeadCodeRx().Match(question);
                LeadRow? row;
                if (code.Success) row = _leads.Search(new LeadQuery { Text = code.Value.ToUpperInvariant(), PageSize = 1 }).Rows.FirstOrDefault();
                else row = _leads.Search(new LeadQuery { Sort = LeadSort.Score, PageSize = 1 }).Rows.FirstOrDefault();
                if (row == null) return new() { Intent = intent, Text = code.Success ? $"I couldn't find {code.Value}." : "You have no leads to write to yet." };
                var detail = _leads.GetDetail(row.Id)!;
                var channel = question.Contains("whatsapp", StringComparison.OrdinalIgnoreCase) ? OutreachChannel.WhatsApp : OutreachChannel.Email;
                var draft = await _drafter.DraftAsync(detail, me, channel, true, ct).ConfigureAwait(false);
                return new() { Intent = intent, Leads = new[] { row }, Draft = draft, Text = (code.Success ? "" : $"No lead was named, so I used your top-scoring lead ({row.LeadCode} {row.CompanyName}).\n") + "Draft ready. It has not been sent; review it and send it from your own e-mail/WhatsApp." };
            }
            case AssistantIntent.FindCompanies:
            {
                var m = FindRx().Match(question.Trim().TrimEnd('.', '?', '!'));
                var what = m.Groups["what"].Value.Trim(); var where = m.Groups["where"].Value.Trim();
                var cleaned = Regex.Replace(what, @"\b(companies|company|businesses|business|firms|prospects|leads|organi[sz]ations)\b", "", RegexOptions.IgnoreCase).Trim();
                var rows = _leads.Search(new LeadQuery { Text = where, Industry = null, PageSize = 200, Sort = LeadSort.Score }).Rows
                    .Where(r => cleaned.Length == 0 || (r.Industry ?? "").Contains(cleaned, StringComparison.OrdinalIgnoreCase) || r.CompanyName.Contains(cleaned, StringComparison.OrdinalIgnoreCase)).ToList();
                var req = new DiscoveryRequest { Product = me.Offer, TargetCustomer = string.IsNullOrWhiteSpace(cleaned) ? what : cleaned, Location = where, Count = 25 };
                return new()
                {
                    Intent = intent, Leads = rows, SuggestedDiscovery = req,
                    Text = (rows.Count == 0 ? $"Your database has no matching leads for \"{what}\" in {where}." : $"{rows.Count} matching lead(s) already in your database.") + " To search live sources for more, run discovery with the suggested settings (nothing is added until you save results)."
                };
            }
            default:
                return new() { Intent = AssistantIntent.Help, Text = HelpText };
        }
    }

    private async Task<AssistantIntent> ClassifyWithAiAsync(string question, CancellationToken ct)
    {
        try
        {
            var names = string.Join(", ", Enum.GetNames<AssistantIntent>());
            var r = await _ai.CompleteAsync($"Classify the user's request into exactly one of: {names}. Reply with only that word.", question, ct).ConfigureAwait(false);
            return Enum.TryParse<AssistantIntent>(r?.Trim().Trim('.', '"'), true, out var i) ? i : AssistantIntent.Help;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException) { return AssistantIntent.Help; }
    }
}
