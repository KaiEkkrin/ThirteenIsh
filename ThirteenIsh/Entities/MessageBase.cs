using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;

namespace ThirteenIsh.Entities;

/// <summary>
/// Common base class for tracked message types.
/// </summary>
[BsonKnownTypes(typeof(DeleteAdventureMessage), typeof(DeleteCharacterMessage))]
public class MessageBase : UserEntityBase
{
    /// <summary>
    /// Messages are no longer valid after this long
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromDays(1);

    /// <summary>
    /// Use this as the Custom ID for Discord.
    /// </summary>
    public string MessageId => Id.ToString();

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Override with message handling code.
    /// The user ID will already have been confirmed to match.
    /// </summary>
    public virtual Task HandleAsync(SocketMessageComponent component, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
