using Discord.WebSocket;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh;

internal interface IMessageHandler
{
    Task<bool> HandleAsync(SocketMessageComponent component, string controlId, MessageBase message,
        CancellationToken cancellationToken = default);
}
