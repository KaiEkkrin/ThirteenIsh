using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureCombatBeginCommand() : SubCommandBase("begin", "Begins an encounter in this channel.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var (output, message) = await dataService.AddEncounterAsync(guildId, channelId, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(message, ephemeral: true);
            return;
        }

        if (output is null) throw new InvalidOperationException(nameof(output));

        var gameSystem = GameSystem.Get(output.Adventure.GameSystem);
        var encounterTable = await gameSystem.BuildEncounterTableAsync(dataService, output.Adventure,
            output.Encounter, cancellationToken);

        var pinnedMessageService = serviceProvider.GetRequiredService<PinnedMessageService>();
        await pinnedMessageService.SetEncounterMessageAsync(command.Channel, output.Encounter.AdventureName, guildId,
            encounterTable, cancellationToken);

        await command.RespondAsync("Encounter begun.", ephemeral: true);
    }
}
