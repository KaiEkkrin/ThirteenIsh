using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.Tests.Game;

public class CharacterTypeExtensionsTests
{
    [Theory]
    [InlineData(CharacterType.PlayerCharacter, FriendlyNameOptions.None, "character")]
    [InlineData(CharacterType.Monster, FriendlyNameOptions.None, "monster")]
    public void FriendlyName_NoOptions_ReturnsBaseName(CharacterType characterType, FriendlyNameOptions options, string expected)
    {
        var result = characterType.FriendlyName(options);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(CharacterType.PlayerCharacter, "Character")]
    [InlineData(CharacterType.Monster, "Monster")]
    public void FriendlyName_CapitalizeFirstCharacter_CapitalizesCorrectly(CharacterType characterType, string expected)
    {
        var result = characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(CharacterType.PlayerCharacter, "characters")]
    [InlineData(CharacterType.Monster, "monsters")]
    public void FriendlyName_Plural_AddsS(CharacterType characterType, string expected)
    {
        var result = characterType.FriendlyName(FriendlyNameOptions.Plural);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(CharacterType.PlayerCharacter, "Characters")]
    [InlineData(CharacterType.Monster, "Monsters")]
    public void FriendlyName_CapitalizeAndPlural_AppliesBothOptions(CharacterType characterType, string expected)
    {
        var result = characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter | FriendlyNameOptions.Plural);
        result.ShouldBe(expected);
    }

    [Fact]
    public void FriendlyName_AllCharacterTypes_DoNotThrow()
    {
        foreach (CharacterType characterType in Enum.GetValues<CharacterType>())
        {
            Should.NotThrow(() => characterType.FriendlyName());
        }
    }

    [Fact]
    public void FriendlyName_AllOptionCombinations_DoNotThrow()
    {
        var allOptions = new[]
        {
            FriendlyNameOptions.None,
            FriendlyNameOptions.CapitalizeFirstCharacter,
            FriendlyNameOptions.Plural,
            FriendlyNameOptions.CapitalizeFirstCharacter | FriendlyNameOptions.Plural
        };

        foreach (var characterType in Enum.GetValues<CharacterType>())
        {
            foreach (var options in allOptions)
            {
                Should.NotThrow(() => characterType.FriendlyName(options));
            }
        }
    }
}