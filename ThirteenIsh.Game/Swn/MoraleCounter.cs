using System.Globalization;

namespace ThirteenIsh.Game.Swn;

internal class MoraleCounter(string name, int defaultValue = 7, int minValue = 2, int maxValue = 12)
    : GameCounter(name, "ML", defaultValue, minValue, maxValue, GameCounterOptions.CanRoll)
{
    public override GameCounterRollResult Roll(
        ITrackedCharacter character, ParseTreeBase? bonus, IRandomWrapper random, int rerolls, ref int? targetValue,
        GameCounter? secondCounter = null, GameCounterRollOptions flags = GameCounterRollOptions.None)
    {
        // Morale checks in Stars Without Number are rolled with 2d6.
        // Unlike skill checks, the morale value is not added to the roll.
        // Instead, the morale value becomes the target value if none is specified.
        // Success is achieved when the roll is equal to or less than the target value.

        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(6, rerolls, 2);

        // Use morale value as target if no target was specified
        if (!targetValue.HasValue)
        {
            var moraleValue = GetValue(character);
            if (!moraleValue.HasValue)
                return new GameCounterRollResult { CounterName = Name, Error = GameCounterRollError.NoValue };

            targetValue = moraleValue.Value;
        }

        // Second counter is ignored (not supported here)

        // Add any additional bonus
        if (bonus != null)
        {
            parseTree = new BinaryOperationParseTree(0, parseTree, bonus, '+');
        }

        var rolledValue = parseTree.Evaluate(random, out var working);
        return new GameCounterRollResult
        {
            CounterName = Name,
            Error = GameCounterRollError.Success,
            Roll = rolledValue,
            Working = working,
            Success = targetValue.HasValue ? rolledValue <= targetValue : null
        };
    }
}