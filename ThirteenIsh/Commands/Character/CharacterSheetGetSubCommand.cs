using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterSheetGetSubCommand() : SubCommandBase("get", "Gets a character's sheet.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddCharacterOption();
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "character", out var characterName))
        {
            await command.RespondAsync("Character not found", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(characterName, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync("Character not found", ephemeral: true);
            return;
        }

        await CommandUtil.RespondWithCharacterSheetAsync(command, character.Sheet, character.Name);
    }
}
