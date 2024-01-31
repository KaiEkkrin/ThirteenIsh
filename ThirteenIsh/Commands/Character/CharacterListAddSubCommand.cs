using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterListAddSubCommand() : SubCommandBase("add", "Adds a new character.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddCharacterOption("name")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("class")
                .WithDescription("The character class.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String)
                );
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync("Character names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var sheet = CharacterSheet.CreateDefault();
        if (CommandUtil.TryGetCanonicalizedOption(option, "class", out var characterClass))
        {
            sheet.Class = characterClass;
        }

        if (CommandUtil.TryGetOption<int>(option, "level", out var level))
        {
            sheet.Level = level;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.CreateCharacterAsync(name, sheet, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync($"Cannot create a character named '{name}'. Perhaps one already exists?",
                ephemeral: true);
            return;
        }

        await CommandUtil.RespondWithCharacterSheetAsync(command, character.Sheet, $"Created character: {character.Name}");
    }
}
