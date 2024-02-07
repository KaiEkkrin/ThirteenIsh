using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class MentalDefenseCounter(
    GameProperty classProperty,
    GameCounter levelCounter,
    AbilityBonusCounter intelligenceBonusCounter,
    AbilityBonusCounter wisdomBonusCounter,
    AbilityBonusCounter charismaBonusCounter)
    : GameCounter("Mental Defense", "MD", ThirteenthAgeSystem.General)
{
    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        int? baseMD = classProperty.GetValue(characterSheet) switch
        {
            ThirteenthAgeSystem.Barbarian => 10,
            ThirteenthAgeSystem.Bard => 11,
            ThirteenthAgeSystem.Cleric => 11,
            ThirteenthAgeSystem.Fighter => 10,
            ThirteenthAgeSystem.Paladin => 12,
            ThirteenthAgeSystem.Ranger => 10,
            ThirteenthAgeSystem.Rogue => 10,
            ThirteenthAgeSystem.Sorcerer => 10,
            ThirteenthAgeSystem.Wizard => 12,
            _ => null
        };

        if (!baseMD.HasValue) return null;
        var bonuses = new List<int?>
        {
            intelligenceBonusCounter.GetValue(characterSheet),
            wisdomBonusCounter.GetValue(characterSheet),
            charismaBonusCounter.GetValue(characterSheet)
        };

        bonuses.Sort();
        return baseMD + bonuses[1] + levelCounter.GetValue(characterSheet);
    }
}
