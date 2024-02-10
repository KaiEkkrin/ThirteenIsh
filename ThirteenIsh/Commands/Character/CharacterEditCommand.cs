using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities.Messages;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

// TODO this is very cumbersome
// Keep it, but also make a `character set` command that lets you set a property by name and value,
// parsing them (as forgivingly as I can)
internal sealed class CharacterEditCommand() : SubCommandBase("edit", "Edits a character.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The character name.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync("Character names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync($"Error getting character '{name}'. Perhaps they do not exist?",
                ephemeral: true);
            return;
        }

        // Here I need to respond with a property group selection, and continue the edit via EditCharacterMessage:
        EditCharacterMessage message = new()
        {
            Name = name,
            UserId = (long)command.User.Id
        };
        await dataService.AddMessageAsync(message, cancellationToken);

        var gameSystem = GameSystem.Get(character.GameSystem);
        var componentBuilder = new ComponentBuilder()
            .WithSelectMenu(gameSystem.BuildPropertyGroupChoiceComponent(
                message.GetMessageId(EditCharacterMessage.PropertyGroupControlId),
                property => property.CanStore))
            .WithButton("Cancel", message.GetMessageId(EditCharacterMessage.CancelControlId));

        await command.RespondAsync($"Editing '{name}' : Select a property group", ephemeral: true,
            components: componentBuilder.Build());
    }
}
