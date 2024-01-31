using ThirteenIsh.Game;

namespace ThirteenIsh.Entities;

/// <summary>
/// Defines a character's stats.
/// </summary>
public class CharacterSheet
{
    /// <summary>
    /// The character's ability scores.
    /// </summary>
    public Dictionary<string, int> AbilityScores { get; set; } = [];

    /// <summary>
    /// The character's class.
    /// </summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>
    /// The character's level (between 1 and 10.)
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// Gets an ability modifier for this character.
    /// </summary>
    public int GetAbilityModifier(string abilityScore)
    {
        // This needs to always round down -- the divide operator rounds towards zero
        if (!AbilityScores.TryGetValue(abilityScore, out int score)) return 0;

        var (quotient, remainder) = Math.DivRem(score - 10, 2);
        return remainder == -1 ? quotient - 1 : quotient;
    }

    public static CharacterSheet CreateDefault() => new()
    {
        AbilityScores = AttributeName.AbilityScores.ToDictionary(score => score, _ => 10),
        Level = 1
    };
}
