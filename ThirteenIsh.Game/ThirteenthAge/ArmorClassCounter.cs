namespace ThirteenIsh.Game.ThirteenthAge;

internal class ArmorClassCounter(
    GameProperty classProperty,
    GameCounter levelCounter,
    AbilityBonusCounter constitutionBonusCounter,
    AbilityBonusCounter dexterityBonusCounter,
    AbilityBonusCounter wisdomBonusCounter)
    : ClassBasedCounter(ThirteenthAgeSystem.ArmorClass, ThirteenthAgeSystem.ArmorClassAlias, classProperty)
{
    protected override int? GetValueInternal(string? classValue, Func<GameCounter, int?> getCounterValue)
    {
        int? baseAC = classValue switch
        {
            ThirteenthAgeSystem.Barbarian => 12,
            ThirteenthAgeSystem.Bard => 12,
            ThirteenthAgeSystem.Cleric => 14,
            ThirteenthAgeSystem.ChaosMage => 10,
            ThirteenthAgeSystem.Commander => 12,
            ThirteenthAgeSystem.Druid => 10,
            ThirteenthAgeSystem.Fighter => 15,
            ThirteenthAgeSystem.Monk => 11,
            ThirteenthAgeSystem.Necromancer => 10,
            ThirteenthAgeSystem.Occultist => 11,
            ThirteenthAgeSystem.Paladin => 16,
            ThirteenthAgeSystem.Ranger => 14,
            ThirteenthAgeSystem.Rogue => 12,
            ThirteenthAgeSystem.Sorcerer => 10,
            ThirteenthAgeSystem.Wizard => 10,
            _ => null
        };

        if (!baseAC.HasValue) return null;
        var bonuses = new List<int?>
        {
            getCounterValue(constitutionBonusCounter),
            getCounterValue(dexterityBonusCounter),
            getCounterValue(wisdomBonusCounter)
        };

        bonuses.Sort();
        return baseAC + bonuses[1] + getCounterValue(levelCounter);
    }
}
