using CRM_Master.Core.BusinessRules;

namespace CRM_Master.Tests.Core;

public class TextRulesTests
{
    [Theory]
    [InlineData("  John   Smith  ", "John Smith")]
    [InlineData("John  Smith", "John Smith")]
    [InlineData("Jo​hn", "John")]
    [InlineData("a\tb\r\nc", "a b c")]
    public void Clean_trims_and_collapses(string raw, string expected) => TextCleaner.Clean(raw).Should().Be(expected);

    [Fact]
    public void Clean_returns_null_for_blank() { TextCleaner.Clean("   ").Should().BeNull(); TextCleaner.Clean(null).Should().BeNull(); }

    [Fact]
    public void CleanMultiline_keeps_line_breaks() =>
        TextCleaner.CleanMultiline(" a  b \r\n\r\n c ").Should().Be("a b\nc");

    [Theory]
    [InlineData("RAVI KUMAR", "Ravi Kumar")]
    [InlineData("ravi kumar", "Ravi Kumar")]
    [InlineData("o'brien", "O'Brien")]
    [InlineData("MARY-JANE WATSON", "Mary-Jane Watson")]
    [InlineData("a.r. rahman", "A.R. Rahman")]
    [InlineData("MCDONALD", "McDonald")]
    [InlineData("LUDWIG VAN BEETHOVEN", "Ludwig van Beethoven")]
    [InlineData("JOHN SMITH III", "John Smith III")]
    public void Proper_case_for_single_case_input(string raw, string expected) => NameCaser.Proper(raw).Should().Be(expected);

    [Theory]
    [InlineData("McDonald")]
    [InlineData("DeSouza")]
    [InlineData("Ravi Kumar")]
    public void Mixed_case_names_are_preserved(string raw) => NameCaser.Proper(raw).Should().Be(raw);

    [Theory]
    [InlineData("Example.COM", "https://example.com")]
    [InlineData("http://www.Acme.in/", "http://www.acme.in")]
    [InlineData("https://acme.com:443/about/", "https://acme.com/about")]
    [InlineData("acme.com/path?q=1", "https://acme.com/path?q=1")]
    public void Url_normalisation(string raw, string expected) => UrlRules.Normalize(raw).Should().Be(expected);

    [Theory]
    [InlineData("acme.com", true)]
    [InlineData("https://www.acme.co.in/x", true)]
    [InlineData("localhost", false)]
    [InlineData("not a url", false)]
    [InlineData("acme.123", false)]
    [InlineData("", false)]
    public void Url_validity(string raw, bool valid) => UrlRules.IsValid(raw).Should().Be(valid);

    [Fact]
    public void Host_key_ignores_www_and_scheme() =>
        UrlRules.HostKey("https://WWW.acme.com/a").Should().Be(UrlRules.HostKey("acme.com"));

    [Theory]
    [InlineData("31/12/2023", 2023, 12, 31)]
    [InlineData("01/02/2024", 2024, 2, 1)]       // day-first: 1 February
    [InlineData("2024-02-01", 2024, 2, 1)]
    [InlineData("5 Mar 2022", 2022, 3, 5)]
    [InlineData("15-Aug-2021", 2021, 8, 15)]
    [InlineData("45000", 2023, 3, 15)]            // Excel serial 45000 = 15 Mar 2023
    public void Dates_parse_day_first_by_default(string raw, int y, int m, int d)
    {
        DateRules.TryParse(raw, out var dt).Should().BeTrue();
        dt.Date.Should().Be(new DateTime(y, m, d));
    }

    [Fact]
    public void Month_first_can_be_selected()
    {
        DateRules.TryParse("01/02/2024", out var dt, dayFirst: false).Should().BeTrue();
        dt.Date.Should().Be(new DateTime(2024, 1, 2));
    }

    [Theory]
    [InlineData("31/02/2023")]
    [InlineData("not a date")]
    [InlineData("13/13/2020")]
    [InlineData("")]
    public void Impossible_dates_fail(string raw) => DateRules.TryParse(raw, out _).Should().BeFalse();

    [Fact]
    public void Plausibility_rejects_future_and_ancient() 
    {
        DateRules.IsPlausible(new DateTime(1960, 1, 1)).Should().BeFalse();
        DateRules.IsPlausible(DateTime.UtcNow.AddYears(2)).Should().BeFalse();
        DateRules.IsPlausible(new DateTime(2020, 5, 5)).Should().BeTrue();
    }

    [Theory]
    [InlineData("Infosys Pvt. Ltd.", "infosys")]
    [InlineData("The Tata Group", "tata group")]
    [InlineData("Acme & Sons LLP", "acme and sons")]
    [InlineData("TCS", "tcs")]
    [InlineData("Pvt Ltd", "pvt")]
    public void Company_key(string raw, string key) => CompanyRules.Key(raw).Should().Be(key);

    [Fact]
    public void Company_similarity_ignores_legal_suffix() =>
        CompanyRules.Similarity("Infosys Limited", "INFOSYS Pvt Ltd").Should().Be(1.0);

    [Theory]
    [InlineData("India", "IN")]
    [InlineData("in", "IN")]
    [InlineData("USA", "US")]
    [InlineData("United Kingdom", "GB")]
    [InlineData("uae", "AE")]
    public void Country_lookup(string raw, string iso) => CountryTable.Find(raw)!.Iso2.Should().Be(iso);

    [Fact]
    public void Unknown_country_is_null() => CountryTable.Find("Atlantis").Should().BeNull();
}
