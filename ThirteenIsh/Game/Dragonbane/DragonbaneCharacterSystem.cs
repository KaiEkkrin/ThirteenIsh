using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Dragonbane;

internal class DragonbaneCharacterSystem(CharacterType characterType, string gameSystemName,
    ImmutableList<GamePropertyGroup> propertyGroups) : CharacterSystem(characterType, gameSystemName, propertyGroups)
{
    protected override GameCounter BuildCustomCounter(CustomCounter cc)
    {
        // TODO fill this in, making a DragonbaneCustomCounter type
        throw new NotImplementedException();
    }
}
