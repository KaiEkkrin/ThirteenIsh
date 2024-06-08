using Discord;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.MessageHandlers;

public abstract class MessageHandlerBase<TMessage> : IMessageHandler where TMessage : MessageBase
{
    public Task<bool> HandleAsync(IDiscordInteraction interaction, string controlId, MessageBase message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message is not TMessage typedMessage)
            throw new ArgumentException($"{GetType()} received a message of mismatched type {message.GetType()}",
                nameof(message));

        return HandleInternalAsync(interaction, controlId, typedMessage, cancellationToken);
    }

    protected abstract Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId, TMessage message,
        CancellationToken cancellationToken = default);
}
