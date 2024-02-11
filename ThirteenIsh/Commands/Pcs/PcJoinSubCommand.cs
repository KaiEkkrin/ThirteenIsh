using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
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

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(characterName, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync("Character not found", ephemeral: true);
            return;
        }

        var (updatedAdventure, errorMessage) = await dataService.EditGuildAsync(
            new EditOperation(command, character), guildId, cancellationToken);

        if (errorMessage is not null)
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
            return;
        }

        if (updatedAdventure is null) throw new InvalidOperationException("updatedAdventure was null after update");

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle($"Joined {updatedAdventure.Name} as {character.Name}");

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    private sealed class EditOperation(SocketSlashCommand command, Entities.Character character)
        : SyncEditOperation<ResultOrMessage<Adventure>, Guild, MessageEditResult<Adventure>>
    {
        public override MessageEditResult<Adventure> DoEdit(Guild guild)
        {
            if (guild.CurrentAdventure is not { } currentAdventure)
                return new MessageEditResult<Adventure>(null, "There is no current adventure in this guild.");

            if (guild.CurrentAdventure.GameSystem != character.GameSystem)
                return new MessageEditResult<Adventure>(null,
                    "This character was not created in the same game system as the adventure.");

            if (!currentAdventure.Adventurers.TryGetValue(command.User.Id, out var adventurer))
            {
                adventurer = new Adventurer()
                {
                    Name = character.Name,
                    LastUpdated = DateTimeOffset.Now,
                    Sheet = character.Sheet
                };

                var gameSystem = GameSystem.Get(character.GameSystem);
                gameSystem.ResetVariables(adventurer);
                currentAdventure.Adventurers.Add(command.User.Id, adventurer);

                return new MessageEditResult<Adventure>(currentAdventure);
            }
            else if (adventurer.Name == character.Name)
            {
                return new MessageEditResult<Adventure>(null, "This character is already joined to the current adventure.");
            }
            else
            {
                return new MessageEditResult<Adventure>(null,
                    "You have already joined this adventure with a different character.");
            }
        }
    }
}
