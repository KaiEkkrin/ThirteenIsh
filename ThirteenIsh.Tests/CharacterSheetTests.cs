using Shouldly;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Tests;

public class CharacterSheetTests
{
    private readonly CharacterSheet _sheet = new()
    {
        AbilityScores = new Dictionary<string, int>
        {
            ["Strength"] = 19,
            ["Dexterity"] = 11,
            ["Constitution"] = 7,
            ["Intelligence"] = 8,
            ["Wisdom"] = 10,
            ["Charisma"] = 9
        }
    };

    [Theory]
    [InlineData("Strength", 4)]
    [InlineData("Dexterity", 0)]
    [InlineData("Constitution", -2)]
    [InlineData("Intelligence", -1)]
    [InlineData("Wisdom", 0)]
    [InlineData("Charisma", -1)]
    public void AbilityModifiersAreCorrect(string ability, int expectedModifier)
    {
        var modifier = _sheet.GetAbilityModifier(ability);
        modifier.ShouldBe(expectedModifier, $"{ability} score: {_sheet.AbilityScores[ability]}");
    }
}
