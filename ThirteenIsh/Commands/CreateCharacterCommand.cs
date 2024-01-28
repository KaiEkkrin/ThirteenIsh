using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class CreateCharacterCommand : CharacterCommandBase
{
    public CreateCharacterCommand() : base("create-character", "Creates a character")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The character name",
            isRequired: true);

        builder.AddOption("class", ApplicationCommandOptionType.String, "The character's class",
            isRequired: true);

        return AddCharacterSlashCommands(builder);
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!TryGetCanonicalizedMultiPartOption(command.Data, "name", out var name))
        {
            await command.RespondAsync("Character names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var sheet = CharacterSheet.CreateDefault();
        ApplyCharacterSheetOptions(command, sheet);

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.CreateCharacterAsync(name, sheet, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync($"Cannot create a character named '{name}'. Perhaps one already exists?",
                ephemeral: true);
            return;
        }

        await RespondWithCharacterSheetAsync(command, character.Sheet, $"Created character: {character.Name}");
    }
}
