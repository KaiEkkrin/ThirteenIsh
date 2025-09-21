using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.Swn;

internal class MonsterSavingThrowCounter() : GameCounter("Save", options: GameCounterOptions.CanRoll)
{
    public override GameCounterRollResult Roll(
        ITrackedCharacter character, ParseTreeBase? bonus, IRandomWrapper random, int rerolls, ref int? targetValue,
        GameCounter? secondCounter = null, GameCounterRollOptions flags = GameCounterRollOptions.None)
    {
        // Monster saving throws roll 1d20, with the save value as the target (not added to roll)
        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(20, rerolls, 1);

        // Add any additional bonus to the roll
        if (bonus != null)
        {
            parseTree = new BinaryOperationParseTree(0, parseTree, bonus, '+');
        }

        var rolledValue = parseTree.Evaluate(random, out var working);

        // Use the monster's save value as the target if no target was specified
        targetValue ??= GetValue(character);

        return new GameCounterRollResult
        {
            CounterName = Name,
            Error = GameCounterRollError.Success,
            Roll = rolledValue,
            Working = working,
            Success = targetValue.HasValue ? rolledValue >= targetValue : null
        };
    }
}