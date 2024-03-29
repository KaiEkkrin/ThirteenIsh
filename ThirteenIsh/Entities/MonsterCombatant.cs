using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace ThirteenIsh.Entities;

/// <summary>
/// This combatant is a monster. It contains the full copy of the monster stats,
/// because each instance of a monster (several of the same name can be added to
/// one encounter) has its own variables, and each one is not persisted beyond
/// that encounter.
/// The alias will be unique. The name will correspond to the monster of this name,
/// owned by the owning user, in the Characters collection.
/// </summary>
public class MonsterCombatant : CombatantBase, ITrackedCharacter
{
    [BsonIgnore]
    public override CharacterType CharacterType => CharacterType.Monster;

    /// <summary>
    /// The owning user ID.
    /// </summary>
    public long UserId { get; set; }

    [BsonIgnore]
    public ulong NativeUserId => (ulong)UserId;

    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

    public CharacterSheet Sheet { get; set; } = new();

    CharacterType ITrackedCharacter.Type => CharacterType.Monster;

    public Dictionary<string, int> Variables { get; set; } = [];

    public override bool TryGetCharacter(Adventure adventure, [MaybeNullWhen(false)] out ITrackedCharacter character)
    {
        character = this;
        return true;
    }
}
