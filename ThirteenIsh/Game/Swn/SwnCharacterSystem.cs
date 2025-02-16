using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class SwnCharacterSystem(ImmutableList<GamePropertyGroup> propertyGroups)
    : CharacterSystem(CharacterType.PlayerCharacter, SwnSystem.SystemName, propertyGroups)
{
    protected override GameCounter BuildCustomCounter(CustomCounter cc)
    {
        throw new NotImplementedException();
    }
}
