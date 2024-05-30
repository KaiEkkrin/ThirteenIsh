using Shouldly;
using ThirteenIsh.Game;

namespace ThirteenIsh.Tests;

public class AttributeNameTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("str", "Str")]
    [InlineData("Strength", "Strength")]
    [InlineData("STRENGTH", "Strength")]
    [InlineData("  s TRe nG  T h ", "Strength")]
    public void AbilityScoreNamesAreCanonicalizedSuccessfully(string name, string expectedName)
    {
        AttributeName.TryCanonicalize(name, out var canonicalizedName).ShouldBeTrue(name);
        canonicalizedName.ShouldBe(expectedName);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("Str_ength")]
    [InlineData("Count von Dr@cula")]
    [InlineData("Count v0n Dracula")]
    public void InvalidAbilityScoreNamesAreRejected(string name)
    {
        AttributeName.TryCanonicalize(name, out _).ShouldBeFalse(name);
        AttributeName.TryCanonicalizeMultiPart(name, out _).ShouldBeFalse(name);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("str", "Str")]
    [InlineData("STRENGTH", "Strength")]
    [InlineData("dravOr\ttHE  THIRD ", "Dravor The Third")]
    [InlineData("   dravOr\ttHE  THIRD  ", "Dravor The Third")]
    [InlineData("count von dracula", "Count Von Dracula")]
    [InlineData("count v dRACULA", "Count V Dracula")]
    public void MultiPartAbilityScoreNamesAreCanonicalizedSuccessfully(string name, string expectedName)
    {
        AttributeName.TryCanonicalizeMultiPart(name, out var canonicalizedName).ShouldBeTrue(name);
        canonicalizedName.ShouldBe(expectedName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    [InlineData("12345")]
    [InlineData("A@! B")]
    public void InvalidTagIsRejected(string value)
    {
        AttributeName.TryCanonicalizeTag(value, out _).ShouldBeFalse(value);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("fish", "fish")]
    [InlineData("   fish\r\n ", "fish")]
    [InlineData("fish fish fish", "fish fish fish")]
    [InlineData("fish    fish\nfish ", "fish fish fish")]
    [InlineData("A12345", "A12345")]
    [InlineData("Persist  42", "Persist 42")]
    public void ValidTagIsCanonicalized(string value, string expectedTagValue)
    {
        AttributeName.TryCanonicalizeTag(value, out var actualTagValue).ShouldBeTrue(value);
        actualTagValue.ShouldBe(expectedTagValue);
    }
}
