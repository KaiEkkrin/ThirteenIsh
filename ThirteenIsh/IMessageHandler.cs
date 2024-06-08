using Discord;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh;

internal interface IMessageHandler
{
    Task<bool> HandleAsync(IDiscordInteraction interaction, string controlId, MessageBase message,
        CancellationToken cancellationToken = default);
}
