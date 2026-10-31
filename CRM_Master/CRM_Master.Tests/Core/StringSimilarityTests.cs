using CRM_Master.Core.BusinessRules;

namespace CRM_Master.Tests.Core;

public class StringSimilarityTests
{
    // Hand-calculated: kitten -> sitting needs k->s, e->i, +g = 3 edits (classic Levenshtein example).
    [Fact] public void Levenshtein_kitten_sitting_is_3() => StringSimilarity.Levenshtein("kitten", "sitting").Should().Be(3);
    [Fact] public void Levenshtein_empty_cases() { StringSimilarity.Levenshtein("", "abc").Should().Be(3); StringSimilarity.Levenshtein("abc", "").Should().Be(3); }
    [Fact] public void Osa_counts_transposition_as_one() { StringSimilarity.OsaDistance("gmial", "gmail").Should().Be(1); StringSimilarity.Levenshtein("gmial", "gmail").Should().Be(2); }
    [Fact] public void Levenshtein_ratio_golden() => StringSimilarity.LevenshteinRatio("kitten", "sitting").Should().BeApproximately(1 - 3.0 / 7.0, 1e-9);

    // Jaro-Winkler reference values (Winkler 1990): MARTHA/MARHTA = 0.9611, DWAYNE/DUANE = 0.84, DIXON/DICKSONX = 0.8133
    [Fact] public void JaroWinkler_martha_marhta() => StringSimilarity.JaroWinkler("MARTHA", "MARHTA").Should().BeApproximately(0.9611, 1e-4);
    [Fact] public void JaroWinkler_dwayne_duane() => StringSimilarity.JaroWinkler("DWAYNE", "DUANE").Should().BeApproximately(0.84, 1e-4);
    [Fact] public void JaroWinkler_dixon_dicksonx() => StringSimilarity.JaroWinkler("DIXON", "DICKSONX").Should().BeApproximately(0.8133, 1e-4);
    [Fact] public void Jaro_martha_marhta() => StringSimilarity.Jaro("MARTHA", "MARHTA").Should().BeApproximately(0.9444, 1e-4);
    [Fact] public void Jaro_identical_and_disjoint() { StringSimilarity.Jaro("abc", "abc").Should().Be(1); StringSimilarity.Jaro("abc", "xyz").Should().Be(0); }

    [Theory]
    [InlineData("Robert", "R163")]
    [InlineData("Rupert", "R163")]
    [InlineData("Tymczak", "T522")]
    [InlineData("Pfister", "P236")]
    [InlineData("Ashcraft", "A261")]
    [InlineData("Lee", "L000")]
    public void Soundex_reference_values(string name, string code) => StringSimilarity.Soundex(name).Should().Be(code);

    [Fact] public void NormalizeText_strips_accents_and_punctuation() => StringSimilarity.NormalizeText("  José  O'Neil-Smith, Jr. ").Should().Be("jose o neil smith jr");

    [Fact] public void Name_similarity_exact_and_reordered() { StringSimilarity.NameSimilarity("John Smith", "john  SMITH").Should().Be(1); StringSimilarity.NameSimilarity("Smith John", "John Smith").Should().BeGreaterThan(0.99); }
    [Fact] public void Name_similarity_tolerates_spelling() => StringSimilarity.NameSimilarity("Jon Smith", "John Smith").Should().BeGreaterThan(0.9);
    [Fact] public void Name_similarity_handles_initials() => StringSimilarity.NameSimilarity("J Smith", "John Smith").Should().BeGreaterThan(0.85);
    [Fact] public void Name_similarity_of_different_people_is_low() => StringSimilarity.NameSimilarity("John Smith", "Priya Patel").Should().BeLessThan(0.6);
    [Fact] public void Name_similarity_with_blank_is_zero() => StringSimilarity.NameSimilarity("", "John").Should().Be(0);
}
