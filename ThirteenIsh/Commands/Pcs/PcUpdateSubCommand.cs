using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcUpdateSubCommand() : SubCommandBase("update", "Syncs the base character sheet with an adventurer.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var (result, errorMessage) = await dataService.EditAdventureAsync(
            guildId, new EditOperation(dataService, command), null, cancellationToken);

        if (errorMessage is not null)
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
            return;
        }

        if (result is null) throw new InvalidOperationException("result was null after update");

        var gameSystem = GameSystem.Get(result.Adventure.GameSystem);
        await CommandUtil.RespondWithAdventurerSummaryAsync(command, result.Adventurer, gameSystem,
            new CommandUtil.AdventurerSummaryOptions
            {
                OnlyVariables = false,
                Title = $"Updated {result.Adventurer.Name}"
            });
    }

    private sealed class EditOperation(SqlDataService dataService, SocketSlashCommand command)
        : EditOperation<ResultOrMessage<EditResult>, Adventure, MessageEditResult<EditResult>>
    {
        public override async Task<MessageEditResult<EditResult>> DoEditAsync(Adventure adventure,
            CancellationToken cancellationToken)
        {
            var adventurer = await dataService.GetAdventurerAsync(adventure, command.User.Id, cancellationToken);
            if (adventurer == null)
                return new MessageEditResult<EditResult>(null, "You have not joined the current adventure.");

            var character = await dataService.GetCharacterAsync(adventurer.Name, command.User.Id, CharacterType.PlayerCharacter,
                cancellationToken);

            if (character is null)
                return new MessageEditResult<EditResult>(null, $"Character {adventurer.Name} not found.");

            adventurer.LastUpdated = DateTimeOffset.Now;
            adventurer.Sheet = character.Sheet;
            return new MessageEditResult<EditResult>(new EditResult(adventure, adventurer));
        }
    }

    private record EditResult(Adventure Adventure, Adventurer Adventurer);
}
