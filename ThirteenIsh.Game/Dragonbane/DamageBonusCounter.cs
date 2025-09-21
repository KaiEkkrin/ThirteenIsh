using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Dragonbane;

/// <summary>
/// This is the size of the bonus damage die rolled with weapons that use the
/// matching ability counter.
/// TODO should be able to roll this, although it requires weapon selection too :)
/// </summary>
internal class DamageBonusCounter(string name, GameAbilityCounter abilityCounter) : GameCounter(name)
{
    public override bool CanStore => false;

    public override int? GetValue(ICounterSheet sheet)
    {
        return abilityCounter.GetValue(sheet) switch
        {
            <= 12 => 0,
            >= 13 and <= 16 => 4,
            _ => 6
        };
    }
}
