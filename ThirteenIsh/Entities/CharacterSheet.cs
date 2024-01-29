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

    public static CharacterSheet CreateDefault() => new()
    {
        AbilityScores = AttributeName.AbilityScores.ToDictionary(score => score, _ => 10),
        Level = 1
    };
}
