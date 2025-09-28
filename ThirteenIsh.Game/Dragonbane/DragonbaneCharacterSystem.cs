

namespace ThirteenIsh.Game.Dragonbane;

internal class DragonbaneCharacterSystem(string name, string gameSystemName, CharacterTypeCompatibility compatibility,
    CharacterType? defaultForType, ImmutableList<GamePropertyGroup> propertyGroups)
    : CharacterSystem(name, gameSystemName, compatibility, defaultForType, propertyGroups)
{
    protected override GameCounter BuildCustomCounter(CustomCounter cc)
    {
        // TODO fill this in, making a DragonbaneCustomCounter type
        throw new NotImplementedException();
    }
}
