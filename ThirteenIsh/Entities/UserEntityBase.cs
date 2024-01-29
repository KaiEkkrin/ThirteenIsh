using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

public class UserEntityBase
{
    [BsonId]
    public ObjectId Id { get; set; }

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
