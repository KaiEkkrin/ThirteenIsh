

namespace ThirteenIsh.Game.ThirteenthAge;

internal abstract class ClassBasedCounter(
    string name,
    string? alias,
    GameProperty classProperty,
    GameCounterOptions options = GameCounterOptions.None)
    : GameCounter(name, alias, options: options)
{
    public sealed override bool CanStore => false;

    protected sealed override int? GetValueInternal(ICharacterBase character)
    {
        var classValue = classProperty.GetValue(character);
        return GetCounterValueInternal(classValue, counter => counter.GetValue(character));
    }

    protected abstract int? GetCounterValueInternal(string? classValue, Func<GameCounter, int?> getCounterValue);
}
