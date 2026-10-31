using LeadPilot.Application.Models;
using LeadPilot.Application.Scoring;
using LeadPilot.Domain;

namespace LeadPilot.Tests;

public class ScoringTests
{
    private static readonly ScoringProfile Profile = new() { Product = "Commercial Solar Panels", TargetCustomer = "Manufacturing Companies", Location = "Chennai" };

    private static LeadFacts Facts() => new()
    {
        Name = "Sri Ganesh Engineering Works", Industry = "Manufacturing", City = "Chennai", Address = "12 Mount Road",
        HasValidPhone = true, HasValidEmail = true, HasWebsite = true, HasContactPerson = false
    };

    [Fact]
    public void Golden_score_is_70_warm()
    {
        // Industry 25 (field match) + Location 20 (city match) + Product 0 (no solar mention) + Contact 18/20 (phone 6 + email 6 + site 4 + address 2)
        // + Intent 0 = 63 of applicable 90 (size requirement absent) => 63/90 = 70.0
        var r = LeadScorer.Score(Profile, Facts(), new ScoringWeights());
        r.Score.Should().Be(70);
        r.Temperature.Should().Be(LeadTemperature.Warm);
        r.Factors.Single(f => f.Name == "Target industry").Points.Should().Be(25);
        r.Factors.Single(f => f.Name == "Target location").Points.Should().Be(20);
        r.Factors.Single(f => f.Name == "Product/service relevance").Points.Should().Be(0);
        r.Factors.Single(f => f.Name == "Contact completeness").Points.Should().Be(18);
        r.Factors.Single(f => f.Name == "Company size").Applicable.Should().BeFalse();
    }

    [Fact]
    public void Product_mention_and_intent_evidence_make_it_hot()
    {
        var f = Facts(); f.Description = "We install solar panels for factories"; f.HasContactPerson = true; f.IntentEvidence = "Filled enquiry form about rooftop solar";
        var r = LeadScorer.Score(Profile, f, new ScoringWeights());
        // product tokens: solar, panel -> both found = 15; contact 20; intent 10 => 25+20+15+20+10 = 90/90
        r.Score.Should().Be(100);
        r.Temperature.Should().Be(LeadTemperature.Hot);
    }

    [Fact]
    public void Same_lead_scores_lower_without_contact_data()
    {
        var f = Facts(); f.HasValidPhone = f.HasValidEmail = f.HasWebsite = false; f.Address = null;
        var r = LeadScorer.Score(Profile, f, new ScoringWeights());
        // 25 + 20 + 0 + 0 + 0 = 45/90 = 50
        r.Score.Should().Be(50);
        r.Factors.Single(x => x.Name == "Contact completeness").Explanation.Should().Contain("missing phone");
    }

    [Fact]
    public void Wrong_city_and_industry_is_unqualified()
    {
        var f = Facts(); f.City = "Mumbai"; f.Industry = "Bakery"; f.Name = "Sweet Treats"; f.Address = null;
        var r = LeadScorer.Score(Profile, f, new ScoringWeights());
        r.Factors.Single(x => x.Name == "Target location").Explanation.Should().Contain("elsewhere");
        r.Temperature.Should().Be(LeadTemperature.Unqualified);
    }

    [Fact]
    public void Size_requirement_participates_when_set()
    {
        var p = new ScoringProfile { TargetCustomer = "Manufacturing", Location = "Chennai", CompanySize = "50-200" };
        var f = Facts(); f.CompanySize = "50-200";
        var r = LeadScorer.Score(p, f, new ScoringWeights());
        r.Factors.Single(x => x.Name == "Company size").Points.Should().Be(10);
    }

    [Fact]
    public void Custom_weights_change_the_result()
    {
        var w = new ScoringWeights { Industry = 0, Location = 100, ContactCompleteness = 0, ProductRelevance = 0, IntentEvidence = 0 };
        LeadScorer.Score(Profile, Facts(), w).Score.Should().Be(100);
    }

    [Fact]
    public void Every_score_has_an_explanation_and_the_intent_disclaimer()
    {
        var text = LeadScorer.Score(Profile, Facts(), new ScoringWeights()).Explain();
        text.Should().Contain("Relevance score 70/100").And.Contain("not evidence that the company intends to buy");
    }

    [Theory]
    [InlineData(100, LeadTemperature.Hot)] [InlineData(75, LeadTemperature.Hot)] [InlineData(74, LeadTemperature.Warm)]
    [InlineData(50, LeadTemperature.Warm)] [InlineData(49, LeadTemperature.Cold)] [InlineData(25, LeadTemperature.Cold)] [InlineData(24, LeadTemperature.Unqualified)]
    public void Temperature_buckets(int score, LeadTemperature expected) => LeadScorer.Bucket(score, new ScoringWeights()).Should().Be(expected);

    [Fact]
    public void Empty_name_scores_zero() => LeadScorer.Score(Profile, new LeadFacts(), new ScoringWeights()).Score.Should().Be(0);

    [Theory]
    [InlineData("Precision Fabrication Works Pvt Ltd", "Manufacturing")]
    [InlineData("St Joseph's College of Engineering", "Education")]
    [InlineData("BrightPath Digital Marketing Agency", "Marketing")]
    public void Industry_guess(string name, string expected) => KeywordMatcher.GuessIndustry(name).Should().Be(expected);
}
