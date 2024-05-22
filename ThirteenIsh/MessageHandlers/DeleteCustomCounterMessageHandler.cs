using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(DeleteCustomCounterMessage))]
internal sealed class DeleteCustomCounterMessageHandler(SqlDataService dataService)
    : MessageHandlerBase<DeleteCustomCounterMessage>
{
    protected override async Task<bool> HandleInternalAsync(SocketMessageComponent component, string controlId,
        DeleteCustomCounterMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditCharacterAsync(message.Name,
            new EditOperation(message.CharacterType, message.CcName), message.UserId, message.CharacterType,
            cancellationToken);

        await result.Handle(
            error => component.RespondAsync(error, ephemeral: true),
            updatedCharacter =>
            {
                EmbedBuilder embedBuilder = new();
                embedBuilder.WithAuthor(component.User);
                embedBuilder.WithTitle(
                    $"Removed '{message.CcName}' from {message.CharacterType.FriendlyName()} '{updatedCharacter.Name}'");

                return component.RespondAsync(embed: embedBuilder.Build());
            });

        return true;
    }

    private sealed class EditOperation(CharacterType characterType, string ccName) : SyncEditOperation<Character, Character>
    {
        public override EditResult<Character> DoEdit(DataContext context, Character character)
        {
            var maybeIndex = character.Sheet.CustomCounters
                ?.FindIndex(cc => cc.Name.Equals(ccName, StringComparison.OrdinalIgnoreCase));

            if (maybeIndex is not (>= 0 and { } index))
            {
                return CreateError(
                    $"The {characterType.FriendlyName()} '{character.Name}' has no custom counter named '{ccName}'. Perhaps it was already deleted?");
            }

            character.Sheet.CustomCounters!.RemoveAt(index);
            return new EditResult<Character>(character);
        }
    }
}
