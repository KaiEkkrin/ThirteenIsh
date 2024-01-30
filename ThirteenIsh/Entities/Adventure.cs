namespace ThirteenIsh.Entities;

/// <summary>
/// An Adventure has a collection of characters with state.
/// It exists within a Guild.
/// </summary>
public class Adventure
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Maps each user ID to their Adventurer.
    /// </summary>
    public Dictionary<long, Adventurer> Adventurers { get; set; } = [];
}
