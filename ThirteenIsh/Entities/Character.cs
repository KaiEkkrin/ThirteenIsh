using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

/// <summary>
/// This entity type describes a character, which is owned by a user.
/// </summary>
internal class Character
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The owning user's Discord ID.
    /// Mongo doesn't natively support the `ulong` type and so I'll promote to the
    /// more precise type here :)
    /// TODO I'll need to ensure an index on this
    /// </summary>
    public decimal UserId { get; set; }
}
