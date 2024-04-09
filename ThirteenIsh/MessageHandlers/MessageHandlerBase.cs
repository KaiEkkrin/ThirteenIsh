using Discord.WebSocket;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.MessageHandlers;

public abstract class MessageHandlerBase<TMessage> : IMessageHandler where TMessage : MessageBase
{
    public Task<bool> HandleAsync(SocketMessageComponent component, string controlId, MessageBase message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message is not TMessage typedMessage)
            throw new ArgumentException($"{GetType()} received a message of mismatched type {message.GetType()}",
                nameof(message));

        return HandleInternalAsync(component, controlId, typedMessage, cancellationToken);
    }

    protected abstract Task<bool> HandleInternalAsync(SocketMessageComponent component, string controlId, TMessage message,
        CancellationToken cancellationToken = default);
}
