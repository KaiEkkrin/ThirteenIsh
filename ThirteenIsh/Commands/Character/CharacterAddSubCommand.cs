using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterAddSubCommand(CharacterType characterType)
    : SubCommandBase("add", $"Adds a new {characterType.FriendlyName()}.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, $"The {characterType.FriendlyName()} name.",
                isRequired: true)
            .AddOption(GameSystem.BuildGameSystemChoiceOption("game-system"));
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync(
                $"{characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} names must contain only letters and spaces",
                ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "game-system", out var gameSystemName) ||
            GameSystem.AllGameSystems.FirstOrDefault(o => o.Name == gameSystemName) is not { } gameSystem)
        {
            await command.RespondAsync("Must choose a recognised game system", ephemeral: true);
            return;
        }

        // Add the character
        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var character = await dataService.CreateCharacterAsync(name, characterType, gameSystemName,
            command.User.Id, gameSystem.GetCharacterSystem(characterType, null).SetNewCharacterStartingValues, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync(
                $"Error creating {characterType.FriendlyName()} '{name}'. Perhaps a character or monster with that name already exists.",
                ephemeral: true);
            return;
        }

        // If we don't have show-on-add properties, we've finished
        var characterSystem = gameSystem.GetCharacterSystem(characterType, null);
        var showOnAddProperties = characterSystem.GetShowOnAddProperties().ToList();
        if (showOnAddProperties.Count == 0)
        {
            await CommandUtil.RespondWithCharacterSheetAsync(command, character,
                $"Added {characterType.FriendlyName()} '{character.Name}'", null);

            return;
        }

        // This will prompt the user to select the show-on-add properties
        AddCharacterMessage message = new()
        {
            CharacterType = characterType,
            Name = name,
            UserId = command.User.Id
        };
        await dataService.AddMessageAsync(message, cancellationToken);

        ComponentBuilder componentBuilder = new();
        foreach (var property in showOnAddProperties)
        {
            if (!characterSystem.TryBuildPropertyValueChoiceComponent(
                message.GetMessageId(property.Name), property.Name, character.Sheet, out var menuBuilder, out var errorMessage))
            {
                // The rest of the logic assumes this won't happen
                throw new InvalidOperationException(errorMessage);
            }

            componentBuilder.WithSelectMenu(menuBuilder);
        }

        componentBuilder.WithButton("Done", message.GetMessageId(AddCharacterMessage.DoneControlId))
            .WithButton("Cancel", message.GetMessageId(AddCharacterMessage.CancelControlId), ButtonStyle.Secondary);

        await command.RespondAsync($"Adding {characterType.FriendlyName()} '{name}'", ephemeral: true,
            components: componentBuilder.Build());
    }
}
