using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

/// <summary>
/// This entity type describes a character, which is owned by a user.
/// </summary>
public class Character : UserEntityBase
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// For searching for characters case insensitively.
    /// This one, rather than Name, is indexed.
    /// </summary>
    [BsonElement]
    public string NameUpper => Name.ToUpperInvariant();

    /// <summary>
    /// The character type.
    /// </summary>
    public CharacterType CharacterType { get; set; }

    /// <summary>
    /// The game system this character uses.
    /// </summary>
    public string GameSystem { get; set; } = string.Empty;

    /// <summary>
    /// The character sheet.
    /// </summary>
    public CharacterSheet Sheet { get; set; } = new();

    public DateTimeOffset LastEdited { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// An incrementing version number, used to detect conflicts.
    /// </summary>
    public long Version { get; set; }
}
