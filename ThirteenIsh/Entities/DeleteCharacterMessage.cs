using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities;

public class DeleteCharacterMessage : MessageBase
{
    /// <summary>
    /// The character name to delete.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public override async Task HandleAsync(SocketMessageComponent component, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var deleted = await dataService.DeleteCharacterAsync(Name, UserId.Value, cancellationToken);
        if (!deleted)
        {
            await component.RespondAsync(
                $"Cannot delete a character named '{Name}'. Perhaps they were already deleted?",
                ephemeral: true);
            return;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(component.User);
        embedBuilder.WithTitle($"Deleted character: {Name}");

        await component.RespondAsync(embed: embedBuilder.Build());
    }
}
