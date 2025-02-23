using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.ThirteenthAge;

/// <summary>
/// A counter type for monster stats, which don't add level to rolls.
/// </summary>
internal class MonsterInitiativeCounter() : GameCounter(ThirteenthAgeSystem.Initiative, options: GameCounterOptions.CanRoll)
{
    public override GameCounterRollResult Roll(
        ITrackedCharacter character,
        ParseTreeBase? bonus,
        IRandomWrapper random,
        int rerolls,
        ref int? targetValue,
        GameCounter? secondCounter = null)
    {
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
