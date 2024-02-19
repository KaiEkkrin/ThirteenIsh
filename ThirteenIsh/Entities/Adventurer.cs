using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

/// <summary>
/// An Adventurer is a Character within an adventure and combines their sheet
/// (basic stats) with their state (what resources they've expended, etc).
/// </summary>
public class Adventurer
{
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

    public CharacterSheet Sheet { get; set; } = new();

    /// <summary>
    /// This adventurer's variables. These are the current values of counters that
    /// can have them.
    /// </summary>
    public Dictionary<string, int> Variables { get; set; } = [];
}
