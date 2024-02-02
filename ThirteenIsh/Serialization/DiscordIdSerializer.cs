using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Serialization;

internal class DiscordIdSerializer : SerializerBase<DiscordId>
{
    public override DiscordId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var longValue = context.Reader.ReadInt64();
        return new DiscordId { Value = (ulong)longValue };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DiscordId value)
    {
        var longValue = (long)value.Value;
        context.Writer.WriteInt64(longValue);
    }
}
