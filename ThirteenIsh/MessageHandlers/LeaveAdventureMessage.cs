using Discord.WebSocket;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(LeaveAdventureMessage))]
internal sealed class LeaveAdventureMessageHandler(SqlDataService dataService) : MessageHandlerBase<LeaveAdventureMessage>
{
    protected override async Task<bool> HandleInternalAsync(SocketMessageComponent component, string controlId,
        LeaveAdventureMessage message, CancellationToken cancellationToken = default)
    {
        var adventurer = await dataService.DeleteAdventurerAsync(message.GuildId, message.UserId, message.Name,
            cancellationToken);
        if (adventurer == null)
        {
            await component.RespondAsync($"You have not joined the adventure '{message.Name}'.", ephemeral: true);
            return true;
        }

        await component.RespondAsync($"'{adventurer.Name}' left adventure '{message.Name}'.");
        return true;
    }
}
