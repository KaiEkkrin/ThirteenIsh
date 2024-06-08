using Discord;
using ThirteenIsh.ChannelMessages.Gm;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Gm;

[MessageHandler(MessageType = typeof(GmAdventureSwitchMessage))]
internal sealed class GmAdventureSwitchMessageHandler(SqlDataService dataService, DiscordService discordService)
    : MessageHandlerBase<GmAdventureSwitchMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        GmAdventureSwitchMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditAdventureAsync(
            message.GuildId, new EditOperation(), message.Name, cancellationToken);

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

    private sealed class EditOperation() : SyncEditOperation<Adventure, Adventure>
    {
        public override EditResult<Adventure> DoEdit(DataContext context, Adventure adventure)
        {
            adventure.Guild.CurrentAdventureName = adventure.Name;
            return new EditResult<Adventure>(adventure);
        }
    }
}
