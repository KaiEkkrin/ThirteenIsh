using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterListSubCommand(CharacterType characterType)
    : SubCommandBase("list", $"Lists your {characterType.FriendlyName(FriendlyNameOptions.Plural)}.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dataService = serviceProvider.GetRequiredService<SqlDataService>();

        var embedBuilder = new EmbedBuilder()
            .WithTitle(characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter | FriendlyNameOptions.Plural));

        await foreach (var character in dataService.ListCharactersAsync(command.User.Id, characterType)
                                                   .WithCancellation(cancellationToken))
        {
            var gameSystem = GameSystem.Get(character.GameSystem);
            var summary = gameSystem.GetCharacterSummary(character.Sheet, characterType);

            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName(character.Name)
                .WithValue($"[{gameSystem.Name}] {summary}"));
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
