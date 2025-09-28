

namespace ThirteenIsh.Game.Dragonbane;

/// <summary>
/// This is used for hit points and willpower points. TODO support ad-hoc bonus?
/// </summary>
internal class PointsCounter(string name, string alias, GameAbilityCounter abilityCounter)
    : GameCounter(name, alias, options: GameCounterOptions.HasVariable)
{
    public override bool CanStore => false;

    protected override int? GetValueInternal(ICharacterBase character)
    {
        return abilityCounter.GetValue(character);
    }
}
