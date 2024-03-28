using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class HitPointsCounter(
    GameProperty classProperty,
    GameCounter levelCounter,
    AbilityBonusCounter constitutionBonusCounter)
    : GameCounter(ThirteenthAgeSystem.HitPoints, ThirteenthAgeSystem.HitPointsAlias,
        options: GameCounterOptions.HasVariable)
{
    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        // See page 31, 76 and onwards. Why is this so baroque?!
        var conBonus = constitutionBonusCounter.GetValue(characterSheet);
        var classValue = classProperty.GetValue(characterSheet);
        var level = levelCounter.GetValue(characterSheet);

        int? baseValue = conBonus + classValue switch
        {
            ThirteenthAgeSystem.Barbarian => 7,
            ThirteenthAgeSystem.Bard => 7,
            ThirteenthAgeSystem.Cleric => 7,
            ThirteenthAgeSystem.Fighter => 8,
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
