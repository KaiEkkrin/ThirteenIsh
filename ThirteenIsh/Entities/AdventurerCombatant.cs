using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace ThirteenIsh.Entities;

/// <summary>
/// This combatant is an adventurer. They use the adventurer record's counters.
/// </summary>
public class AdventurerCombatant : CombatantBase
{
    [BsonIgnore]
    public override CharacterType CharacterType => CharacterType.PlayerCharacter;

    /// <summary>
    /// The owning user ID.
    /// </summary>
    public long UserId { get; set; }

    [BsonIgnore]
    public ulong NativeUserId => (ulong)UserId;

    public override bool TryGetAdventurer(Adventure adventure, [MaybeNullWhen(false)] out Adventurer adventurer)
    {
        return adventure.Adventurers.TryGetValue(NativeUserId, out adventurer);
    }
}
