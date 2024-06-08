using Discord;
using ThirteenIsh.ChannelMessages.Gm;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Gm;

[MessageHandler(MessageType = typeof(GmCombatBeginMessage))]
internal sealed class GmCombatBeginMessageHandler(SqlDataService dataService, DiscordService discordService,
    PinnedMessageService pinnedMessageService) : MessageHandlerBase<GmCombatBeginMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        GmCombatBeginMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.AddEncounterAsync(message.GuildId, message.ChannelId, cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            async output =>
            {
                var gameSystem = GameSystem.Get(output.Adventure.GameSystem);
                var encounterTable = await gameSystem.BuildEncounterTableAsync(dataService,
                    output.Encounter, cancellationToken);

                var channel = await discordService.GetGuildMessageChannelAsync(message.GuildId, message.ChannelId)
                    ?? throw new InvalidOperationException($"No channel for guild {message.GuildId}, channel {message.ChannelId}");

                await pinnedMessageService.SetEncounterMessageAsync(channel, output.Encounter.AdventureName,
                    message.GuildId, encounterTable, cancellationToken);

                await interaction.ModifyOriginalResponseAsync(properties => properties.Content = "Encounter begun.");
            });

        return true;
    }
}
