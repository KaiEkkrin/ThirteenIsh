using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class EditCharacterCommand : CharacterCommandBase
{
    public EditCharacterCommand() : base("edit-character", "Edits a character")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The character name",
            isRequired: true);

        return builder;
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!TryGetCanonicalizedMultiPartOption(command.Data, "name", out var name))
        {
            await command.RespondAsync("Character names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.UpdateCharacterAsync(name, UpdateSheet, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync($"Cannot edit a character named '{name}'. Perhaps they do not exist?",
                ephemeral: true);
            return;
        }

        await RespondWithCharacterSheetAsync(command, character.Sheet, $"Edited character: {character.Name}");

        void UpdateSheet(CharacterSheet sheet)
        {
            ApplyCharacterSheetOptions(command, sheet);
        }
    }
}
