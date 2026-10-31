using LeadPilot.Application.Duplicates;
using LeadPilot.Domain;

namespace LeadPilot.Tests;

/// <summary>Golden cases for the noisy-OR score. Contributions: phone .80, e-mail .85, domain .80, name .65·sim⁴, address .45·sim.</summary>
public class DuplicateDetectorTests
{
    private static MatchProfile P(int id, string name, string? phone = null, string? email = null, string? web = null, string? addr = null) =>
        MatchProfile.From(id, $"LD-{id:D6}", name, web, new[] { phone }, new[] { email }, addr, "Chennai");

    [Fact]
    public void Same_phone_only_scores_0_80_high()
    {
        var r = DuplicateDetector.Compare(P(1, "Alpha Steel", "+91 98765 43210"), P(2, "Zeta Textiles", "9876543210"));
        r!.Score.Should().Be(0.80); r.Confidence.Should().Be(DuplicateConfidence.High);
        r.Reasons.Should().ContainSingle(x => x.StartsWith("Same phone"));
    }

    [Fact]
    public void Same_email_only_scores_0_85_high() =>
        DuplicateDetector.Compare(P(1, "Alpha", email: "Info@Alpha.com"), P(2, "Beta Works", email: "info@alpha.com"))!.Score.Should().Be(0.85);

    [Fact]
    public void Identical_name_alone_scores_0_65_medium_needs_review()
    {
        var r = DuplicateDetector.Compare(P(1, "ABC Industries Pvt Ltd"), P(2, "ABC Industries"))!;
        r.Score.Should().Be(0.65); r.Confidence.Should().Be(DuplicateConfidence.Medium);
    }

    [Fact]
    public void Name_plus_domain_is_high()
    {
        // 1 - (1-0.65)(1-0.80) = 0.93
        var r = DuplicateDetector.Compare(P(1, "ABC Industries", web: "https://www.abc.com"), P(2, "ABC Industries Ltd", web: "abc.com/contact"))!;
        r.Score.Should().Be(0.93); r.Confidence.Should().Be(DuplicateConfidence.High);
    }

    [Fact]
    public void Similar_name_scores_between_low_and_medium()
    {
        var r = DuplicateDetector.Compare(P(1, "Sri Ganesh Engineering Works"), P(2, "Sri Ganesha Engineering Works"))!;
        r.Score.Should().BeInRange(0.55, 0.65);
        r.Confidence.Should().Be(DuplicateConfidence.Medium);
    }

    [Fact]
    public void Unrelated_companies_do_not_match() =>
        DuplicateDetector.Compare(P(1, "Alpha Steel", "+919876543210"), P(2, "Zeta Textiles", "+919876500000")).Should().BeNull();

    [Fact]
    public void Shared_social_domain_is_not_an_identifier() =>
        DuplicateDetector.Compare(P(1, "Alpha", web: "https://facebook.com/alpha"), P(2, "Beta", web: "https://facebook.com/beta")).Should().BeNull();

    [Fact]
    public void Free_mail_is_not_treated_as_domain_but_email_still_matches()
    {
        var r = DuplicateDetector.Compare(P(1, "Alpha", email: "alpha@gmail.com"), P(2, "Alpha Co", email: "alpha@gmail.com"))!;
        r.Confidence.Should().Be(DuplicateConfidence.High);
    }

    [Fact]
    public void Result_pair_is_ordered_by_id()
    {
        var r = DuplicateDetector.Compare(P(9, "X", "+919876543210"), P(3, "Y", "+919876543210"))!;
        (r.LeadAId, r.LeadBId).Should().Be((3, 9));
    }

    [Fact]
    public void FindAll_and_index_agree_and_report_each_pair_once()
    {
        var pool = new List<MatchProfile> { P(1, "Alpha Steel", "+919876543210"), P(2, "Alpha Steel Pvt Ltd", "+919876543210"), P(3, "Zeta Textiles"), P(4, "Alpha Steel Industries") };
        var all = DuplicateDetector.FindAll(pool);
        all.Select(a => (a.LeadAId, a.LeadBId)).Should().OnlyHaveUniqueItems();
        all.Should().Contain(a => a.LeadAId == 1 && a.LeadBId == 2 && a.Confidence == DuplicateConfidence.High);
        var idx = new DuplicateIndex(pool.Where(p => p.LeadId != 2));
        idx.Find(pool[1]).Should().Contain(r => r.LeadAId == 1 && r.LeadBId == 2);
    }
}
