using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class ArmorClassCounter(GameCounter armorValueCounter, AttributeBonusCounter dexterity)
    : GameCounter(SwnSystem.ArmorClass, SwnSystem.ArmorClassAlias)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        if (sheet is not CharacterSheet characterSheet) return null;
        return armorValueCounter.GetValue(sheet) + dexterity.GetValue(sheet);
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        return armorValueCounter.GetValue(character) + dexterity.GetValue(character);
    }
}
