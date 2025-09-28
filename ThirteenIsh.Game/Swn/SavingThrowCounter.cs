

namespace ThirteenIsh.Game.Swn;

internal class SavingThrowCounter(string name, params AttributeBonusCounter[] attributeBonuses)
    : GameCounter(name, options: GameCounterOptions.CanRoll)
{
    protected override int? GetValueInternal(ICharacterBase character)
    {
        return GetSavingThrow(attributeBonuses.Select(a => a.GetValue(character)));
    }

    public override GameCounterRollResult Roll(
        ITrackedCharacter character, ParseTreeBase? bonus, IRandomWrapper random, int rerolls, ref int? targetValue,
        GameCounter? secondCounter = null, GameCounterRollOptions flags = GameCounterRollOptions.None)
    {
        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(20, rerolls);
        if (bonus is not null)
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
