

namespace ThirteenIsh.Game.Dragonbane;

internal class DragonbaneCharacterSystemBuilder(string characterSystemName, string gameSystemName,
    CharacterTypeCompatibility compatibility, CharacterType? defaultForType)
    : CharacterSystemBuilder
{
    public override CharacterSystem Build()
    {
        var propertyGroups = ValidatePropertyGroups(gameSystemName);
        return new DragonbaneCharacterSystem(characterSystemName, gameSystemName, compatibility, defaultForType, propertyGroups);
    }
}
