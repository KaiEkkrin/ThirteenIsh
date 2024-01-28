using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class ShowCharacterCommand : CommandBase
{
    public ShowCharacterCommand() : base("show-character", "Shows the details of a character")
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
            await command.RespondAsync("Character not found");
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync("Character not found");
            return;
        }

        await RespondWithCharacterSheetAsync(command, character.Sheet, character.Name);
    }
}
