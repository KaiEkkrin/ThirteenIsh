

namespace ThirteenIsh.Game.ThirteenthAge;

internal class ThirteenthAgeCharacterSystemBuilder(string characterSystemName, string gameSystemName,
    CharacterTypeCompatibility compatibility, CharacterType? defaultForType)
    : CharacterSystemBuilder
{
    public override CharacterSystem Build()
    {
        var propertyGroups = ValidatePropertyGroups(gameSystemName);
        return new ThirteenthAgeCharacterSystem(characterSystemName, gameSystemName, compatibility, defaultForType, propertyGroups);
    }
}
