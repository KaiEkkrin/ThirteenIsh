using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.Dragonbane;

/// <summary>
/// A Dragonbane skill level counter. This represents the die target a player must roll under
/// (or equal to) in order to succeed at a skill check for it.
/// </summary>
internal class SkillLevelCounter(GameAbilityCounter attribute, GameCounter skill, bool secondary = false)
    : GameCounter($"{skill.Name} Level", options: GameCounterOptions.CanRoll | GameCounterOptions.IsHidden)
{
    /// <summary>
    /// The attribute associated with this skill.
    /// </summary>
    public GameAbilityCounter Attribute => attribute;

    /// <summary>
    /// This counts the number of points the player has invested in the skill (0 = untrained.)
    /// </summary>
    public GameCounter Skills => skill;

    /// <summary>
    /// True if this is a secondary skill, else false.
    /// </summary>
    public bool Secondary => secondary;

    public override bool CanStore => false;

    public override int? GetValue(ICounterSheet sheet)
    {
        var attributeValue = attribute.GetValue(sheet);
        int? baseChance = attributeValue switch
        {
            >= 1 and <= 5 => 3,
            >= 6 and <= 8 => 4,
            >= 9 and <= 12 => 5,
            >= 13 and <= 15 => 6,
            >= 16 and <= 18 => 7,
            _ => null
        };

        var skillValue = skill.GetValue(sheet);
        if (!baseChance.HasValue || !skillValue.HasValue) return null;
        return skillValue >= 1
            ? Math.Min(18, baseChance.Value * 2 + skillValue.Value - 1)
            : secondary
            ? 0
            : baseChance;
    }

    public override GameCounterRollResult Roll(
        CharacterSheet sheet,
        ParseTreeBase? bonus,
        IRandomWrapper random,
        int rerolls,
        ref int? targetValue)
    {
        ParseTreeBase parseTree = DiceRollParseTree.BuildWithRerolls(20, rerolls);
        if (bonus is not null)
        {
            parseTree = new BinaryOperationParseTree(0, parseTree, bonus, '+');
        }

        var rolledValue = parseTree.Evaluate(random, out var working);

        targetValue ??= GetValue(sheet);
        return new GameCounterRollResult
        {
            Roll = rolledValue,
            Success = targetValue.HasValue ? rolledValue <= targetValue.Value : null,
            Working = working
        };
    }
}
