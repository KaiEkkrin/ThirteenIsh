using Discord.WebSocket;

namespace ThirteenIsh.Messages;

/// <summary>
/// Base class for tracked messages.
/// </summary>
internal abstract class MessageBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromHours(4);

    private static long _counter;

    public MessageBase(ulong userId)
    {
        // Generate a unique message ID for it
        var number = Interlocked.Increment(ref _counter);
        MessageId = $"{GetType().Name}-{number}";

        UserId = userId;
    }

    public string MessageId { get; }

    public bool IsExpired => (DateTimeOffset.UtcNow - Timestamp) > Timeout;
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    protected ulong UserId { get; }

    public async Task HandleAsync(SocketMessageComponent component, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        if (component.User.Id != UserId)
        {
            await component.RespondAsync("User ID mismatch", ephemeral: true);
            return;
        }

        await HandleInternalAsync(component, serviceProvider, cancellationToken);
    }

    protected abstract Task HandleInternalAsync(SocketMessageComponent component, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);
}
