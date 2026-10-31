using CRM_Master.Core.BusinessRules;

namespace CRM_Master.Tests.Core;

public class EmailRulesTests
{
    [Theory]
    [InlineData("  John.Doe@Example-Corp.COM ", "john.doe@example-corp.com")]
    [InlineData("mailto:Sales@Acme.in", "sales@acme.in")]
    [InlineData("<a.b@x.io>", "a.b@x.io")]
    [InlineData("a b @ x.com", "ab@x.com")]
    public void Normalize_lowercases_and_strips_decoration(string raw, string expected) =>
        EmailRules.Normalize(raw).Should().Be(expected);

    [Fact]
    public void Normalize_of_blank_is_null() => EmailRules.Normalize("   ").Should().BeNull();

    [Theory]
    [InlineData("ravi.kumar@acme.co.in")]
    [InlineData("a+tag@sub.domain.example")]
    [InlineData("o'brien@firm.com")]
    [InlineData("x@y.io")]
    public void Valid_addresses_pass(string email) => EmailRules.Check(email).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("plainaddress", "'@'")]
    [InlineData("@no-local.com", "'@'")]
    [InlineData("two@@ats.com", "more than one")]
    [InlineData("john@nodot", "dot")]
    [InlineData("john..doe@x.com", "dots")]
    [InlineData("john@x..com", "consecutive")]
    [InlineData("john@x.c", "valid")]
    public void Invalid_addresses_fail_with_reason(string email, string reason)
    {
        var r = EmailRules.Check(email);
        r.IsValid.Should().BeFalse();
        r.Error.Should().Contain(reason);
    }

    [Fact]
    public void Inner_spaces_are_stripped_before_checking() => EmailRules.Check("jo hn@x.com").Normalized.Should().Be("john@x.com");

    [Fact]
    public void Overlong_local_part_is_invalid() =>
        EmailRules.Check(new string('a', 65) + "@x.com").IsValid.Should().BeFalse();

    [Theory]
    [InlineData("ravi@gmial.com", "ravi@gmail.com")]
    [InlineData("ravi@gmai.com", "ravi@gmail.com")]
    [InlineData("ravi@yahooo.com", "ravi@yahoo.com")]
    [InlineData("ravi@hotmial.com", "ravi@hotmail.com")]
    [InlineData("ravi@acme.con", "ravi@acme.com")]
    public void Common_typos_get_suggestions(string email, string suggestion) =>
        EmailRules.Check(email).SuggestedCorrection.Should().Be(suggestion);

    [Fact]
    public void Correct_popular_domain_gets_no_suggestion() =>
        EmailRules.Check("a@gmail.com").SuggestedCorrection.Should().BeNull();

    [Fact]
    public void Placeholder_disposable_and_role_flags()
    {
        EmailRules.Check("test@test.com").IsPlaceholder.Should().BeTrue();
        EmailRules.Check("x@mailinator.com").IsDisposable.Should().BeTrue();
        EmailRules.Check("info@acme.com").IsRoleBased.Should().BeTrue();
        EmailRules.Check("ravi@acme.com").IsPlaceholder.Should().BeFalse();
    }
}
