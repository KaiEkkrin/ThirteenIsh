namespace ThirteenIsh.Entities;

/// <summary>
/// Defines a character's stats.
/// </summary>
public class CharacterSheet
{
    /// <summary>
    /// This character's counters.
    /// </summary>
    public Dictionary<string, int> Counters { get; set; } = [];

    /// <summary>
    /// This character's properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = [];
}
