using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

// TODO should be able to roll this
internal class InitiativeCounter(
    GameCounter levelCounter,
    AbilityBonusCounter dexterityBonusCounter)
    : GameCounter("Initiative", "Init")
{
    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        var level = levelCounter.GetValue(characterSheet);
        var dexterityBonus = dexterityBonusCounter.GetValue(characterSheet);
        return dexterityBonus + level;
    }
}
