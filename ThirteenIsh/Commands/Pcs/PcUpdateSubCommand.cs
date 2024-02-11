using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcUpdateSubCommand() : SubCommandBase("update", "Syncs the base character sheet with an adventurer.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var (updatedAdventure, errorMessage) = await dataService.EditGuildAsync(
            new EditOperation(command, dataService), guildId, cancellationToken);

        if (errorMessage is not null)
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
            return;
        }

        if (updatedAdventure is null) throw new InvalidOperationException("updatedAdventure was null after update");

        var gameSystem = GameSystem.Get(updatedAdventure.GameSystem);
        var adventurer = updatedAdventure.Adventurers[command.User.Id];

        await CommandUtil.RespondWithAdventurerSummaryAsync(command, adventurer, gameSystem,
            new CommandUtil.AdventurerSummaryOptions
            {
                OnlyVariables = false,
                Title = $"Updated {adventurer.Name}"
            });
    }

    private sealed class EditOperation(SocketSlashCommand command, DataService dataService)
        : EditOperation<ResultOrMessage<Adventure>, Guild, MessageEditResult<Adventure>>
    {
        public override async Task<MessageEditResult<Adventure>> DoEditAsync(Guild guild, CancellationToken cancellationToken)
        {
            if (guild.CurrentAdventure is not { } currentAdventure)
                return new MessageEditResult<Adventure>(null, "There is no current adventure in this guild.");

            if (!currentAdventure.Adventurers.TryGetValue(command.User.Id, out var adventurer))
                return new MessageEditResult<Adventure>(null, "You have not joined the current adventure.");

            var character = await dataService.GetCharacterAsync(adventurer.Name, command.User.Id, cancellationToken);
            if (character is null)
                return new MessageEditResult<Adventure>(null, $"Character {adventurer.Name} not found.");

            adventurer.LastUpdated = DateTimeOffset.Now;
            adventurer.Sheet = character.Sheet;
            return new MessageEditResult<Adventure>(currentAdventure);
        }
    }
}
