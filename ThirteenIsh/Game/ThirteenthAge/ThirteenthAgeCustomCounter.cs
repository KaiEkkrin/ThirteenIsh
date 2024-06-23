using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

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
        ref int? targetValue)
    {
        // The 13th Age custom counter rolls by adding its value to a d20, with nothing else --
        // seems like the simplest thing to do. Like this it can be used e.g. to define monster attacks.

        // TODO throwing GamePropertyException here currently fails the whole command, instead fix it
        // so that a suitable error message is returned
        var value = GetValue(character);
        if (!value.HasValue) throw new GamePropertyException(Name);

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
            Roll = rolledValue,
            Success = targetValue.HasValue ? rolledValue >= targetValue.Value : null,
            Working = working
        };
    }
}
