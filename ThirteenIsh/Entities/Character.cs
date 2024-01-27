using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ThirteenIsh.Game;

namespace ThirteenIsh.Entities;

/// <summary>
/// This entity type describes a character, which is owned by a user.
/// </summary>
internal class Character
{
    [BsonId]
    public ObjectId Id { get; set; }

    #region Discord

    /// <summary>
    /// The owning user's Discord ID.
    /// Mongo doesn't natively support the `ulong` type, be careful to convert this correctly
    /// by calling ToDatabaseUserId :)
    /// TODO I'll need to ensure an index on this
    /// </summary>
    public long UserId { get; set; }

    [BsonIgnore]
    public ulong NativeUserId => (ulong)UserId;

    public static long ToDatabaseUserId(ulong userId) => (long)userId;

    #endregion

    #region Game stats

    /// <summary>
    /// The character's ability scores.
    /// </summary>
    public Dictionary<string, int> AbilityScores { get; set; } = [];

    /// <summary>
    /// The character's class.
    /// </summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>
    /// The character's level (between 1 and 10.)
    /// </summary>
    public int Level { get; set; } = 1;

    public string Name { get; set; } = string.Empty;

    #endregion

    /// <summary>
    /// Creates a new character with default properties.
    /// </summary>
    public static Character CreateNew(ulong userId) => new()
    {
        UserId = ToDatabaseUserId(userId),
        Level = 1,
        AbilityScores = AttributeName.AbilityScores.ToDictionary(score => score, _ => 10)
    };
}
