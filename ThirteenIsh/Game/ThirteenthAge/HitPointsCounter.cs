using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class HitPointsCounter(
    GameProperty classProperty,
    GameCounter levelCounter,
    AbilityBonusCounter constitutionBonusCounter)
    : ClassBasedCounter(ThirteenthAgeSystem.HitPoints, ThirteenthAgeSystem.HitPointsAlias, classProperty,
        GameCounterOptions.HasVariable)
{
    protected override int? GetValueInternal(string? classValue, Func<GameCounter, int?> getCounterValue)
    {
        // See page 31, 76 and onwards. Why is this so baroque?!
        var conBonus = getCounterValue(constitutionBonusCounter);
        var level = getCounterValue(levelCounter);

        int? baseValue = conBonus + classValue switch
        {
            ThirteenthAgeSystem.Barbarian => 7,
            ThirteenthAgeSystem.Bard => 7,
            ThirteenthAgeSystem.Cleric => 7,
            ThirteenthAgeSystem.ChaosMage => 6,
            ThirteenthAgeSystem.Commander => 7,
            ThirteenthAgeSystem.Druid => 6,
            ThirteenthAgeSystem.Fighter => 8,
            ThirteenthAgeSystem.Monk => 7,
            ThirteenthAgeSystem.Necromancer => 6,
            ThirteenthAgeSystem.Occultist => 6,
            ThirteenthAgeSystem.Paladin => 8,
            ThirteenthAgeSystem.Ranger => 7,
            ThirteenthAgeSystem.Rogue => 6,
            ThirteenthAgeSystem.Sorcerer => 6,
            ThirteenthAgeSystem.Wizard => 6,
            _ => null
        };

        if (!baseValue.HasValue) return null;
        int? value = level switch
        {
            1 => baseValue * 3,
            2 => baseValue * 4,
            3 => baseValue * 5,
            4 => baseValue * 6,
            5 => baseValue * 8,
            6 => baseValue * 10,
            7 => baseValue * 12,
            8 => baseValue * 16,
            9 => baseValue * 20,
            10 => baseValue * 24,
            _ => null
        };

        return value;
    }
}
