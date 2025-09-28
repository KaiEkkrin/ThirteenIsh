

namespace ThirteenIsh.Game.Swn;

internal class AttackBonusCounter(GameProperty class1Property, GameProperty class2Property, GameCounter levelCounter)
    : GameCounter(SwnSystem.AttackBonus, options: GameCounterOptions.CanRoll)
{
    protected override int? GetValueInternal(ICharacterBase character)
    {
        return GetAttackBonus(
            levelCounter.GetValue(character),
            class1Property.GetValue(character),
            class2Property.GetValue(character));
    }

    private static int? GetAttackBonus(int? level, string class1, string class2)
    {
        if (level == null) return null;
        var attackBonus = (level, class1, class2) switch
        {
            (_, SwnSystem.Warrior, SwnSystem.Warrior) => level,
            (_, SwnSystem.Warrior, _) => level / 2 + GetWarriorBonus(level.Value),
            (_, _, SwnSystem.Warrior) => level / 2 + GetWarriorBonus(level.Value),
            _ => level / 2
        };

        return attackBonus;
    }

    private static int GetWarriorBonus(int level)
    {
        // Gain +1 to attack bonus at first and fifth levels
        return level >= 5 ? 2 : 1;
    }
}
