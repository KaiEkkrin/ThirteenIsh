using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.Dragonbane;

/// <summary>
/// A Dragonbane skill level counter. This represents the die target a player must roll under
/// (or equal to) in order to succeed at a skill check for it.
/// </summary>
internal class SkillLevelCounter(GameAbilityCounter attribute, GameCounter skill, bool secondary = false)
    : GameCounter($"{skill.Name} Level", maxValue: 15)
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

    public override int GetValue(CharacterSheet characterSheet)
    {
        var attributeValue = attribute.GetValue(characterSheet);
        var baseChance = attributeValue switch
        {
            >= 1 and <= 5 => 3,
            >= 6 and <= 8 => 4,
            >= 9 and <= 12 => 5,
            >= 13 and <= 15 => 6,
            >= 16 and <= 18 => 7,
            _ => throw new InvalidOperationException($"Invalid value for {attribute.Name} : {attributeValue}")
        };

        var skillValue = skill.GetValue(characterSheet);
        return skillValue >= 1
            ? Math.Max(18, baseChance * 2 + skillValue - 1)
            : secondary
            ? 0
            : baseChance;
    }
}
