﻿using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class ArmorClassCounter(
    GameProperty classProperty,
    GameCounter levelCounter,
    AbilityBonusCounter constitutionBonusCounter,
    AbilityBonusCounter dexterityBonusCounter,
    AbilityBonusCounter wisdomBonusCounter)
    : GameCounter(ThirteenthAgeSystem.ArmorClass, ThirteenthAgeSystem.ArmorClassAlias)
{
    public override bool CanStore => false;

    public override int? GetValue(CounterSheet sheet)
    {
        if (sheet is not CharacterSheet characterSheet) return null;
        int? baseAC = classProperty.GetValue(characterSheet) switch
        {
            ThirteenthAgeSystem.Barbarian => 12,
            ThirteenthAgeSystem.Bard => 12,
            ThirteenthAgeSystem.Cleric => 14,
            ThirteenthAgeSystem.Fighter => 15,
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
            constitutionBonusCounter.GetValue(sheet),
            dexterityBonusCounter.GetValue(sheet),
            wisdomBonusCounter.GetValue(sheet)
        };

        bonuses.Sort();
        return baseAC + bonuses[1] + levelCounter.GetValue(sheet);
    }
}
