
namespace ThirteenIsh.Game.Swn;

internal class AttackCounter() : GameCounter(SwnSystem.Attack, options: GameCounterOptions.CanRoll)
{
    public override GameCounterRollResult Roll(
        ITrackedCharacter character, ParseTreeBase? bonus, IRandomWrapper random, int rerolls, ref int? targetValue,
        GameCounter? secondCounter = null, GameCounterRollOptions flags = GameCounterRollOptions.None)
    {
        // Monster attacks in Stars Without Number roll 1d20 + attack bonus
        var attackBonus = GetValue(character);
        if (!attackBonus.HasValue)
            return new GameCounterRollResult { CounterName = Name, Error = GameCounterRollError.NoValue };

        ParseTreeBase parseTree = new BinaryOperationParseTree(0,
            DiceRollParseTree.BuildWithRerolls(20, rerolls, 1),
            new IntegerParseTree(0, attackBonus.Value, Name),
            '+');

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
            Success = targetValue.HasValue ? rolledValue >= targetValue : null
        };
    }
}