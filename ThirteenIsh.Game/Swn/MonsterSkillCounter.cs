using System.Globalization;

namespace ThirteenIsh.Game.Swn;

internal class MonsterSkillCounter(string name, int defaultValue = -1)
    : GameCounter(name, defaultValue: defaultValue, minValue: -1, maxValue: 4, options: GameCounterOptions.CanRoll)
{
    public override GameCounterRollResult Roll(
        ITrackedCharacter character, ParseTreeBase? bonus, IRandomWrapper random, int rerolls, ref int? targetValue,
        GameCounter? secondCounter = null, GameCounterRollOptions flags = GameCounterRollOptions.None)
    {
        // Monster skill checks cannot be attack rolls since they don't have an AttackBonusCounter
        if (flags.HasFlag(GameCounterRollOptions.IsAttack))
        {
            return new GameCounterRollResult
            {
                CounterName = Name,
                Error = GameCounterRollError.NotRollable,
                Working = "Cannot make attack rolls with monster skill counter"
            };
        }

        // Monster skill checks roll 2d6 + skill bonus (no attribute bonus)
        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(6, rerolls, 2);

        var skillBonus = GetValue(character) ?? DefaultValue;
        parseTree = new BinaryOperationParseTree(0, parseTree, new IntegerParseTree(0, skillBonus, Name), '+');

        // Add second counter (attribute bonus) if provided
        if (secondCounter != null)
        {
            var secondCounterValue = secondCounter.GetValue(character);
            if (!secondCounterValue.HasValue)
                return new GameCounterRollResult { CounterName = Name, Error = GameCounterRollError.NoValue };

            parseTree = new BinaryOperationParseTree(0, parseTree,
                new IntegerParseTree(0, secondCounterValue.Value, secondCounter.Name), '+');
        }

        // Add any additional bonus
        if (bonus != null)
        {
            parseTree = new BinaryOperationParseTree(0, parseTree, bonus, '+');
        }

        StringBuilder rollNameBuilder = new($"{Name}");
        if (secondCounter != null)
        {
            rollNameBuilder.Append(CultureInfo.CurrentCulture,
                $" ({secondCounter.Name[..3].ToUpper(CultureInfo.CurrentCulture)})");
        }

        if (skillBonus < 0) rollNameBuilder.Append(" unskilled");

        var rolledValue = parseTree.Evaluate(random, out var working);
        return new GameCounterRollResult
        {
            CounterName = rollNameBuilder.ToString(),
            Error = GameCounterRollError.Success,
            Roll = rolledValue,
            Working = working,
            Success = targetValue.HasValue ? rolledValue >= targetValue : null
        };
    }
}