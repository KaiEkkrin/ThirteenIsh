using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

public class UserEntityBase
{
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The owning user's Discord ID.
    /// </summary>
    public DiscordId UserId { get; set; } = new();
}
