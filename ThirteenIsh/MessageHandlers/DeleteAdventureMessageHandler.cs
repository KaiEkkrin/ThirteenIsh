using Discord;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(DeleteAdventureMessage))]
internal sealed class DeleteAdventureMessageHandler(SqlDataService dataService) : MessageHandlerBase<DeleteAdventureMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        DeleteAdventureMessage message, CancellationToken cancellationToken = default)
    {
        var adventure = await dataService.DeleteAdventureAsync(message.GuildId, message.Name, cancellationToken);
        if (adventure == null)
        {
            await interaction.RespondAsync(
                $"Cannot delete an adventure named '{message.Name}'. Perhaps it was already deleted?",
                ephemeral: true);
            return true;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(interaction.User);
        embedBuilder.WithTitle($"Deleted adventure: {message.Name}");

        await interaction.RespondAsync(embed: embedBuilder.Build());
        return true;
    }
}
