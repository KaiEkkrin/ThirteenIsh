using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ThirteenIsh.Serialization;

namespace ThirteenIsh.Entities;

/// <summary>
/// This entity type describes our guild-specific state.
/// </summary>
public class Guild
{
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The version of commands most recently registered to this guild.
    /// </summary>
    public int CommandVersion { get; set; }

    /// <summary>
    /// The guild's Discord ID.
    /// </summary>
    public long GuildId { get; set; }

    [BsonIgnore]
    public ulong NativeGuildId => (ulong)GuildId;

    /// <summary>
    /// This guild's list of adventures.
    /// </summary>
    public List<Adventure> Adventures { get; set; } = [];

    /// <summary>
    /// This guild's current adventure.
    /// </summary>
    public string CurrentAdventureName { get; set; } = string.Empty;

    /// <summary>
    /// Maps each channel ID to the current encounter in that channel, if any.
    /// </summary>
    [BsonSerializer(typeof(UlongDictionarySerializer<Encounter>))]
    public Dictionary<ulong, Encounter> Encounters { get; set; } = [];

    /// <summary>
    /// An incrementing version number, used to detect conflicts.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// The current adventure, if any.
    /// </summary>
    [BsonIgnore]
    public Adventure? CurrentAdventure => Adventures.FirstOrDefault(o => o.Name == CurrentAdventureName);
}
