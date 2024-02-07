using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class InitiativeCounter(
    GameCounter levelCounter,
    AbilityBonusCounter dexterityBonusCounter)
    : GameCounter("Initiative", "Init", ThirteenthAgeSystem.General)
{
    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        var level = levelCounter.GetValue(characterSheet);
        var dexterityBonus = dexterityBonusCounter.GetValue(characterSheet);
        return dexterityBonus + level;
    }
}
