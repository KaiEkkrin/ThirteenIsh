using Discord;
using Discord.WebSocket;
using ThirteenIsh.Commands;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Character;

[MessageHandler(MessageType = typeof(AddCharacterMessage))]
internal sealed class AddCharacterMessageHandler(SqlDataService dataService) : MessageHandlerBase<AddCharacterMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        AddCharacterMessage message, CancellationToken cancellationToken = default)
    {
        if (controlId == AddCharacterMessage.CancelControlId)
        {
            await interaction.RespondAsync(
                $"Add cancelled. The {message.CharacterType.FriendlyName()} has already been saved; you can set more properties with the `{message.CharacterType.FriendlyName()} set` command, or delete them with `{message.CharacterType.FriendlyName()} remove.`",
                ephemeral: true);
            return true;
        }

        var character = await dataService.GetCharacterAsync(message.Name, interaction.User.Id, message.CharacterType,
            cancellationToken: cancellationToken);
        if (character is null)
        {
            await interaction.RespondAsync(
                $"Cannot find a {message.CharacterType.FriendlyName()} named '{message.Name}'. Perhaps they were deleted?",
                ephemeral: true);
            return true;
        }

        var gameSystem = GameSystem.Get(character.GameSystem);
        if (gameSystem is null)
        {
            await interaction.RespondAsync($"Cannot find a game system named '{character.GameSystem}'.",
                ephemeral: true);
            return true;
        }

        if (controlId == AddCharacterMessage.DoneControlId)
        {
            // Edit completed
            await CommandUtil.RespondWithCharacterSheetAsync(interaction, character,
                $"Added {message.CharacterType.FriendlyName()} '{message.Name}'", null);
            return true;
        }

        // If we got here, we're setting a property value
        var characterSystem = gameSystem.GetCharacterSystem(message.CharacterType, character.CharacterSystemName);
        var property = characterSystem.GetProperty(controlId);
        if (property is null)
        {
            await interaction.RespondAsync(
                $"Cannot find a {message.CharacterType.FriendlyName()} property '{controlId}'.",
                ephemeral: true);
            return true;
        }

        var newValue = interaction is SocketMessageComponent component
            ? component.Data.Values.SingleOrDefault() ?? string.Empty
            : throw new InvalidOperationException($"Unexpected interaction type: {interaction.GetType()}");

        var result = await dataService.EditCharacterAsync(
            message.Name, new SetCharacterPropertyOperation(property, newValue), interaction.User.Id, message.CharacterType,
            cancellationToken);

        return await result.Handle(
            async errorMessage =>
            {
                await interaction.RespondAsync(errorMessage, ephemeral: true);
                return true;
            },
            async _ =>
            {
                await interaction.DeferAsync(true);
                return false; // keep this message around, the user might make more selections
            });
    }
}
