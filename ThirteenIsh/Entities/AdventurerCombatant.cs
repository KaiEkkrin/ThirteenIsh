using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

/// <summary>
/// This combatant is an adventurer. They use the adventurer record's counters.
/// </summary>
public class AdventurerCombatant : CombatantBase
{
    /// <summary>
    /// The owning user ID.
    /// </summary>
    public long UserId { get; set; }

    [BsonIgnore]
    public ulong NativeUserId => (ulong)UserId;
}
