using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterListSubCommand() : SubCommandBase("list", "Lists your characters.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Characters");

        await foreach (var character in dataService.ListCharactersAsync(null, command.User.Id, cancellationToken))
        {
            var gameSystem = GameSystem.Get(character.GameSystem);
            var summary = gameSystem.Logic.GetCharacterSummary(character.Sheet);

            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName(character.Name)
                .WithValue($"[{gameSystem.Name}] {summary}"));
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
