using ThirteenIsh.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.ThirteenthAge;

// TODO should be able to roll this
internal class InitiativeCounter(
    GameCounter levelCounter,
    AbilityBonusCounter dexterityBonusCounter)
    : GameCounter("Initiative", "Init")
{
    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        var level = levelCounter.GetValue(characterSheet);
        var dexterityBonus = dexterityBonusCounter.GetValue(characterSheet);
        return dexterityBonus + level;
    }

    public override GameCounterRollResult Roll(
        Adventurer adventurer, ParseTreeBase? bonus, IRandomWrapper random, int rerolls, ref int? targetValue)
    {
        // Add the level onto the bonus here
        IntegerParseTree levelBonus = new(0, levelCounter.GetValue(adventurer.Sheet) ?? 0, "level");
        ParseTreeBase fullBonus = bonus is not null
            ? new BinaryOperationParseTree(0, levelBonus, bonus, '+')
            : levelBonus;

        return dexterityBonusCounter.Roll(adventurer, fullBonus, random, rerolls, ref targetValue);
    }
}
