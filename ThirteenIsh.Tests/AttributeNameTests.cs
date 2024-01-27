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
}
