using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureCombatBeginCommand() : SubCommandBase("begin", "Begins an encounter in this channel.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var (output, message) = await dataService.EditGuildAsync(
            new EditOperation(channelId), guildId, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(message, ephemeral: true);
            return;
        }

        if (output is null) throw new InvalidOperationException(nameof(output));

        var encounterTable = output.GameSystem.EncounterTable(output.Adventure, output.Encounter);
        var pinnedMessageService = serviceProvider.GetRequiredService<PinnedMessageService>();
        await pinnedMessageService.SetEncounterMessageAsync(command.Channel, output.Encounter.AdventureName, guildId,
            encounterTable, cancellationToken);

        await command.RespondAsync("Encounter begun.", ephemeral: true);
    }


    private sealed class EditOperation(ulong channelId)
        : SyncEditOperation<ResultOrMessage<EditOutput>, Guild, MessageEditResult<EditOutput>>
    {
        public override MessageEditResult<EditOutput> DoEdit(Guild guild)
        {
            if (guild.Encounters.ContainsKey(channelId))
                return new MessageEditResult<EditOutput>(null, "There is already an active encounter in this channel.");

            if (guild.CurrentAdventure is null)
                return new MessageEditResult<EditOutput>(null, "There is no current adventure.");

            Encounter encounter = new()
            {
                AdventureName = guild.CurrentAdventure.Name
            };

            var gameSystem = GameSystem.Get(guild.CurrentAdventure.GameSystem);
            gameSystem.EncounterBegin(encounter);

            guild.Encounters.Add(channelId, encounter);
            return new MessageEditResult<EditOutput>(new EditOutput(guild.CurrentAdventure, gameSystem, guild, encounter));
        }
    }

    private sealed record EditOutput(Adventure Adventure, GameSystem GameSystem, Guild Guild, Encounter Encounter);
}
