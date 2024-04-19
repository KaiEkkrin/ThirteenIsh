using Discord.WebSocket;
using ThirteenIsh.Commands;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(AddCharacterMessage))]
internal sealed class AddCharacterMessageHandler(SqlDataService dataService) : MessageHandlerBase<AddCharacterMessage>
{
    protected override async Task<bool> HandleInternalAsync(SocketMessageComponent component, string controlId,
        AddCharacterMessage message, CancellationToken cancellationToken = default)
    {
        if (controlId == AddCharacterMessage.CancelControlId)
        {
            await component.RespondAsync(
                $"Add cancelled. The {message.CharacterType.FriendlyName()} has already been saved; you can set more properties with the `{message.CharacterType.FriendlyName()} set` command, or delete them with `{message.CharacterType.FriendlyName()} remove.`",
                ephemeral: true);
            return true;
        }

        var character = await dataService.GetCharacterAsync(message.Name, component.User.Id, message.CharacterType,
            cancellationToken: cancellationToken);
        if (character is null)
        {
            await component.RespondAsync(
                $"Cannot find a {message.CharacterType.FriendlyName()} named '{message.Name}'. Perhaps they were deleted?",
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

        if (controlId == AddCharacterMessage.DoneControlId)
        {
            // Edit completed
            await CommandUtil.RespondWithCharacterSheetAsync(component, character,
                $"Added {message.CharacterType.FriendlyName()} '{message.Name}'");
            return true;
        }

        // If we got here, we're setting a property value
        var characterSystem = gameSystem.GetCharacterSystem(message.CharacterType);
        var property = characterSystem.GetProperty(controlId);
        if (property is null)
        {
            await component.RespondAsync(
                $"Cannot find a {message.CharacterType.FriendlyName()} property '{controlId}'.",
                ephemeral: true);
            return true;
        }

        var newValue = component.Data.Values.SingleOrDefault() ?? string.Empty;
        var (_, errorMessage) = await dataService.EditCharacterAsync(
            message.Name, new SetCharacterPropertyOperation(property, newValue), component.User.Id, message.CharacterType,
            cancellationToken);

        if (errorMessage is not null)
        {
            await component.RespondAsync(errorMessage, ephemeral: true);
            return true;
        }

        await component.DeferAsync(true);
        return false; // keep this message around, the user might make more selections
    }
}
