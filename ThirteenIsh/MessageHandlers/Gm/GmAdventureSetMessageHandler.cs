using Discord;
using ThirteenIsh.ChannelMessages.Gm;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Gm;

[MessageHandler(MessageType = typeof(GmAdventureSetMessage))]
internal sealed class GmAdventureSetMessageHandler(SqlDataService dataService, DiscordService discordService)
    : MessageHandlerBase<GmAdventureSetMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        GmAdventureSetMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditAdventureAsync(
            message.GuildId, new EditOperation(message.Description), message.Name, cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            async adventure =>
            {
                var embed = await discordService.BuildAdventureSummaryEmbedAsync(dataService, interaction, adventure,
                    message.Name);

                return await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    private sealed class EditOperation(string description) : SyncEditOperation<Adventure, Adventure>
    {
        public override EditResult<Adventure> DoEdit(DataContext context, Adventure adventure)
        {
            adventure.Description = description;
            return new EditResult<Adventure>(adventure);
        }
    }
}
