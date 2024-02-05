using ThirteenIsh.Game;

namespace ThirteenIsh.Entities;

/// <summary>
/// Defines a character's stats.
/// </summary>
public class CharacterSheet
{
    /// <summary>
    /// This character's counters.
    /// </summary>
    public List<CharacterCounter> Counters { get; set; } = [];

    /// <summary>
    /// This character's properties.
    /// </summary>
    public List<CharacterProperty> Properties { get; set; } = [];
}
