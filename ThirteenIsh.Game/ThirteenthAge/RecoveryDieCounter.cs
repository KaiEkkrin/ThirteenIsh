using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class RecoveryDieCounter(GameProperty classProperty) : GameCounter("Recovery Die")
{
    public override bool CanStore => false;

    public override int? GetValue(ICounterSheet sheet)
    {
        if (sheet is not CharacterSheet characterSheet) return null;
        return classProperty.GetValue(characterSheet) switch
        {
            ThirteenthAgeSystem.Barbarian => 10,
            ThirteenthAgeSystem.Bard => 8,
            ThirteenthAgeSystem.Cleric => 8,
            ThirteenthAgeSystem.ChaosMage => 6,
            ThirteenthAgeSystem.Commander => 8,
            ThirteenthAgeSystem.Druid => 6,
            ThirteenthAgeSystem.Fighter => 10,
            ThirteenthAgeSystem.Monk => 8,
            ThirteenthAgeSystem.Necromancer => 6,
            ThirteenthAgeSystem.Occultist => 6,
            ThirteenthAgeSystem.Paladin => 10,
            ThirteenthAgeSystem.Ranger => 8,
            ThirteenthAgeSystem.Rogue => 8,
            ThirteenthAgeSystem.Sorcerer => 6,
            ThirteenthAgeSystem.Wizard => 6,
            _ => null
        };
    }
}
