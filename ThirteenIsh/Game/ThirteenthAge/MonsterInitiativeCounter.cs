using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.ThirteenthAge;

/// <summary>
/// A counter type for monster stats, which don't add level to rolls.
/// </summary>
internal class MonsterInitiativeCounter() : GameCounter(ThirteenthAgeSystem.Initiative, options: GameCounterOptions.CanRoll)
{
    public override GameCounterRollResult Roll(
        CharacterSheet sheet,
        ParseTreeBase? bonus,
        IRandomWrapper random,
        int rerolls,
        ref int? targetValue)
    {
        var value = GetValue(sheet);
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
