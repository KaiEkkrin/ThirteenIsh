using Discord.WebSocket;
using ThirteenIsh.Commands;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities.Messages;

/// <summary>
/// For adding a character. Forms a wizard that walks you through the character
/// properties group by group.
/// </summary>
public class AddCharacterMessage : MessageBase
{
    public const string CancelControlId = "Cancel";
    public const string DoneControlId = "Done";

    /// <summary>
    /// The character name to edit.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public override async Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        if (controlId == CancelControlId)
        {
            await component.RespondAsync(
                "Add cancelled. The character has already been saved; you can set more properties with the `character set` command, or delete them with `character remove.`",
                ephemeral: true);
            return true;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(Name, component.User.Id, cancellationToken);
        if (character is null)
        {
            await component.RespondAsync($"Cannot find a character named '{Name}'. Perhaps they were deleted?",
                ephemeral: true);
            return true;
        }

        var gameSystem = GameSystem.Get(character.GameSystem);
        if (gameSystem is null)
        {
            await component.RespondAsync($"Cannot find a game system named '{character.GameSystem}'.",
                ephemeral: true);
            return true;
        }

        if (controlId == DoneControlId)
        {
            // Edit completed
            await CommandUtil.RespondWithCharacterSheetAsync(component, character, $"Added '{Name}'");
            return true;
        }

        // If we got here, we're setting a property value
        var property = gameSystem.GetProperty(controlId);
        if (property is null)
        {
            await component.RespondAsync($"Cannot find a character property '{controlId}'.",
                ephemeral: true);
            return true;
        }

        var newValue = component.Data.Values.SingleOrDefault() ?? string.Empty;
        var (_, errorMessage) = await dataService.EditCharacterAsync(
            Name, new SetCharacterPropertyOperation(property, newValue), component.User.Id, cancellationToken);

        if (errorMessage is not null)
        {
            await component.RespondAsync(errorMessage, ephemeral: true);
            return true;
        }

        await component.DeferAsync(true);
        return false; // keep this message around, the user might make more selections
    }
}
