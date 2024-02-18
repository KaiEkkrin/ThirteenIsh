using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

/// <summary>
/// This entity describes a combatant in an encounter.
/// Specialise for different combatant types.
/// </summary>
[BsonKnownTypes(typeof(AdventurerCombatant))]
public class CombatantBase
{
    /// <summary>
    /// This combatant's place in the initiative order.
    /// </summary>
    public int Initiative { get; set; }

    /// <summary>
    /// This combatant's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

