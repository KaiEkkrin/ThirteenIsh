using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcUpdateSubCommand() : SubCommandBase("update", "Syncs the base character sheet with an adventurer.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var (updatedAdventurer, errorMessage) = await dataService.EditGuildAsync(
            new EditOperation(command, dataService), guildId, cancellationToken);

        if (errorMessage is not null)
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
            return;
        }

        if (updatedAdventurer is null) throw new InvalidOperationException("updatedAdventurer was null after update");
        await CommandUtil.RespondWithAdventurerSummaryAsync(command, updatedAdventurer, $"Updated {updatedAdventurer.Name}");
    }

    private sealed class EditOperation(SocketSlashCommand command, DataService dataService)
        : EditOperation<ResultOrMessage<Adventurer>, Guild, MessageEditResult<Adventurer>>
    {
        public override async Task<MessageEditResult<Adventurer>> DoEditAsync(Guild guild, CancellationToken cancellationToken)
        {
            if (guild.CurrentAdventure is not { } currentAdventure)
                return new MessageEditResult<Adventurer>(null, "There is no current adventure in this guild.");

            if (!currentAdventure.Adventurers.TryGetValue(command.User.Id, out var adventurer))
                return new MessageEditResult<Adventurer>(null, "You have not joined the current adventure.");

            var character = await dataService.GetCharacterAsync(adventurer.Name, command.User.Id, cancellationToken);
            if (character is null)
                return new MessageEditResult<Adventurer>(null, $"Character {adventurer.Name} not found.");

            adventurer.LastUpdated = DateTimeOffset.Now;
            adventurer.Sheet = character.Sheet;
            return new MessageEditResult<Adventurer>(adventurer);
        }
    }
}
