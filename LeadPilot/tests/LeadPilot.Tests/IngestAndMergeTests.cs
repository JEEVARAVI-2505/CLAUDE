using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Domain;
using LeadPilot.Tests.Support;

namespace LeadPilot.Tests;

public class IngestAndMergeTests
{
    [Fact]
    public void Ingest_normalises_scores_and_records_evidence_with_timestamp_and_url()
    {
        using var env = new TestEnv();
        var id = env.Add("Sri Ganesh Engineering Works", c =>
        {
            c.Phone = "098765 43210"; c.Email = "Sales@Ganesh.com"; c.Website = "ganesh.com"; c.Address = "12 Mount Road";
            c.SourceName = "OpenStreetMap"; c.SourceType = "Public business directory"; c.SourceUrl = "https://www.openstreetmap.org/node/1"; c.IsPublicListing = true;
        });
        var d = env.Leads.GetDetail(id)!;
        d.Business.NormalizedPhone.Should().Be("+919876543210");
        d.Business.NormalizedEmail.Should().Be("sales@ganesh.com");
        d.Business.Domain.Should().Be("ganesh.com");
        d.Business.Industry.Should().Be("Manufacturing", "guessed from the name by keyword rules");
        d.Lead.PhoneStatus.Should().Be(PhoneStatus.PubliclyListed);
        d.Lead.EmailStatus.Should().Be(EmailStatus.ValidFormat, "format only – never 'verified'");
        d.Lead.WebsiteStatus.Should().Be(WebsiteStatus.Unverified);
        d.Lead.Score.Should().Be(70); d.Lead.ScoreExplanation.Should().Contain("Target industry");
        d.Sources.Should().ContainSingle(s => s.SourceUrl == "https://www.openstreetmap.org/node/1" && s.RetrievedAt == env.Clock.UtcNow);
        d.Evidence.Should().Contain(e => e.FieldName == "Phone" && e.SourceUrl == "https://www.openstreetmap.org/node/1" && e.ExtractedAt == env.Clock.UtcNow);
    }

    [Fact]
    public void Invalid_email_and_phone_are_kept_but_flagged()
    {
        using var env = new TestEnv();
        var id = env.Add("Flag Works", c => { c.Email = "not-an-email"; c.Phone = "12345"; });
        var d = env.Leads.GetDetail(id)!;
        d.Lead.EmailStatus.Should().Be(EmailStatus.InvalidFormat); d.Lead.PhoneStatus.Should().Be(PhoneStatus.InvalidFormat);
        d.Business.Email.Should().Be("not-an-email"); d.Business.NormalizedEmail.Should().BeNull();
    }

    [Fact]
    public void High_confidence_duplicate_is_not_inserted_but_source_is_attached()
    {
        using var env = new TestEnv();
        var first = env.Add("Alpha Steel", c => c.Phone = "+919876543210", allowDuplicate: false);
        var res = env.Ingest.Ingest(new ProspectCandidate { Name = "Alpha Steel Industries", Phone = "09876543210", SourceName = "Second source", SourceType = "Directory" },
            new IngestContext { Profile = TestEnv.ChennaiManufacturing });
        res.Status.Should().Be(IngestStatus.DuplicateLinked); res.LeadId.Should().Be(first);
        env.Leads.Count().Should().Be(1);
        env.Leads.GetDetail(first)!.Sources.Select(s => s.SourceName).Should().Contain("Second source");
    }

    [Fact]
    public void Medium_confidence_duplicate_is_inserted_and_flagged_for_review()
    {
        using var env = new TestEnv();
        env.Add("ABC Industries");
        var res = env.Ingest.Ingest(new ProspectCandidate { Name = "ABC Industries Pvt Ltd", City = "Madurai" }, new IngestContext { Profile = TestEnv.ChennaiManufacturing });
        res.Status.Should().Be(IngestStatus.Created);
        env.Dups.CountPending().Should().Be(1);
        var v = env.Duplicates.Pending().Single();
        v.Match.Confidence.Should().Be(DuplicateConfidence.Medium);
        (v.A.CompanyName, v.B.CompanyName).Should().Be(("ABC Industries", "ABC Industries Pvt Ltd"));
    }

    [Fact]
    public void License_limit_blocks_new_leads()
    {
        using var env = new TestEnv();
        env.License.Current = env.License.Current with { MaxLeads = 1 };
        env.Add("One Works");
        var res = env.Ingest.Ingest(new ProspectCandidate { Name = "Two Works" }, new IngestContext());
        res.Status.Should().Be(IngestStatus.Skipped); res.Message.Should().Contain("limit");
    }

    [Fact]
    public void Expired_license_blocks_ingest()
    {
        using var env = new TestEnv();
        env.License.Current = env.License.Current with { IsValid = false, Message = "Trial expired" };
        env.Ingest.Ingest(new ProspectCandidate { Name = "X Works" }, new IngestContext()).Status.Should().Be(IngestStatus.Skipped);
    }

    [Fact]
    public void Scan_finds_pairs_and_decisions_stick()
    {
        using var env = new TestEnv();
        env.Add("Alpha Steel", c => c.Phone = "+919876543210");
        env.Add("Alpha Steels Pvt Ltd", c => c.Phone = "+91 98765 43210");
        env.Add("Zeta Textiles");
        env.Dups.CountPending().Should().Be(1, "the second add already flagged the pair");
        env.Duplicates.Scan().Should().Be(0, "no new pairs");
        var m = env.Duplicates.Pending().Single();
        env.Duplicates.MarkNotDuplicate(m.Match.Id);
        env.Duplicates.Scan().Should().Be(0, "a rejected pair is not suggested again");
        env.Dups.CountPending().Should().Be(0);
    }

    private (TestEnv env, int a, int b) TwoLeads()
    {
        var env = new TestEnv();
        var a = env.Add("Alpha Steel", c => { c.Phone = "+919876543210"; c.Address = "1 Main Rd"; c.SourceName = "Src A"; });
        var b = env.Add("Alpha Steel Industries", c => { c.Phone = "+919876543210"; c.Email = "info@alpha.com"; c.Website = "https://alpha.com"; c.ContactName = "Meena"; c.SourceName = "Src B"; c.Industry = "Manufacturing"; }, allowDuplicate: true);
        return (env, a, b);
    }

    [Fact]
    public void Merge_fills_gaps_moves_history_archives_and_never_overwrites()
    {
        var (env, a, b) = TwoLeads(); using var _ = env;
        env.FollowUps.Schedule(b, "Call Meena", env.Clock.UtcNow.AddDays(1));
        env.FollowUps.LogInteraction(b, ActivityType.Call, "Intro call", null);
        var id = env.Duplicates.Merge(a, b);
        id.Should().BeGreaterThan(0);

        var s = env.Leads.GetDetail(a)!;
        s.Business.Name.Should().Be("Alpha Steel", "survivor values are never overwritten");
        s.Business.Address.Should().Be("1 Main Rd");
        s.Business.Email.Should().Be("info@alpha.com"); s.Business.Website.Should().Be("https://alpha.com"); s.Business.Industry.Should().Be("Manufacturing");
        s.Lead.PrimaryContactId.Should().NotBeNull();
        s.Sources.Select(x => x.SourceName).Should().BeEquivalentTo("Src A", "Src B");
        s.Tasks.Should().ContainSingle(t => t.Title == "Call Meena");
        s.Activities.Should().Contain(x => x.Subject == "Intro call");
        s.Lead.NextFollowUpAt.Should().Be(env.Clock.UtcNow.AddDays(1));

        var m = env.Leads.GetLead(b)!;
        m.IsArchived.Should().BeTrue(); m.MergedIntoLeadId.Should().Be(a);
        env.Leads.Search(new LeadQuery()).Total.Should().Be(1);
        env.Dups.GetMerges().Single().MergedLeadId.Should().Be(b);
        env.Sys.GetAudit(10).Should().Contain(x => x.Action == "lead.merge");
    }

    [Fact]
    public void Undo_restores_both_leads_exactly()
    {
        var (env, a, b) = TwoLeads(); using var _ = env;
        env.FollowUps.Schedule(b, "Call Meena", env.Clock.UtcNow.AddDays(1));
        var beforeA = env.Leads.GetDetail(a)!; var beforeB = env.Leads.GetDetail(b)!;
        var mergeId = env.Duplicates.Merge(a, b);
        env.Duplicates.Undo(mergeId);

        var afterA = env.Leads.GetDetail(a)!; var afterB = env.Leads.GetDetail(b)!;
        afterA.Business.Email.Should().Be(beforeA.Business.Email); afterA.Business.Website.Should().Be(beforeA.Business.Website);
        afterA.Business.Industry.Should().Be(beforeA.Business.Industry);
        afterA.Lead.PrimaryContactId.Should().Be(beforeA.Lead.PrimaryContactId);
        afterA.Sources.Should().HaveCount(beforeA.Sources.Count);
        afterA.Tasks.Should().BeEmpty();
        afterB.Lead.IsArchived.Should().BeFalse(); afterB.Lead.MergedIntoLeadId.Should().BeNull();
        afterB.Sources.Should().HaveCount(beforeB.Sources.Count);
        afterB.Tasks.Should().ContainSingle();
        afterB.Lead.NextFollowUpAt.Should().Be(env.Clock.UtcNow.AddDays(1));
        env.Dups.GetMerges().Single().UndoneAt.Should().NotBeNull();
        env.Dups.CountPending().Should().Be(1, "the pair is pending again");
        ((Action)(() => env.Duplicates.Undo(mergeId))).Should().Throw<InvalidOperationException>("already undone");
    }

    [Fact]
    public void Cannot_merge_into_self_or_archived()
    {
        var (env, a, b) = TwoLeads(); using var _ = env;
        ((Action)(() => env.Duplicates.Merge(a, a))).Should().Throw<InvalidOperationException>();
        env.Duplicates.Merge(a, b);
        ((Action)(() => env.Duplicates.Merge(a, b))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rescore_uses_campaign_profile_and_changes_when_weights_change()
    {
        using var env = new TestEnv();
        var cmp = env.Work.AddCampaign(new Campaign { Name = "C", Product = "Solar", TargetCustomer = "Manufacturing", Location = "Chennai" });
        var id = env.Add("Sri Ganesh Engineering Works", c => c.Phone = "+919876543210", new IngestContext { Profile = new Application.Scoring.ScoringProfile(), CampaignId = cmp });
        var before = env.Leads.GetLead(id)!.Score;
        env.Scoring.Rescore(id)!.Factors.Single(f => f.Name == "Target industry").Fraction.Should().Be(1);
        var s = env.Settings.Current; s.Scoring.Location = 0; s.Scoring.ContactCompleteness = 80; env.Settings.Save(s);
        env.Scoring.RescoreAll().Should().Be(1);
        env.Leads.GetLead(id)!.Score.Should().NotBe(before);
    }

    [Fact]
    public async Task Assistant_answers_from_real_records()
    {
        using var env = new TestEnv();
        env.Add("Top Works", c => { c.Phone = "+919876543210"; c.Email = "a@top.com"; c.Website = "https://top.com"; c.Industry = "Manufacturing"; });
        env.Add("Low Bakery", c => c.Industry = "Bakery");
        var drafter = new OutreachDrafter(new NoAi(), env.Work);
        var asst = new AssistantService(env.Leads, env.Work, env.Dups, env.Stats, drafter, env.Settings, new NoAi(), env.Clock);
        var me = new SenderProfile { SenderName = "Asha", SenderCompany = "SunCo", Offer = "rooftop solar" };

        var top = (await asst.AskAsync("Show my top 1 prospects", me));
        top.Leads.Should().ContainSingle().Which.CompanyName.Should().Be("Top Works");
        (await asst.AskAsync("Find companies with missing contact information", me)).Leads.Should().ContainSingle().Which.CompanyName.Should().Be("Low Bakery");
        (await asst.AskAsync("Summarize my lead database", me)).Text.Should().Contain("2 lead(s)");
        var find = (await asst.AskAsync("Find manufacturing companies in Chennai", me));
        find.Intent.Should().Be(AssistantIntent.FindCompanies);
        find.Leads.Should().ContainSingle().Which.CompanyName.Should().Be("Top Works");
        find.SuggestedDiscovery!.Location.Should().Be("Chennai");
        var mail = (await asst.AskAsync("Generate an introduction email for LD-000001", me));
        mail.Draft!.Body.Should().Contain("Top Works").And.Contain("stop");
        (await asst.AskAsync("what is the weather", me)).Text.Should().Contain("I can:");
        (await asst.AskAsync("Generate an introduction email for LD-009999", me)).Text.Should().Contain("couldn't find");
    }

    private sealed class NoAi : Application.Abstractions.IAiClient
    {
        public bool IsAvailable => false; public string Name => "none";
        public Task<string?> CompleteAsync(string system, string prompt, CancellationToken ct) => Task.FromResult<string?>(null);
    }
}
