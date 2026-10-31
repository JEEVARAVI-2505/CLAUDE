using LeadPilot.Application.Text;

namespace LeadPilot.Tests;

public class TextTests
{
    [Theory]
    [InlineData("+91 98765 43210", "+919876543210")]
    [InlineData("098765 43210", "+919876543210")]
    [InlineData("+1 (415) 555-2671", "+14155552671")]
    public void Phones_normalise_to_e164(string raw, string expected) =>
        Normalizer.NormalizePhone(raw, "IN").Should().Be(expected);

    [Theory]
    [InlineData("12345")]
    [InlineData("call us")]
    [InlineData("")]
    public void Invalid_phones_are_rejected(string raw) =>
        Normalizer.ParsePhone(raw, "IN").IsValid.Should().BeFalse();

    [Theory]
    [InlineData("sales@abc-industries.com", true)]
    [InlineData("A.B+tag@sub.example.co.in", true)]
    [InlineData("no-at-sign.com", false)]
    [InlineData("two@@example.com", false)]
    [InlineData("dots..bad@example.com", false)]
    [InlineData("user@nodot", false)]
    [InlineData("user@example.c", false)]
    [InlineData("", false)]
    public void Email_format(string email, bool valid) => Normalizer.IsValidEmailFormat(email).Should().Be(valid);

    [Fact]
    public void Email_is_lowercased() => Normalizer.NormalizeEmail("  Sales@ABC.com ").Should().Be("sales@abc.com");

    [Theory]
    [InlineData("https://www.ABC-Industries.com/contact?x=1", "abc-industries.com")]
    [InlineData("abc.co.in", "abc.co.in")]
    [InlineData("www.foo.in", "foo.in")]
    public void Domain_extraction(string url, string domain) => Normalizer.DomainOf(url).Should().Be(domain);

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://x.com")]
    [InlineData("localhost")]
    public void Bad_urls(string url) => Normalizer.NormalizeUrl(url).Should().BeNull();

    [Fact]
    public void Same_site_ignores_www_and_subdomains()
    {
        Normalizer.SameSite("https://www.abc.com", "http://abc.com/x").Should().BeTrue();
        Normalizer.SameSite("https://shop.abc.com", "abc.com").Should().BeTrue();
        Normalizer.SameSite("abc.com", "abcd.com").Should().BeFalse();
    }

    [Theory]
    [InlineData("ABC Industries Pvt. Ltd.", "abc industries")]
    [InlineData("Müller & Söhne GmbH", "muller sohne")]
    [InlineData("The Company", "the company")] // only suffix tokens -> fall back to full name
    public void Core_name(string name, string core) => Normalizer.CoreName(name).Should().Be(core);

    [Fact]
    public void Jaro_winkler_known_values()
    {
        // Classic reference pair: MARTHA / MARHTA = 0.9611
        StringSimilarity.JaroWinkler("martha", "marhta").Should().BeApproximately(0.9611, 0.0005);
        StringSimilarity.JaroWinkler("abc", "abc").Should().Be(1);
        StringSimilarity.JaroWinkler("abc", "xyz").Should().Be(0);
    }

    [Fact]
    public void Name_similarity_ignores_legal_suffix() =>
        StringSimilarity.NameSimilarity("ABC Industries Pvt Ltd", "A B C Industries").Should().BeGreaterThan(0.8);
}
