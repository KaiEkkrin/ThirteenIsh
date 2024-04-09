using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ThirteenIsh.Database.Entities.Messages;

[Index(nameof(Timestamp))]
public class MessageBase
{
    /// <summary>
    /// Messages are no longer valid after this long
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromDays(1);

    public long Id { get; set; }

    /// <summary>
    /// The concurrency token -- see https://www.npgsql.org/efcore/modeling/concurrency.html?tabs=data-annotations
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// The owning user ID.
    /// </summary>
    public required ulong UserId { get; set; }

    /// <summary>
    /// Our custom IDs supplied to Discord can either be simply the ID of the message entity,
    /// or that ID combined with a control ID if there are multiple controls in the component,
    /// in which case we separate the two with a `:` character.
    /// This method returns the combined message ID to use for a given control ID.
    /// </summary>
    public string GetMessageId(string? controlId = null)
    {
        return string.IsNullOrEmpty(controlId) ? $"{Id}" : $"{Id}:{controlId}";
    }

    /// <summary>
    /// Splits a combined Discord message ID into entity and control ID parts.
    /// </summary>
    public static bool TryParseMessageId(string messageId, [MaybeNullWhen(false)] out long entityId,
        [MaybeNullWhen(false)] out string controlId)
    {
        var indexOfSeparator = messageId.IndexOf(':');
        var (entityIdString, controlIdString) = indexOfSeparator >= 0
            ? (messageId[..indexOfSeparator], messageId[(indexOfSeparator + 1)..])
            : (messageId, string.Empty);

        if (!long.TryParse(entityIdString, out entityId))
        {
            controlId = null;
            return false;
        }

        controlId = controlIdString;
        return true;
    }
}
