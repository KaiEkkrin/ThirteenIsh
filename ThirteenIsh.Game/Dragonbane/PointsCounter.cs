

namespace ThirteenIsh.Game.Dragonbane;

/// <summary>
/// This is used for hit points and willpower points. TODO support ad-hoc bonus?
/// </summary>
internal class PointsCounter(string name, string alias, GameAbilityCounter abilityCounter)
    : GameCounter(name, alias, options: GameCounterOptions.HasVariable)
{
    public override bool CanStore => false;

    public override int? GetValue(ICounterSheet sheet)
    {
        return abilityCounter.GetValue(sheet);
    }
}
