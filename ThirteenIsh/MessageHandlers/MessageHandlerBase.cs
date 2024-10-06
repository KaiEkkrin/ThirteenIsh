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

    protected static async Task RespondOrModifyAsync(
        IDiscordInteraction interaction, MessageBase message, Embed? embed = null, MessageComponent? components = null)
    {
        // Assumes that if we have no message ID (it's a channel message not a database message),
        // the response will already have been started, but not otherwise:
        if (message.Id == 0)
        {
            await interaction.ModifyOriginalResponseAsync(properties =>
            {
                if (embed != null) properties.Embed = embed;
                if (components != null) properties.Components = components;
            });
        }
        else
        {
            await interaction.RespondAsync(embed: embed, components: components);
        }
    }
}
