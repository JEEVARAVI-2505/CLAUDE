using System.Diagnostics;
using CRM_Master.Core.BusinessRules;
using CRM_Master.Core.DTOs;
using CRM_Master.Core.Entities;
using CRM_Master.Engine.Automation;
using CRM_Master.Engine.DuplicateDetection;
using CRM_Master.Tests.Support;
using Xunit.Abstractions;

namespace CRM_Master.Tests.Engine;

public class DuplicateDetectorTests
{
    private static int _id;
    private static Contact C(string? name, string? email = null, string? phone = null, string? company = null, string? city = null)
    {
        var c = new Contact { Id = ++_id, FullName = name, Email = email, Phone = phone, CompanyName = company, City = city };
        ContactKeys.Apply(c, "91"); return c;
    }
    private static readonly DuplicateOptions O = new();

    // ---- scoring goldens (hand calculated from the documented formula) ----
    [Fact]
    public void Same_email_and_same_name_is_exact()
    {
        var p = DuplicateDetector.Compare(C("Ravi Kumar", "ravi@x.com"), C("RAVI  KUMAR", "RAVI@x.com"), O)!;
        p.Class.Should().Be(MatchClass.Exact); p.Score.Should().Be(1.0); p.MatchedOn.Should().Contain("Email");
    }

    [Fact]
    public void Same_email_different_names_is_high_not_exact()
    {
        // email 0.90, names are dissimilar so no name bonus: 0.90 -> High
        var p = DuplicateDetector.Compare(C("Ravi Kumar", "shared@x.com"), C("Priya Shah", "shared@x.com"), O)!;
        p.Score.Should().BeApproximately(0.90, 1e-9); p.Class.Should().Be(MatchClass.High);
    }

    [Fact]
    public void Phone_only_evidence_scores_0_872_and_is_possible()
    {
        // 0.82 + 0.13 * 0.4 (name unknown) = 0.872
        var p = DuplicateDetector.Compare(C(null, phone: "9812345670"), C(null, phone: "+91 98123-45670"), O)!;
        p.Score.Should().BeApproximately(0.872, 1e-9); p.Class.Should().Be(MatchClass.Possible); p.MatchedOn.Should().Be("Phone");
    }

    [Fact]
    public void Phone_matches_across_primary_and_alternate_numbers()
    {
        var a = C("A One", phone: "9812345670"); var b = C("A One"); b.AlternatePhone = "09812345670"; ContactKeys.Apply(b, "91");
        DuplicateDetector.Compare(a, b, O).Should().NotBeNull();
    }

    [Fact]
    public void Name_only_match_is_possible_at_0_8()
    {
        var p = DuplicateDetector.Compare(C("John Smith"), C("john smith"), O)!;
        p.Score.Should().BeApproximately(0.80, 1e-9); p.Class.Should().Be(MatchClass.Possible);
    }

    [Fact]
    public void Fuzzy_name_alone_is_low_confidence_0_7733()
    {
        // NameSimilarity(Jon Smith, John Smith) = (0.9333 + 1)/2 = 0.9667 ; name-only score = 0.8 * 0.9667 = 0.7733
        var p = DuplicateDetector.Compare(C("Jon Smith"), C("John Smith"), O)!;
        p.Score.Should().BeApproximately(0.7733, 1e-3); p.Class.Should().Be(MatchClass.Low);
    }

    [Fact]
    public void Fuzzy_name_plus_same_company_is_high_0_98()
    {
        // 0.6 * 0.9667 + 0.4 * 1.0 = 0.98
        var p = DuplicateDetector.Compare(C("Jon Smith", company: "Acme Pvt Ltd"), C("John Smith", company: "ACME Limited"), O)!;
        p.Score.Should().BeApproximately(0.98, 1e-3); p.Class.Should().Be(MatchClass.High);
        p.MatchedOn.Should().Contain("Company");
    }

    [Fact]
    public void Same_name_at_different_companies_is_not_high()
    {
        var p = DuplicateDetector.Compare(C("John Smith", company: "Acme"), C("John Smith", company: "Zenith Corp"), O);
        (p is null || p.Class < MatchClass.High).Should().BeTrue();
    }

    [Fact]
    public void Different_people_do_not_match() =>
        DuplicateDetector.Compare(C("Ravi Kumar", "a@x.com", "9812345670"), C("Priya Shah", "b@x.com", "9812345671"), O).Should().BeNull();

    [Fact]
    public void Exact_requires_every_shared_field_to_agree()
    {
        var a = C("Ravi Kumar", "r@x.com", "9812345670", "Acme"); var b = C("Ravi Kumar", "r@x.com", "9812345999", "Acme");
        DuplicateDetector.IsExact(a, b).Should().BeFalse();       // phone conflicts
        DuplicateDetector.Compare(a, b, O)!.Class.Should().Be(MatchClass.High);
    }

    [Fact]
    public void Thresholds_are_configurable()
    {
        var a = C("Jon Smith"); var b = C("John Smith");
        DuplicateDetector.Compare(a, b, new DuplicateOptions { LowThreshold = 0.5, PossibleThreshold = 0.75, HighThreshold = 0.95 })!.Class.Should().Be(MatchClass.Possible);
        DuplicateDetector.Compare(a, b, new DuplicateOptions { LowThreshold = 0.8, PossibleThreshold = 0.85, HighThreshold = 0.95 }).Should().BeNull();
    }

    [Fact]
    public void Matching_rules_can_be_switched_off()
    {
        var a = C("A One", "same@x.com"); var b = C("B Two", "same@x.com");
        DuplicateDetector.Compare(a, b, new DuplicateOptions { MatchEmail = false }).Should().BeNull();
        var j1 = C("Jon Smith"); var j2 = C("John Smith");
        DuplicateDetector.Compare(j1, j2, new DuplicateOptions { UseFuzzy = false }).Should().BeNull();
        DuplicateDetector.Compare(C("John Smith"), C("John Smith"), new DuplicateOptions { UseFuzzy = false }).Should().NotBeNull();
    }

    // ---- scanning & clustering ----
    [Fact]
    public void Scan_groups_connected_records_and_keeps_low_matches_as_pairs()
    {
        var list = new List<Contact>
        {
            C("Ravi Kumar", "r@x.com", "9812345670"), C("Ravi Kumar", "r@x.com"), C(null, phone: "9812345670"),
            C("Jon Smith"), C("John Smith"), C("Unrelated Person", "u@x.com"),
        };
        var scan = DuplicateDetector.Scan(list, O);
        scan.Clusters.Should().HaveCount(2);
        var big = scan.Clusters[0];
        big.ContactIds.Should().HaveCount(3); big.Class.Should().Be(MatchClass.Exact);
        scan.Clusters[1].Class.Should().Be(MatchClass.Low);
        scan.Clusters.SelectMany(c => c.ContactIds).Should().NotContain(list[5].Id);
    }

    [Fact]
    public void Pairs_marked_separate_are_never_proposed_again()
    {
        var a = C("Ravi Kumar", "r@x.com"); var b = C("Ravi Kumar", "r@x.com");
        DuplicateDetector.Scan(new[] { a, b }, O, new HashSet<(int, int)> { (a.Id, b.Id) }).Clusters.Should().BeEmpty();
    }

    [Fact]
    public void Side_by_side_comparison_flags_matches_and_conflicts()
    {
        var l = C("Ravi Kumar", "r@x.com", "9812345670", "Acme"); l.City = "Pune";
        var r = C("Ravi Kumar", "R@X.com", "9812345999", "Acme Pvt Ltd"); r.JobTitle = "CEO";
        var cmp = DuplicateDetector.CompareFields(l, r).ToDictionary(x => x.Field);
        cmp[ContactField.Email].Status.Should().Be(FieldCompareStatus.Match);
        cmp[ContactField.Company].Status.Should().Be(FieldCompareStatus.Match);
        cmp[ContactField.Phone].Status.Should().Be(FieldCompareStatus.Conflict);
        cmp[ContactField.City].Status.Should().Be(FieldCompareStatus.OnlyLeft);
        cmp[ContactField.JobTitle].Status.Should().Be(FieldCompareStatus.OnlyRight);
        cmp[ContactField.Notes].Status.Should().Be(FieldCompareStatus.BothEmpty);
    }

    // ---- merge planning ----
    [Fact]
    public void Merge_plan_keeps_master_values_fills_gaps_and_flags_conflicts()
    {
        var master = C("Ravi Kumar", "r@x.com"); master.City = "Pune";
        var other = C("Ravi Kumar", "r@x.com", "9812345670"); other.City = "Mumbai"; other.Notes = "VIP";
        var plan = MergePlanner.Plan(master, new[] { other }).ToDictionary(p => p.Field);
        plan[ContactField.City].Selected.Should().Be("Pune"); plan[ContactField.City].IsConflict.Should().BeTrue();
        plan[ContactField.Phone].Selected.Should().Be("9812345670");
        plan[ContactField.Notes].Selected.Should().Be("VIP");
        plan[ContactField.Email].IsConflict.Should().BeFalse();
    }

    [Fact]
    public void Suggested_master_is_the_most_complete_record()
    {
        var sparse = C("A", "a@x.com"); var full = C("A", "a@x.com", "9812345670", "Acme", "Pune");
        MergePlanner.SuggestMaster(new[] { sparse, full }).Should().BeSameAs(full);
    }
}

public class DuplicateServiceTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _out; public DuplicateServiceTests(ITestOutputHelper o) => _out = o;
    private TestDb _db = null!; private DuplicateService _svc = null!; private AutomationService _auto = null!;
    public async Task InitializeAsync()
    {
        _db = await TestDb.CreateAsync();
        _auto = new AutomationService(_db.Factory, _db.Settings, _db.Backups);
        _svc = new DuplicateService(_db.Factory, _db.Settings, _db.Backups, _db.Contacts, _auto);
    }
    public Task DisposeAsync() { _db.Dispose(); return Task.CompletedTask; }

    private async Task<(Contact a, Contact b)> SeedPair()
    {
        var a = await _db.Contacts.AddAsync(new Contact { FullName = "Ravi Kumar", Email = "r@x.com", City = "Pune", CompanyName = "Acme" });
        var b = await _db.Contacts.AddAsync(new Contact { FullName = "Ravi Kumar", Email = "r@x.com", Phone = "9812345670", City = "Mumbai", Notes = "VIP" });
        return (a, b);
    }

    [Fact]
    public async Task Scan_persists_groups_matches_and_audit()
    {
        await SeedPair();
        await _db.Contacts.AddAsync(new Contact { FullName = "Someone Else", Email = "s@x.com" });
        var scan = await _svc.ScanAsync(_svc.DefaultOptions());
        scan.Clusters.Should().ContainSingle();
        var page = await _svc.GetGroupsAsync();
        page.TotalCount.Should().Be(1);
        var g = page.Items.Single();
        (g.Class, g.Contacts.Count, g.Matches.Single().MatchedOn).Should().Be((MatchClass.Exact, 2, "Email + Name"));   // every field both records share agrees
        (await _db.Audit.QueryAsync(new(Operation: AuditOperation.DuplicateDetection))).Items.Single().Summary.Should().Contain("1 group");
    }

    [Fact]
    public async Task Merge_combines_selected_fields_removes_losers_and_is_audited_and_undoable()
    {
        var (a, b) = await SeedPair();
        await _svc.ScanAsync(_svc.DefaultOptions());
        var g = (await _svc.GetGroupsAsync()).Items.Single();
        var plan = MergePlanner.Plan(a, new[] { b }).ToDictionary(p => p.Field, p => p.Selected);
        plan[ContactField.City] = "Mumbai";                               // user prefers the other record's city

        var res = await _svc.MergeAsync(g.GroupId, a.Id, plan);
        res.Merged.Should().Be(1);
        File.Exists(res.BackupPath!).Should().BeTrue();
        (await _db.Contacts.CountAsync()).Should().Be(1);
        var m = (await _db.Contacts.GetAsync(a.Id))!;
        (m.City, m.Phone, m.Notes, m.CompanyName).Should().Be(("Mumbai", "9812345670", "VIP", "Acme"));
        (await _svc.GetGroupsAsync(DuplicateStatus.Open)).TotalCount.Should().Be(0);
        (await _svc.GetGroupsAsync(DuplicateStatus.Merged)).TotalCount.Should().Be(1);
        (await _db.Audit.QueryAsync(new(Operation: AuditOperation.Merge, BatchId: res.BatchId))).TotalCount.Should().Be(2);

        var undo = await new CRM_Master.Engine.BatchUndoService(_db.Factory, _db.Settings).UndoAsync(res.BatchId);
        (undo.Restored, undo.Reverted).Should().Be((1, 1));
        (await _db.Contacts.CountAsync()).Should().Be(2);
        (await _db.Contacts.GetAsync(a.Id))!.City.Should().Be("Pune");
    }

    [Fact]
    public async Task Marking_separate_is_remembered_by_later_scans()
    {
        await SeedPair();
        await _svc.ScanAsync(_svc.DefaultOptions());
        await _svc.MarkSeparateAsync((await _svc.GetGroupsAsync()).Items.Single().GroupId);
        (await _svc.GetGroupsAsync()).TotalCount.Should().Be(0);
        await _svc.ScanAsync(_svc.DefaultOptions());
        (await _svc.GetGroupsAsync()).TotalCount.Should().Be(0);
        (await _svc.GetGroupsAsync(DuplicateStatus.MarkedSeparate)).TotalCount.Should().Be(1);
        (await _db.Contacts.CountAsync()).Should().Be(2);   // nothing deleted
    }

    [Fact]
    public async Task Skipping_leaves_data_untouched_and_rescan_reopens_the_group()
    {
        await SeedPair();
        await _svc.ScanAsync(_svc.DefaultOptions());
        await _svc.SkipAsync((await _svc.GetGroupsAsync()).Items.Single().GroupId);
        (await _svc.GetGroupsAsync()).TotalCount.Should().Be(0);
        await _svc.ScanAsync(_svc.DefaultOptions());
        (await _svc.GetGroupsAsync()).TotalCount.Should().Be(1);
        (await _db.Contacts.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Duplicate_rule_flags_contacts_during_scan_only_when_enabled()
    {
        await _auto.SeedDefaultsAsync();
        await SeedPair();
        await _svc.ScanAsync(_svc.DefaultOptions());
        (await _db.Contacts.QueryAsync(new(Quality: QualityFilter.FlaggedDuplicate))).TotalCount.Should().Be(2);
        (await _auto.HistoryAsync()).Should().Contain(h => h.Rule!.Name == "Flag duplicate emails" && h.AffectedCount == 2);
    }

    [Fact]
    public async Task Merge_all_exact_handles_only_exact_groups()
    {
        await _db.Contacts.AddAsync(new Contact { FullName = "Exact Twin", Email = "t@x.com" });
        await _db.Contacts.AddAsync(new Contact { FullName = "exact twin", Email = "T@x.com", City = "Pune" });
        await _db.Contacts.AddAsync(new Contact { FullName = "Jon Smith" });
        await _db.Contacts.AddAsync(new Contact { FullName = "John Smith" });     // uncertain -> must stay
        await _svc.ScanAsync(_svc.DefaultOptions());
        var (groups, records) = await _svc.MergeAllExactAsync();
        (groups, records).Should().Be((1, 1));
        (await _db.Contacts.CountAsync()).Should().Be(3);
        (await _db.Contacts.QueryAsync(new(Search: "Smith"))).TotalCount.Should().Be(2);
        (await _db.Contacts.QueryAsync(new(Search: "twin"))).Items.Single().City.Should().Be("Pune");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void Fifty_thousand_contacts_scan_with_planted_duplicates()
    {
        var rnd = new Random(12345);   // explicit seed: deterministic
        var list = new List<Contact>(50_500);
        for (var i = 1; i <= 50_000; i++)
        {
            var c = new Contact { Id = i, FullName = $"First{i % 997} Last{i}", Email = $"user{i}@company{i % 3000}.com", Phone = (9000000000L + i * 7).ToString(), CompanyName = $"Company {i % 3000}" };
            ContactKeys.Apply(c, "91"); list.Add(c);
        }
        for (var k = 0; k < 500; k++)   // plant 500 duplicates of random rows with different formatting
        {
            var src = list[rnd.Next(0, 50_000)];
            var d = new Contact { Id = 50_001 + k, FullName = src.FullName!.ToUpperInvariant(), Email = src.Email!.ToUpperInvariant(), Phone = "0" + src.Phone };
            ContactKeys.Apply(d, "91"); list.Add(d);
        }
        var sw = Stopwatch.StartNew();
        var scan = DuplicateDetector.Scan(list, new DuplicateOptions());
        _out.WriteLine($"Scanned {list.Count:N0} contacts in {sw.Elapsed.TotalSeconds:F1}s; {scan.Clusters.Count} groups; {scan.CandidatePairs:N0} comparisons (full pairwise would be {(long)list.Count * (list.Count - 1) / 2:N0}); {scan.SkippedLargeBlocks} oversized blocks skipped");
        scan.Clusters.Count(c => c.Class == MatchClass.Exact).Should().BeGreaterThanOrEqualTo(499);   // planted (a repeat pick of the same row merges into one group)
        scan.CandidatePairs.Should().BeLessThan(20_000_000);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(2));
    }
}
