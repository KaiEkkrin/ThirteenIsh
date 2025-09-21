

namespace ThirteenIsh.Game.ThirteenthAge;

internal class ThirteenthAgeCharacterSystemBuilder(CharacterType characterType, string name)
    : CharacterSystemBuilder
{
    public override CharacterSystem Build()
    {
        var propertyGroups = ValidatePropertyGroups(name);
        return new ThirteenthAgeCharacterSystem(characterType, name, propertyGroups);
    }
}
