using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.Dragonbane;

/// <summary>
/// This is the size of the bonus damage die rolled with weapons that use the
/// matching ability counter.
/// </summary>
internal class DamageBonusCounter(string name, GameAbilityCounter abilityCounter)
    : GameCounter(name)
{
    public override bool CanStore => false;

    public override int GetValue(CharacterSheet characterSheet)
    {
        return abilityCounter.GetValue(characterSheet) switch
        {
            <= 12 => 0,
            >= 13 and <= 16 => 4,
            _ => 6
        };
    }
}
