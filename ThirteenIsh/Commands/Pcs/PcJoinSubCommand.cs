using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcJoinSubCommand() : SubCommandBase("join", "Joins the current adventure.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddCharacterOption("name");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var characterName))
        {
            await command.RespondAsync("Character names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var character = await dataService.GetCharacterAsync(characterName, command.User.Id, CharacterType.PlayerCharacter,
            false, cancellationToken);

        if (character is null)
        {
            await command.RespondAsync("Character not found", ephemeral: true);
            return;
        }

        var result = await dataService.EditAdventureAsync(
            guildId, new EditOperation(dataService, character), cancellationToken: cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            adventure =>
            {
                EmbedBuilder embedBuilder = new();
                embedBuilder.WithAuthor(command.User);
                embedBuilder.WithTitle($"Joined {adventure.Name} as {character.Name}");

                return command.RespondAsync(embed: embedBuilder.Build());
            });
    }

    private sealed class EditOperation(SqlDataService dataService, Database.Entities.Character character)
        : EditOperation<Adventure, Adventure>
    {
        public override async Task<EditResult<Adventure>> DoEditAsync(DataContext context, Adventure adventure,
            CancellationToken cancellationToken = default)
        {
            if (adventure.GameSystem != character.GameSystem)
                return CreateError("This character was not created in the same game system as the adventure.");

            var currentAdventurer = await dataService.GetAdventurerAsync(adventure, character.UserId,
                cancellationToken);
            if (currentAdventurer == null)
            {
                Adventurer adventurer = new()
                {
                    Name = character.Name,
                    LastUpdated = DateTimeOffset.UtcNow,
                    Sheet = character.Sheet,
                    UserId = character.UserId
                };

                var gameSystem = GameSystem.Get(character.GameSystem);
                var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
                characterSystem.ResetVariables(adventurer);
                adventure.Adventurers.Add(adventurer);
                return new EditResult<Adventure>(adventure);
            }
            else if (currentAdventurer.Name == character.Name)
            {
                return CreateError("This character is already joined to the current adventure.");
            }
            else
            {
                return CreateError("You have already joined this adventure with a different character.");
            }
        }
    }
}
