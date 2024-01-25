using Discord;
using Discord.WebSocket;
using MongoDB.Driver;
using ThirteenIsh.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class ListCharactersCommand : CommandBase
{
    public ListCharactersCommand() : base("list-characters", "Lists saved characters")
    {
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle("characters");

        // TODO limit/paginate/what have you?  (eventually)
        using var cursor = await dataService.GetCharacters().FindAsync(
            Builders<Character>.Filter.Eq(o => o.UserId, (decimal)command.User.Id),
            cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var character in cursor.Current)
            {
                embedBuilder.AddField(new EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName(character.Name)
                    .WithValue("..."));
            }
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
