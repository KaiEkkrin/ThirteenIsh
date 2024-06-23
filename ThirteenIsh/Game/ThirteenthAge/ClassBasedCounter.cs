using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal abstract class ClassBasedCounter(
    string name,
    string alias,
    GameProperty classProperty,
    GameCounterOptions options = GameCounterOptions.None)
    : GameCounter(name, alias, options: options)
{
    public sealed override bool CanStore => false;

    // Here we need to have different ways to calculate the value in the tracked character case because
    // we could have fix values for both the ability bonuses and the final value
    public sealed override int? GetValue(ICounterSheet sheet)
    {
        if (sheet is not CharacterSheet characterSheet) return null;
        var classValue = classProperty.GetValue(characterSheet);
        return GetValueInternal(classValue, counter => counter.GetValue(sheet));
    }

    public sealed override int? GetValue(ITrackedCharacter character)
    {
        var classValue = classProperty.GetValue(character.Sheet);
        var baseValue = GetValueInternal(classValue, counter => counter.GetValue(character));
        return AddFix(baseValue, character);
    }

    protected abstract int? GetValueInternal(string? classValue, Func<GameCounter, int?> getCounterValue);
}
