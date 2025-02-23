using System.Globalization;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.Swn;

internal class SkillCounter(string name) : GameCounter(name, defaultValue: -1, minValue: -1, maxValue: 4, options: GameCounterOptions.CanRoll)
{
    public override GameCounterRollResult Roll(
        ITrackedCharacter character, ParseTreeBase? bonus, IRandomWrapper random, int rerolls, ref int? targetValue,
        GameCounter? secondCounter = null)
    {
        // In Stars Without Number, a skill check is rolled with 2d6 + skill bonus + attribute bonus.
        // Rules as written, rerolls aren't a thing, but since 13ish supports them, I might as well
        // do something sensible...
        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(6, rerolls, 2);

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

        if (bonus is not null)
        {
            parseTree = new BinaryOperationParseTree(0, parseTree, bonus, '+');
        }

        StringBuilder rollNameBuilder = new($"{Name}");
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
