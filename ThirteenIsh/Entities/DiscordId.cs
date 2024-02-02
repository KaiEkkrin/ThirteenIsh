using MongoDB.Bson.Serialization.Attributes;
using ThirteenIsh.Serialization;

namespace ThirteenIsh.Entities;

/// <summary>
/// Because Mongo doesn't natively support serializing a `ulong`
/// </summary>
[BsonSerializer(typeof(DiscordIdSerializer))]
public class DiscordId
{
    public DiscordId()
    {
    }
    
    public DiscordId(ulong value)
    {
        Value = value;
    }

    public ulong Value { get; set; }
}
