using System.Globalization;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.Swn;

internal class SkillCounter(string name, AttackBonusCounter? attackBonusCounter, int defaultValue = -1, GameCounterOptions options = GameCounterOptions.CanRoll) : GameCounter(name, defaultValue: defaultValue, minValue: -1, maxValue: 4, options: options)
{
    public override GameCounterRollResult Roll(
        ITrackedCharacter character, ParseTreeBase? bonus, IRandomWrapper random, int rerolls, ref int? targetValue,
        GameCounter? secondCounter = null, GameCounterRollOptions flags = GameCounterRollOptions.None)
    {
        // In Stars Without Number, a skill check is rolled with 2d6 + skill bonus + attribute bonus.
        // For attack rolls, use 1d20 + skill bonus + attribute bonus + attack bonus.
        // TODO : Support the Specialist focus. For each specialisation, we should add one extra die to the
        // pool of dice to be rolled. Then, we accept the top two dice from the pool.
        ParseTreeBase parseTree = flags.HasFlag(GameCounterRollOptions.IsAttack)
            ? DiceRollParseTree.BuildWithRerolls(20, rerolls, 1)
            : DiceRollParseTree.BuildWithRerolls(6, rerolls, 2);

        // It's up to the GM to decide whether or not the skill check is valid, if the character has
        // no skill bonus
        var skillBonus = GetValue(character) ?? DefaultValue;
        parseTree = new BinaryOperationParseTree(0, parseTree, new IntegerParseTree(0, skillBonus, Name), '+');

        if (secondCounter is AttributeBonusCounter attributeBonusCounter)
        {
            var attributeBonus = attributeBonusCounter.GetValue(character);
            if (!attributeBonus.HasValue) return new GameCounterRollResult { CounterName = Name, Error = GameCounterRollError.NoValue };

            parseTree = new BinaryOperationParseTree(0, parseTree, new IntegerParseTree(0, attributeBonus.Value, attributeBonusCounter.Name), '+');
        }

        // Add attack bonus when this is an attack roll
        if (flags.HasFlag(GameCounterRollOptions.IsAttack))
        {
            if (attackBonusCounter == null)
            {
                return new GameCounterRollResult
                {
                    CounterName = Name,
                    Error = GameCounterRollError.NotRollable,
                    Working = "Cannot make attack rolls without AttackBonusCounter"
                };
            }

            var attackBonus = attackBonusCounter.GetValue(character);
            if (!attackBonus.HasValue) return new GameCounterRollResult { CounterName = Name, Error = GameCounterRollError.NoValue };

            parseTree = new BinaryOperationParseTree(0, parseTree, new IntegerParseTree(0, attackBonus.Value, attackBonusCounter.Name), '+');
        }

        if (bonus is not null)
        {
            parseTree = new BinaryOperationParseTree(0, parseTree, bonus, '+');
        }

        StringBuilder rollNameBuilder = new($"{Name}");
        if (flags.HasFlag(GameCounterRollOptions.IsAttack))
        {
            rollNameBuilder.Append(" attack");
        }

        if (secondCounter is not null)
        {
            rollNameBuilder.Append(CultureInfo.CurrentCulture, $" ({secondCounter.Name[..3].ToUpper(CultureInfo.CurrentCulture)})");
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
