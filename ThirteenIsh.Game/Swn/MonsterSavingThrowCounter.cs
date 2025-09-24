
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

        // Extract the natural d20 roll for critical success/failure detection
        var naturalRoll = DiceRollHelper.ExtractNaturalD20Roll(working);

        targetValue ??= GetValue(character);

        // Determine success: natural 20 always succeeds, natural 1 always fails
        bool? success = null;
        if (targetValue.HasValue)
        {
            if (naturalRoll == 20)
                success = true;
            else if (naturalRoll == 1)
                success = false;
            else
                success = rolledValue >= targetValue;
        }

        return new GameCounterRollResult
        {
            CounterName = Name,
            Error = GameCounterRollError.Success,
            Roll = rolledValue,
            Working = working,
            Success = success
        };
    }
}