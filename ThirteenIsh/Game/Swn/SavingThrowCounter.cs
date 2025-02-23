using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.Swn;

internal class SavingThrowCounter(string name, params AttributeBonusCounter[] attributeBonuses)
    : GameCounter(name, options: GameCounterOptions.CanRoll)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        return GetSavingThrow(attributeBonuses.Select(a => a.GetValue(sheet)));
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        return GetSavingThrow(attributeBonuses.Select(a => a.GetValue(character.Sheet)));
    }

    public override GameCounterRollResult Roll(
        ITrackedCharacter character, ParseTreeBase? bonus, IRandomWrapper random, int rerolls, ref int? targetValue)
    {
        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(20, rerolls);
        if (bonus is not null)
        {
            parseTree = new BinaryOperationParseTree(0, parseTree, bonus, '+');
        }

        var rolledValue = parseTree.Evaluate(random, out var working);
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

    private static int? GetSavingThrow(IEnumerable<int?> bonuses)
    {
        // The saving throw is 15 minus the higher of the two bonuses
        List<int> bonusValues = new(2);
        foreach (var bonus in bonuses)
        {
            if (bonus.HasValue) bonusValues.Add(bonus.Value);
        }

        int? savingThrowValue = bonusValues.Count == 0 ? null : bonusValues.Max();
        return 15 - savingThrowValue;
    }
}
