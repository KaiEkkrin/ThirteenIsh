using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Dragonbane;

internal class DragonbaneCharacterSystemBuilder(CharacterType characterType, string name)
    : CharacterSystemBuilder
{
    public override CharacterSystem Build()
    {
        var propertyGroups = ValidatePropertyGroups(name);
        return new DragonbaneCharacterSystem(characterType, name, propertyGroups);
    }
}
