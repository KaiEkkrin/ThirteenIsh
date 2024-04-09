using Discord.WebSocket;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(EndEncounterMessage))]
internal sealed class EndEncounterMessageHandler(SqlDataService dataService) : MessageHandlerBase<EndEncounterMessage>
{
    protected override async Task<bool> HandleInternalAsync(SocketMessageComponent component, string controlId,
        EndEncounterMessage message, CancellationToken cancellationToken = default)
    {
        var encounter = await dataService.DeleteEncounterAsync(message.GuildId, message.ChannelId, cancellationToken);
        if (encounter == null)
        {
            await component.RespondAsync("There is no active encounter in this channel.", ephemeral: true);
            return true;
        }

        await component.RespondAsync("Encounter has ended.");
        return true;
    }
}
