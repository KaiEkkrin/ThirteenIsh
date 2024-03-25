using ThirteenIsh.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class AbilityBonusCounter(GameCounter levelCounter, GameCounter scoreCounter)
    : GameCounter($"{scoreCounter.Name} {Suffix}", options: GameCounterOptions.CanRoll | GameCounterOptions.IsHidden)
{
    public const string Suffix = "Bonus";

    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        var score = scoreCounter.GetValue(characterSheet);
        if (!score.HasValue) return null;

        // Always round this down, rather than towards zero
        var (div, rem) = Math.DivRem(score.Value - 10, 2);
        return rem < 0 ? div - 1 : div;
    }

    public override GameCounterRollResult Roll(
        Adventurer adventurer,
        ParseTreeBase? bonus,
        IRandomWrapper random,
        int rerolls,
        ref int? targetValue)
    {
        var value = GetValue(adventurer.Sheet);
        if (!value.HasValue) throw new GamePropertyException(Name);

        // In 13th Age we always add the character's level bonus to rolls like this
        IntegerParseTree levelBonus = new(0, levelCounter.GetValue(adventurer.Sheet) ?? 0, "level");
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
            Roll = rolledValue,
            Success = targetValue.HasValue ? rolledValue >= targetValue.Value : null,
            Working = working
        };
    }
}
