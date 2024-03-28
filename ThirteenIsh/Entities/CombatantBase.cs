using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
namespace ThirteenIsh.Entities;

/// <summary>
/// This entity describes a combatant in an encounter.
/// Specialise for different combatant types.
/// TODO Make a MonsterCombatant type, which would have the monster stats copied from the
/// monster record, plus variables.
/// </summary>
[BsonKnownTypes(typeof(AdventurerCombatant))]
public abstract class CombatantBase
{
    /// <summary>
    /// This combatant's character type.
    /// </summary>
    [BsonIgnore]
    public abstract CharacterType CharacterType { get; }

    /// <summary>
    /// This combatant's place in the initiative order.
    /// </summary>
    public int Initiative { get; set; }

    /// <summary>
    /// This combatant's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets an adventurer record for this combatant.
    /// TODO how to handle monsters correctly? Perhaps I should wrap this stuff in an interface.
    /// The goal here is to be able to return the storage of counters and variables.
    /// GameCounter should be the class that knows how to read the storage content.
    /// </summary>
    public abstract bool TryGetAdventurer(Adventure adventure, [MaybeNullWhen(false)] out Adventurer adventurer);
}

