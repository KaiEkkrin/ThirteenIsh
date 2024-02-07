using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class RecoveryDieCounter(GameProperty classProperty) : GameCounter("Recovery Die", category: ThirteenthAgeSystem.General)
{
    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        return classProperty.GetValue(characterSheet) switch
        {
            ThirteenthAgeSystem.Barbarian => 10,
            ThirteenthAgeSystem.Bard => 8,
            ThirteenthAgeSystem.Cleric => 8,
            ThirteenthAgeSystem.Fighter => 10,
            ThirteenthAgeSystem.Paladin => 10,
            ThirteenthAgeSystem.Ranger => 8,
            ThirteenthAgeSystem.Rogue => 8,
            ThirteenthAgeSystem.Sorcerer => 6,
            ThirteenthAgeSystem.Wizard => 6,
            _ => null
        };
    }
}
