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
        var result = await dataService.EditAdventurerAsync(
            guildId, command.User.Id, new EditOperation(dataService, command), cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            adventurer =>
            {
                var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
                return CommandUtil.RespondWithTrackedCharacterSummaryAsync(command, adventurer, gameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        Flags = CommandUtil.AdventurerSummaryFlags.WithTags,
                        Title = $"Updated {adventurer.Name}"
                    });
            });
    }

    private sealed class EditOperation(SqlDataService dataService, SocketSlashCommand command)
        : EditOperation<Adventurer, Adventurer>
    {
        public override async Task<EditResult<Adventurer>> DoEditAsync(DataContext context, Adventurer adventurer,
            CancellationToken cancellationToken)
        {
            var character = await dataService.GetCharacterAsync(adventurer.Name, command.User.Id, CharacterType.PlayerCharacter,
                false, cancellationToken);

            if (character is null)
                return new EditResult<Adventurer>(null, $"Character {adventurer.Name} not found.");

            adventurer.LastUpdated = DateTimeOffset.UtcNow;
            adventurer.Sheet = character.Sheet;
            return new EditResult<Adventurer>(adventurer);
        }
    }
}
