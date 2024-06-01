using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmCombatBeginSubCommand() : SubCommandBase("begin", "Begins an encounter in this channel.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var result = await dataService.AddEncounterAsync(guildId, channelId, cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            async output =>
            {
                await command.DeferAsync(ephemeral: true);

                var gameSystem = GameSystem.Get(output.Adventure.GameSystem);
                var encounterTable = await gameSystem.BuildEncounterTableAsync(dataService,
                    output.Encounter, cancellationToken);

                var pinnedMessageService = serviceProvider.GetRequiredService<PinnedMessageService>();
                await pinnedMessageService.SetEncounterMessageAsync(command.Channel, output.Encounter.AdventureName,
                    guildId, encounterTable, cancellationToken);

                await command.ModifyOriginalResponseAsync(properties => properties.Content = "Encounter begun.");
            });
    }
}
