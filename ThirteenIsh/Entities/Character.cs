using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

/// <summary>
/// This entity type describes a character, which is owned by a user.
/// </summary>
public class Character
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The character sheet.
    /// </summary>
    public CharacterSheet Sheet { get; set; } = new();

    /// <summary>
    /// The owning user's Discord ID.
    /// Mongo doesn't natively support the `ulong` type, be careful to convert this correctly
    /// by calling ToDatabaseUserId :)
    /// </summary>
    public long UserId { get; set; }

    [BsonIgnore]
    public ulong NativeUserId => (ulong)UserId;

    public static long ToDatabaseUserId(ulong userId) => (long)userId;
}
