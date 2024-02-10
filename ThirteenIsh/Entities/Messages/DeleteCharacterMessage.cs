using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities.Messages;

public class DeleteCharacterMessage : MessageBase
{
    /// <summary>
    /// The character name to delete.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public override async Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var deleted = await dataService.DeleteCharacterAsync(Name, NativeUserId, cancellationToken);
        if (!deleted)
        {
            await component.RespondAsync(
                $"Cannot delete a character named '{Name}'. Perhaps they were already deleted?",
                ephemeral: true);
            return true;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(component.User);
        embedBuilder.WithTitle($"Deleted character: {Name}");

        await component.RespondAsync(embed: embedBuilder.Build());
        return true;
    }
}
