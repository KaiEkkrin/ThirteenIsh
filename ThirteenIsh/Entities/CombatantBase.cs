using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
namespace ThirteenIsh.Entities;

/// <summary>
/// This entity describes a combatant in an encounter.
/// Specialise for different combatant types.
/// TODO Make a MonsterCombatant type, which would have the monster stats copied from the
/// monster record, plus variables.
/// </summary>
[BsonKnownTypes(typeof(AdventurerCombatant), typeof(MonsterCombatant))]
public abstract class CombatantBase
{
    /// <summary>
    /// This combatant's character type.
    /// </summary>
    [BsonIgnore]
    public abstract CharacterType CharacterType { get; }

    /// <summary>
    /// This combatant's alias, which identifies it uniquely amongst combatants,
    /// even if it's got the same name as another one (e.g. for monsters.)
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// This combatant's place in the initiative order.
    /// </summary>
    public int Initiative { get; set; }

    /// <summary>
    /// The initiative roll working, in case we want to display it again.
    /// </summary>
    public string InitiativeRollWorking { get; set; } = string.Empty;

    /// <summary>
    /// This combatant's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets a character record for this combatant.
    /// </summary>
    public abstract bool TryGetCharacter(Adventure adventure, [MaybeNullWhen(false)] out ITrackedCharacter character);
}

