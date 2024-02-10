using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities.Messages;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterAddCommand() : SubCommandBase("add", "Adds a new character.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The character name.",
                isRequired: true)
            .AddOption(GameSystem.BuildGameSystemChoiceOption("game-system"));
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync("Character names must contain only letters and spaces", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "game-system", out var gameSystemName) ||
            GameSystem.AllGameSystems.FirstOrDefault(o => o.Name == gameSystemName) is not { } gameSystem)
        {
            await command.RespondAsync("Must choose a recognised game system", ephemeral: true);
            return;
        }

        // Add the character
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.CreateCharacterAsync(name, gameSystemName, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync($"Error creating character '{name}'. Perhaps a character with that name already exists.",
                ephemeral: true);
            return;
        }

        // This will prompt the user to select the show-on-add properties
        AddCharacterMessage message = new()
        {
            Name = name,
            UserId = (long)command.User.Id
        };
        await dataService.AddMessageAsync(message, cancellationToken);

        ComponentBuilder componentBuilder = new();
        foreach (var property in gameSystem.ShowOnAddProperties)
        {
            if (!gameSystem.TryBuildPropertyValueChoiceComponent(
                message.GetMessageId(property.Name), property.Name, character.Sheet, out var menuBuilder, out var errorMessage))
            {
                // The rest of the logic assumes this won't happen
                throw new InvalidOperationException(errorMessage);
            }

            componentBuilder.WithSelectMenu(menuBuilder);
        }

        componentBuilder.WithButton("Done", message.GetMessageId(AddCharacterMessage.DoneControlId))
            .WithButton("Cancel", message.GetMessageId(AddCharacterMessage.CancelControlId), ButtonStyle.Secondary);

        await command.RespondAsync($"Adding '{name}'", ephemeral: true,
            components: componentBuilder.Build());
    }
}
