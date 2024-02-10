using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace ThirteenIsh.Entities;

/// <summary>
/// Common base class for tracked message types.
/// </summary>
[BsonKnownTypes(
    typeof(DeleteAdventureMessage),
    typeof(DeleteCharacterMessage),
    typeof(EditCharacterMessage),
    typeof(LeaveAdventureMessage))]
public class MessageBase : UserEntityBase
{
    /// <summary>
    /// Messages are no longer valid after this long
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromDays(1);

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Our custom IDs supplied to Discord can either be simply the ID of the message entity,
    /// or that ID combined with a control ID if there are multiple controls in the component,
    /// in which case we separate the two with a `:` character.
    /// This method returns the combined message ID to use for a given control ID.
    /// </summary>
    public string GetMessageId(string? controlId = null)
    {
        return string.IsNullOrEmpty(controlId) ? Id.ToString() : $"{Id}:{controlId}";
    }

    /// <summary>
    /// Override with message handling code.
    /// The user ID will already have been confirmed to match.
    /// Return true if the message handling is completed and it can be deleted, else false.
    /// </summary>
    public virtual Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Splits a combined Discord message ID into entity and control ID parts.
    /// </summary>
    public static bool TryParseMessageId(string messageId, [MaybeNullWhen(false)] out ObjectId entityId,
        [MaybeNullWhen(false)] out string controlId)
    {
        var indexOfSeparator = messageId.IndexOf(':');
        var (entityIdString, controlIdString) = indexOfSeparator >= 0
            ? (messageId[..indexOfSeparator], messageId[(indexOfSeparator + 1)..])
            : (messageId, string.Empty);

        if (!ObjectId.TryParse(entityIdString, out entityId))
        {
            controlId = null;
            return false;
        }

        controlId = controlIdString;
        return true;
    }
}
