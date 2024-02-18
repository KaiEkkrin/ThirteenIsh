using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

/// <summary>
/// This entity describes an encounter, which happens within a channel in a guild.
/// </summary>
public class Encounter
{
    /// <summary>
    /// The name of the adventure this encounter is set in.
    /// </summary>
    public string AdventureName { get; set; } = string.Empty;

    /// <summary>
    /// The list of combatants in turn order.
    /// </summary>
    public List<CombatantBase> Combatants { get; set; } = [];

    /// <summary>
    /// The Discord ID of the pinned message relating to this encounter, if any.
    /// </summary>
    public long? PinnedMessageId { get; set; }

    [BsonIgnore]
    public ulong? NativePinnedMessageId => (ulong?)PinnedMessageId;

    /// <summary>
    /// Which round the encounter is on (begins at 1.)
    /// </summary>
    public int Round { get; set; } = 1;

    /// <summary>
    /// Whose turn it is currently.
    /// </summary>
    public int TurnIndex { get; set; }

    /// <summary>
    /// This encounter's variables (game system dependent.)
    /// </summary>
    public Dictionary<string, int> Variables { get; set; } = [];
}
