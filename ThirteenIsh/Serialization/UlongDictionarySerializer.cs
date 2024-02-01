using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Globalization;

namespace ThirteenIsh.Serialization;

/// <summary>
/// Because `ulong` can't be a Mongo document key type directly
/// </summary>
/// <typeparam name="T">The document value type</typeparam>
internal class UlongDictionarySerializer<T> : SerializerBase<Dictionary<ulong, T>>
{
    private readonly IBsonSerializer<T> _valueSerializer = BsonSerializer.LookupSerializer<T>();

    public override Dictionary<ulong, T> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();

        Dictionary<ulong, T> dictionary = [];
        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var stringKey = context.Reader.ReadName();
            var key = ulong.Parse(stringKey, CultureInfo.InvariantCulture);
            var value = _valueSerializer.Deserialize(context, new BsonDeserializationArgs { NominalType = typeof(T) });

            dictionary[key] = value;
        }

        context.Reader.ReadEndDocument();
        return dictionary;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Dictionary<ulong, T> value)
    {
        context.Writer.WriteStartDocument();
        foreach (var (key, item) in value)
        {
            var stringKey = key.ToString(CultureInfo.InvariantCulture);
            context.Writer.WriteName(stringKey);
            _valueSerializer.Serialize(context, new BsonSerializationArgs { NominalType = typeof(T) }, item);
        }

        context.Writer.WriteEndDocument();
    }
}
