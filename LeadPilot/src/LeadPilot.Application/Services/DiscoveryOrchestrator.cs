using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Duplicates;
using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Application.Text;
using LeadPilot.Domain;
using Microsoft.Extensions.Logging;

namespace LeadPilot.Application.Services;

/// <summary>Runs the enabled providers with paging, rate limits, retries and failure isolation. One failing provider never stops the others.</summary>
public sealed class DiscoveryOrchestrator
{
    private const int MaxAttempts = 3;
    private readonly IReadOnlyList<IDiscoveryProvider> _providers;
    private readonly ISecretStore _secrets;
    private readonly ISettingsService _settings;
    private readonly ISystemRepository _sys;
    private readonly IWebsiteExtractor _extractor;
    private readonly RateLimiter _limiter = new();
    private readonly ILogger<DiscoveryOrchestrator> _log;
    private readonly IClock _clock;

    public DiscoveryOrchestrator(IEnumerable<IDiscoveryProvider> providers, ISecretStore secrets, ISettingsService settings, ISystemRepository sys, IWebsiteExtractor extractor, ILogger<DiscoveryOrchestrator> log, IClock clock)
    { _providers = providers.ToList(); _secrets = secrets; _settings = settings; _sys = sys; _extractor = extractor; _log = log; _clock = clock; }

    public IReadOnlyList<IDiscoveryProvider> Providers => _providers;

    /// <summary>Settings for a provider (customer-supplied key from the secret store).</summary>
    public ProviderSettings SettingsFor(IDiscoveryProvider p)
    {
        _settings.Current.ProviderEndpoints.TryGetValue(p.Id, out var endpoint);
        return new ProviderSettings(_secrets.Get("provider:" + p.Id), endpoint, _settings.Current.ContactEmail);
    }

    /// <summary>Searches all enabled providers until <see cref="DiscoveryRequest.Count"/> unique prospects are found or sources are exhausted.</summary>
    public async Task<DiscoveryRun> RunAsync(DiscoveryRequest req, IProgress<string>? progress, CancellationToken ct)
    {
        var invalid = req.Validate();
        if (invalid != null) throw new ArgumentException(invalid);
        var run = new DiscoveryRun { Request = req };
        var enabled = _providers.Where(p => _settings.Current.EnabledProviders.Contains(p.Id)).ToList();
        if (enabled.Count == 0) { progress?.Report("No discovery source is enabled. Open API Integrations to enable one."); return run; }
        var seen = new DuplicateIndex(Array.Empty<MatchProfile>());
        var nextId = 1;
        var excluded = req.ExcludedCompanies.Select(Normalizer.CoreName).Where(x => x.Length > 0).ToList();

        foreach (var p in enabled)
        {
            if (run.Candidates.Count >= req.Count) break;
            var pr = new ProviderRunResult { ProviderId = p.Id, DisplayName = p.DisplayName };
            run.Providers.Add(pr);
            var settings = SettingsFor(p);
            try
            {
                var avail = await p.CheckAvailabilityAsync(settings, ct).ConfigureAwait(false);
                if (!avail.IsAvailable) { pr.Error = avail.Message; progress?.Report($"{p.DisplayName}: unavailable – {avail.Message}"); continue; }
                string? token = null; var guard = 0;
                do
                {
                    progress?.Report($"{p.DisplayName}: searching… ({run.Candidates.Count}/{req.Count} found)");
                    var page = await FetchWithRetry(p, req, token, settings, pr, ct).ConfigureAwait(false);
                    pr.Warnings.AddRange(page.Warnings);
                    foreach (var c in page.Candidates)
                    {
                        pr.Returned++;
                        if (!Accept(c, req, excluded)) continue;
                        c.Industry ??= KeywordMatcher.GuessIndustry(c.Name, c.Services, c.Description);
                        var prof = MatchProfile.From(nextId++, "", c.Name, c.Website, new[] { c.Phone }, new[] { c.Email }, c.Address, c.City, _settings.Current.DefaultRegion);
                        if (seen.Find(prof).Any(m => m.Confidence == DuplicateConfidence.High)) continue;
                        seen.Add(prof);
                        run.Candidates.Add(c);
                        if (run.Candidates.Count >= req.Count) break;
                    }
                    token = page.NextPageToken;
                } while (token != null && run.Candidates.Count < req.Count && ++guard < 50 && !ct.IsCancellationRequested);
                pr.Succeeded = true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                pr.Error = Describe(ex);
                _log.LogWarning(ex, "Provider {Provider} failed", p.Id);
                progress?.Report($"{p.DisplayName}: failed – {pr.Error}. Continuing with other sources.");
            }
        }

        if (_settings.Current.VisitWebsitesDuringDiscovery && run.Candidates.Count > 0)
            await EnrichFromWebsitesAsync(run.Candidates, progress, ct).ConfigureAwait(false);
        return run;
    }

    private async Task<ProviderPage> FetchWithRetry(IDiscoveryProvider p, DiscoveryRequest req, string? token, ProviderSettings s, ProviderRunResult pr, CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await _limiter.WaitAsync(p.Id, p.MinInterval, ct).ConfigureAwait(false);
            pr.Requests++;
            try
            {
                var page = await p.SearchPageAsync(req, token, s, ct).ConfigureAwait(false);
                _sys.AddApiUsage(new ApiUsageRecord { At = _clock.UtcNow, Provider = p.Id, Operation = "search", Requests = 1, Success = true, ResultCount = page.Candidates.Count });
                return page;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException && !ct.IsCancellationRequested)
            {
                last = ex;
                _sys.AddApiUsage(new ApiUsageRecord { At = _clock.UtcNow, Provider = p.Id, Operation = "search", Requests = 1, Success = false, ErrorMessage = Describe(ex) });
                if (attempt < MaxAttempts && p.MinInterval >= TimeSpan.Zero) await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(4000, 500 * Math.Pow(2, attempt - 1) * (p.MinInterval == TimeSpan.Zero ? 0 : 1))), ct).ConfigureAwait(false);
            }
        }
        throw last ?? new InvalidOperationException("Provider failed");
    }

    private static string Describe(Exception ex) => ex switch
    {
        TaskCanceledException => "request timed out",
        HttpRequestException h when h.StatusCode != null => $"HTTP {(int)h.StatusCode}",
        _ => ex.Message
    };

    private static bool Accept(ProspectCandidate c, DiscoveryRequest req, List<string> excluded)
    {
        if (string.IsNullOrWhiteSpace(c.Name)) return false;
        if (req.RequireWebsite && string.IsNullOrWhiteSpace(c.Website)) return false;
        var core = Normalizer.CoreName(c.Name);
        return !excluded.Any(x => core == x || core.Contains(x, StringComparison.Ordinal));
    }

    private async Task EnrichFromWebsitesAsync(List<ProspectCandidate> cands, IProgress<string>? progress, CancellationToken ct)
    {
        var todo = cands.Where(c => !string.IsNullOrWhiteSpace(c.Website) && (string.IsNullOrWhiteSpace(c.Phone) || string.IsNullOrWhiteSpace(c.Email))).ToList();
        var done = 0;
        using var gate = new SemaphoreSlim(4);
        await Task.WhenAll(todo.Select(async c =>
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var ex = await _extractor.ExtractAsync(c.Website!, ct).ConfigureAwait(false);
                if (!ex.Success) return;
                if (StringSimilarity.NameSimilarity(c.Name, ex.CompanyName ?? c.Name) < EnrichmentService.MinNameSimilarity) return;
                if (string.IsNullOrWhiteSpace(c.Phone)) foreach (var ph in ex.Phones) if (Normalizer.ParsePhone(ph, _settings.Current.DefaultRegion).IsValid)
                { c.Phone = ph; c.IsPublicListing = true; c.Evidence.Add(new CandidateEvidence { Field = "Phone", Value = ph, SourceUrl = ex.ValueSources.GetValueOrDefault(ph, ex.Url), Confidence = 0.7, Note = "Publicly listed on the company's website" }); break; }
                if (string.IsNullOrWhiteSpace(c.Email)) foreach (var em in ex.Emails) if (Normalizer.SameSite(Normalizer.EmailDomain(em), Normalizer.DomainOf(c.Website)))
                { c.Email = em; c.Evidence.Add(new CandidateEvidence { Field = "Email", Value = em, SourceUrl = ex.ValueSources.GetValueOrDefault(em, ex.Url), Confidence = 0.7, Note = "Listed on the company's own website; not verified" }); break; }
                if (string.IsNullOrWhiteSpace(c.Description) && !string.IsNullOrWhiteSpace(ex.Description)) c.Description = ex.Description;
                c.Industry ??= KeywordMatcher.GuessIndustry(c.Name, c.Description, ex.AboutText);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException) { /* keep candidate as is */ }
            finally { gate.Release(); progress?.Report($"Reading company websites… {Interlocked.Increment(ref done)}/{todo.Count}"); }
        })).ConfigureAwait(false);
    }
}
