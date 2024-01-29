using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Messages;

internal sealed class DeleteCharacterMessage(string name, ulong userId) : MessageBase(userId)
{
    protected override async Task HandleInternalAsync(SocketMessageComponent component, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var deleted = await dataService.DeleteCharacterAsync(name, UserId, cancellationToken);
        if (!deleted)
        {
            await component.RespondAsync(
                $"Cannot delete a character named '{name}'. Perhaps they were already deleted?",
                ephemeral: true);
            return;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(component.User);
        embedBuilder.WithTitle($"Deleted character: {name}");

        await component.RespondAsync(embed: embedBuilder.Build());
    }
}
