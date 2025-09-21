namespace ThirteenIsh.Game.ThirteenthAge;

internal class PhysicalDefenseCounter(
    GameProperty classProperty,
    GameCounter levelCounter,
    AbilityBonusCounter strengthBonusCounter,
    AbilityBonusCounter constitutionBonusCounter,
    AbilityBonusCounter dexterityBonusCounter)
    : ClassBasedCounter(ThirteenthAgeSystem.PhysicalDefense, ThirteenthAgeSystem.PhysicalDefenseAlias,
        classProperty)
{
    protected override int? GetValueInternal(string? classValue, Func<GameCounter, int?> getCounterValue)
    {
        int? basePD = classValue switch
        {
            ThirteenthAgeSystem.Barbarian => 11,
            ThirteenthAgeSystem.Bard => 10,
            ThirteenthAgeSystem.Cleric => 11,
            ThirteenthAgeSystem.ChaosMage => 10,
            ThirteenthAgeSystem.Commander => 10,
            ThirteenthAgeSystem.Druid => 11,
            ThirteenthAgeSystem.Fighter => 10,
            ThirteenthAgeSystem.Monk => 11,
            ThirteenthAgeSystem.Necromancer => 10,
            ThirteenthAgeSystem.Occultist => 10,
            ThirteenthAgeSystem.Paladin => 10,
            ThirteenthAgeSystem.Ranger => 11,
            ThirteenthAgeSystem.Rogue => 12,
            ThirteenthAgeSystem.Sorcerer => 11,
            ThirteenthAgeSystem.Wizard => 10,
            _ => null
        };

        if (!basePD.HasValue) return null;
        var bonuses = new List<int?>
        {
            getCounterValue(strengthBonusCounter),
            getCounterValue(constitutionBonusCounter),
            getCounterValue(dexterityBonusCounter)
        };

        bonuses.Sort();
        return basePD + bonuses[1] + getCounterValue(levelCounter);
    }
}
