using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterListGetSubCommand() : SubCommandBase("get", "Lists your characters.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle("Characters");

        // TODO limit/paginate/what have you?  (eventually)
        await foreach (var character in dataService.ListCharactersAsync(
            userId: command.User.Id, cancellationToken: cancellationToken))
        {
            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName(character.Name)
                .WithValue($"Level {character.Sheet.Level} {character.Sheet.Class}"));
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
