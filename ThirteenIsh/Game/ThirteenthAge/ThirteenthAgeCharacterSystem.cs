using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class ThirteenthAgeCharacterSystem(CharacterType characterType, string gameSystemName,
    ImmutableList<GamePropertyGroup> propertyGroups) : CharacterSystem(characterType, gameSystemName, propertyGroups)
{
    protected override GameCounter BuildCustomCounter(CustomCounter cc)
    {
        return new ThirteenthAgeCustomCounter(cc);
    }
}
