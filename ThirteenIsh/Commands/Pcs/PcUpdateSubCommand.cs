using Discord.WebSocket;
using ThirteenIsh.Database;
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
        var (adventurer, errorMessage) = await dataService.EditAdventurerAsync(
            guildId, command.User.Id, new EditOperation(dataService, command), cancellationToken);

        if (errorMessage is not null)
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
            return;
        }

        if (adventurer is null) throw new InvalidOperationException("result was null after update");

        var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
        await CommandUtil.RespondWithAdventurerSummaryAsync(command, adventurer, gameSystem,
            new CommandUtil.AdventurerSummaryOptions
            {
                OnlyVariables = false,
                Title = $"Updated {adventurer.Name}"
            });
    }

    private sealed class EditOperation(SqlDataService dataService, SocketSlashCommand command)
        : EditOperation<ResultOrMessage<Adventurer>, Adventurer, MessageEditResult<Adventurer>>
    {
        public override async Task<MessageEditResult<Adventurer>> DoEditAsync(DataContext context, Adventurer adventurer,
            CancellationToken cancellationToken)
        {
            var character = await dataService.GetCharacterAsync(adventurer.Name, command.User.Id, CharacterType.PlayerCharacter,
                false, cancellationToken);

            if (character is null)
                return new MessageEditResult<Adventurer>(null, $"Character {adventurer.Name} not found.");

            adventurer.LastUpdated = DateTimeOffset.UtcNow;
            adventurer.Sheet = character.Sheet;
            return new MessageEditResult<Adventurer>(adventurer);
        }
    }
}
