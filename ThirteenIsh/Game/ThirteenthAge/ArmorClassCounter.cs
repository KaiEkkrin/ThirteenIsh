using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class ArmorClassCounter(
    GameProperty classProperty,
    GameCounter levelCounter,
    AbilityBonusCounter constitutionBonusCounter,
    AbilityBonusCounter dexterityBonusCounter,
    AbilityBonusCounter wisdomBonusCounter)
    : GameCounter("Armor Class")
{
    public override bool CanStore => false;

    public override int GetValue(CharacterSheet characterSheet)
    {
        var baseAC = classProperty.GetValue(characterSheet) switch
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
            var c => throw new InvalidOperationException($"Unrecognised class: {c}")
        };

        var bonuses = new List<int>
        {
            constitutionBonusCounter.GetValue(characterSheet),
            dexterityBonusCounter.GetValue(characterSheet),
            wisdomBonusCounter.GetValue(characterSheet)
        };

        bonuses.Sort();
        return baseAC + bonuses[1] + levelCounter.GetValue(characterSheet);
    }
}
