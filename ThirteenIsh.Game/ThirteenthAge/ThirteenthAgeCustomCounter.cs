

namespace ThirteenIsh.Game.ThirteenthAge;

internal class ThirteenthAgeCustomCounter(CustomCounter customCounter)
    : GameCounter(customCounter.Name, defaultValue: customCounter.DefaultValue, options: customCounter.Options)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        return base.GetValue(sheet) ?? DefaultValue;
    }

    public override GameCounterRollResult Roll(
        ITrackedCharacter character,
        ParseTreeBase? bonus,
        IRandomWrapper random,
        int rerolls,
        ref int? targetValue,
        GameCounter? secondCounter = null,
        GameCounterRollOptions flags = GameCounterRollOptions.None)
    {
        // The 13th Age custom counter rolls by adding its value to a d20, with nothing else --
        // seems like the simplest thing to do. Like this it can be used e.g. to define monster attacks.
        var value = GetValue(character);
        if (!value.HasValue) return new GameCounterRollResult { CounterName = Name, Error = GameCounterRollError.NoValue };

        ParseTreeBase parseTree =
            new BinaryOperationParseTree(0,
                DiceRollParseTree.BuildWithRerolls(20, rerolls),
                new IntegerParseTree(0, value.Value, Name),
                '+');

        if (bonus is not null)
        {
            parseTree = new BinaryOperationParseTree(0, parseTree, bonus, '+');
        }

        var rolledValue = parseTree.Evaluate(random, out var working);
        return new GameCounterRollResult
        {
            CounterName = Name,
            Error = GameCounterRollError.Success,
            Roll = rolledValue,
            Success = targetValue.HasValue ? rolledValue >= targetValue.Value : null,
            Working = working
        };
    }
}
