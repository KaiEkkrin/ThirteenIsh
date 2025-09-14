using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class AttackBonusCounter(GameProperty class1Property, GameProperty class2Property, GameCounter levelCounter)
    : GameCounter(SwnSystem.AttackBonus, options: GameCounterOptions.CanRoll)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        if (sheet is not CharacterSheet characterSheet) return null;
        return GetAttackBonus(
            levelCounter.GetValue(sheet),
            class1Property.GetValue(characterSheet),
            class2Property.GetValue(characterSheet));
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        return GetAttackBonus(
            levelCounter.GetValue(character),
            class1Property.GetValue(character.Sheet),
            class2Property.GetValue(character.Sheet));
    }

    private static int? GetAttackBonus(int? level, string class1, string class2)
    {
        if (level == null) return null;
        var attackBonus = (level, class1, class2) switch
        {
            (_, SwnSystem.Warrior, SwnSystem.Warrior) => level,
            ( < 5, SwnSystem.Warrior, _) => 1 + level / 2,
            ( < 5, _, SwnSystem.Warrior) => 1 + level / 2,
            (_, SwnSystem.Warrior, _) => 2 + level / 2,
            (_, _, SwnSystem.Warrior) => 2 + level / 2,
            _ => level / 2
        };

        return attackBonus;
    }
}
