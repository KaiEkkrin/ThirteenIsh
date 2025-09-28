

namespace ThirteenIsh.Game.Swn;

internal class ArmorClassCounter(GameCounter armorValueCounter, AttributeBonusCounter dexterity)
    : GameCounter(SwnSystem.ArmorClass, SwnSystem.ArmorClassAlias)
{
    protected override int? GetValueInternal(ICharacterBase character)
    {
        return armorValueCounter.GetValue(character) + dexterity.GetValue(character);
    }
}
