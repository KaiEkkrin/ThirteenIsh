using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

/// <summary>
/// This entity type describes our guild-specific state.
/// </summary>
internal class Guild
{
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The version of commands most recently registered to this guild.
    /// </summary>
    public int CommandVersion { get; set; }

    /// <summary>
    /// The guild's Discord ID.
    /// Like Character.UserId -- use conversions to and from `ulong`
    /// </summary>
    public long GuildId { get; set; }

    [BsonIgnore]
    public ulong NativeGuildId => (ulong)GuildId;

    public static long ToDatabaseGuildId(ulong guildId) => (long)guildId;
}
