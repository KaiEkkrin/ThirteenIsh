using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class PhysicalDefenseCounter(
    GameProperty classProperty,
    GameCounter levelCounter,
    AbilityBonusCounter strengthBonusCounter,
    AbilityBonusCounter constitutionBonusCounter,
    AbilityBonusCounter dexterityBonusCounter)
    : GameCounter(ThirteenthAgeSystem.PhysicalDefense, ThirteenthAgeSystem.PhysicalDefenseAlias)
{
    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        int? basePD = classProperty.GetValue(characterSheet) switch
        {
            ThirteenthAgeSystem.Barbarian => 11,
            ThirteenthAgeSystem.Bard => 10,
            ThirteenthAgeSystem.Cleric => 11,
            ThirteenthAgeSystem.Fighter => 10,
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
            strengthBonusCounter.GetValue(characterSheet),
            constitutionBonusCounter.GetValue(characterSheet),
            dexterityBonusCounter.GetValue(characterSheet)
        };

        bonuses.Sort();
        return basePD + bonuses[1] + levelCounter.GetValue(characterSheet);
    }
}
