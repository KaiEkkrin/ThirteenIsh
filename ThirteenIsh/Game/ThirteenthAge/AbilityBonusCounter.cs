using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class AbilityBonusCounter(GameCounter levelCounter, GameCounter scoreCounter)
    : GameCounter(GetBonusCounterName(scoreCounter.Name), options: GameCounterOptions.CanRoll | GameCounterOptions.IsHidden)
{
    public override bool CanStore => false;

    public override int? GetValue(ICounterSheet sheet)
    {
        var score = scoreCounter.GetValue(sheet);
        return GetBonusValue(score);
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        var score = scoreCounter.GetValue(character);
        return GetBonusValue(score);
    }

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

        // In 13th Age we always add the character's level bonus to rolls like this
        IntegerParseTree levelBonus = new(0, levelCounter.GetValue(character) ?? 0, "level");
        ParseTreeBase parseTree =
            new BinaryOperationParseTree(0,
                new BinaryOperationParseTree(0,
                    DiceRollParseTree.BuildWithRerolls(20, rerolls),
                    new IntegerParseTree(0, value.Value, Name),
                    '+'),
                levelBonus,
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

    public static string GetBonusCounterName(string counterName) => $"{counterName} Bonus";

    private static int? GetBonusValue(int? score)
    {
        if (!score.HasValue) return null;

        // Always round this down, rather than towards zero
        var (div, rem) = Math.DivRem(score.Value - 10, 2);
        return rem < 0 ? div - 1 : div;
    }
}
