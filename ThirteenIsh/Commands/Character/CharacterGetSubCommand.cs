using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterGetSubCommand(CharacterType characterType)
    : SubCommandBase("get", $"Gets a {characterType.FriendlyName()}'s sheet.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, $"The {characterType.FriendlyName()} name.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync(
                $"{characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, characterType,
            false, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync(
                $"Error getting {characterType.FriendlyName()} '{name}'. Perhaps they do not exist, or there is more than one character or monster matching that name?",
                ephemeral: true);
            return;
        }

        await CommandUtil.RespondWithCharacterSheetAsync(command, character, character.Name);
    }
}
