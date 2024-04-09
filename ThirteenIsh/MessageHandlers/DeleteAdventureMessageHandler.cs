using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(DeleteAdventureMessage))]
internal sealed class DeleteAdventureMessageHandler(SqlDataService dataService) : MessageHandlerBase<DeleteAdventureMessage>
{
    protected override async Task<bool> HandleInternalAsync(SocketMessageComponent component, string controlId,
        DeleteAdventureMessage message, CancellationToken cancellationToken = default)
    {
        var adventure = await dataService.DeleteAdventureAsync(message.GuildId, message.Name, cancellationToken);
        if (adventure == null)
        {
            await component.RespondAsync(
                $"Cannot delete an adventure named '{message.Name}'. Perhaps it was already deleted?",
                ephemeral: true);
            return true;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(component.User);
        embedBuilder.WithTitle($"Deleted adventure: {message.Name}");

        await component.RespondAsync(embed: embedBuilder.Build());
        return true;
    }
}
