using CRM_Master.Core.BusinessRules;

namespace CRM_Master.Tests.Core;

public class PhoneNumberParserTests
{
    [Theory]
    [InlineData("9876543210", "+919876543210", "+91 98765 43210")]
    [InlineData("98765 43210", "+919876543210", "+91 98765 43210")]
    [InlineData("+91-98765-43210", "+919876543210", "+91 98765 43210")]
    [InlineData("091 9876543210", "+919876543210", "+91 98765 43210")]
    [InlineData("09876543210", "+919876543210", "+91 98765 43210")]
    [InlineData("919876543210", "+919876543210", "+91 98765 43210")]
    [InlineData("0091 98765 43210", "+919876543210", "+91 98765 43210")]
    [InlineData("(+91) 98765.43210", "+919876543210", "+91 98765 43210")]
    public void Indian_mobile_variants_normalise_to_same_number(string raw, string e164, string formatted)
    {
        var r = PhoneNumberParser.Parse(raw);
        r.IsValid.Should().BeTrue(r.Error);
        r.E164.Should().Be(e164);
        r.Formatted.Should().Be(formatted);
        r.Kind.Should().Be(PhoneKind.Mobile);
        r.Key.Should().Be("919876543210");
    }

    [Fact]
    public void Excel_scientific_notation_is_expanded()
    {
        var r = PhoneNumberParser.Parse("9.876543210E+09");
        r.IsValid.Should().BeTrue();
        r.E164.Should().Be("+919876543210");
    }

    [Fact]
    public void Indian_landline_with_std_code_is_valid_landline()
    {
        var r = PhoneNumberParser.Parse("011 2345 6789"); // 0 + 10 digits
        r.IsValid.Should().BeTrue();
        r.Kind.Should().Be(PhoneKind.Landline);
        r.E164.Should().Be("+911123456789");
    }

    [Fact]
    public void Extension_is_split_off()
    {
        var r = PhoneNumberParser.Parse("98765 43210 ext 204");
        r.IsValid.Should().BeTrue();
        r.Extension.Should().Be("204");
    }

    [Theory]
    [InlineData("12345", "Too short")]
    [InlineData("98765", "Too short")]
    [InlineData("0123456789", "cannot start with 0")]
    [InlineData("123456789012345", "Too long")]
    [InlineData("call me", "letters")]
    [InlineData("98765-43210abc", "letters")]
    public void Invalid_numbers_are_rejected_with_reason(string raw, string reasonContains)
    {
        var r = PhoneNumberParser.Parse(raw);
        r.IsValid.Should().BeFalse();
        r.Error.Should().Contain(reasonContains);
    }

    [Fact]
    public void Indian_number_must_have_10_digits_and_nine_digit_is_invalid()
    {
        PhoneNumberParser.Parse("987654321").IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_is_invalid_with_Empty_error()
    {
        PhoneNumberParser.Parse("  ").Error.Should().Be("Empty");
        PhoneNumberParser.Parse(null).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("9999999999")]
    [InlineData("1234567890")]
    [InlineData("9876543210")]
    [InlineData("9090909090")]
    public void Placeholder_like_numbers_are_flagged_suspicious_but_syntactically_valid(string raw)
    {
        var r = PhoneNumberParser.Parse(raw);
        r.IsValid.Should().BeTrue();
        r.IsSuspicious.Should().BeTrue();
    }

    [Fact]
    public void Real_looking_number_is_not_suspicious() =>
        PhoneNumberParser.Parse("9812345670").IsSuspicious.Should().BeFalse();

    [Fact]
    public void International_numbers_use_calling_code_table()
    {
        var us = PhoneNumberParser.Parse("+1 (415) 555-2671");
        us.IsValid.Should().BeTrue(us.Error);
        us.Formatted.Should().Be("+1 415 555 2671");
        var uk = PhoneNumberParser.Parse("+44 (0) 7911 123456");
        uk.IsValid.Should().BeTrue(uk.Error);
        uk.E164.Should().Be("+447911123456");
        PhoneNumberParser.Parse("+999 123456789").IsValid.Should().BeFalse();
    }

    [Fact]
    public void North_american_area_code_cannot_start_with_zero_or_one() =>
        PhoneNumberParser.Parse("+1 015 555 2671").IsValid.Should().BeFalse();

    [Fact]
    public void Default_country_code_other_than_india_is_applied()
    {
        var r = PhoneNumberParser.Parse("07911 123456", "44");
        r.IsValid.Should().BeTrue();
        r.E164.Should().Be("+447911123456");
    }

    [Fact]
    public void Same_number_written_differently_yields_identical_keys()
    {
        var keys = new[] { "98765 43210", "+91 9876543210", "09876543210" }.Select(p => ContactKeys.PhoneKey(p, "91")).Distinct();
        keys.Should().ContainSingle().Which.Should().Be("919876543210");
    }
}
