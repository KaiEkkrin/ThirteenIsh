using Discord;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(EndEncounterMessage))]
internal sealed class EndEncounterMessageHandler(DiscordService discordService,
    SqlDataService dataService)
    : MessageHandlerBase<EndEncounterMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        EndEncounterMessage message, CancellationToken cancellationToken = default)
    {
        var encounter = await dataService.DeleteEncounterAsync(message.GuildId, message.ChannelId, cancellationToken);
        if (encounter == null)
        {
            await interaction.RespondAsync("There is no active encounter in this channel.", ephemeral: true);
            return true;
        }

        if (encounter.PinnedMessageId is { } pinnedMessageId)
        {
            var channel = await discordService.GetGuildMessageChannelAsync(message.GuildId,
                message.ChannelId);

            if (channel != null)
                await PinnedMessageService.DeleteEncounterMessageAsync(channel, pinnedMessageId);
        }

        await interaction.RespondAsync("Encounter has ended.");
        return true;
    }
}
