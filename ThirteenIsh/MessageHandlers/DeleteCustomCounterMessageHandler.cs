using Discord;
using ThirteenIsh.Database;
using ThirteenIsh.Game;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(DeleteCustomCounterMessage))]
internal sealed class DeleteCustomCounterMessageHandler(SqlDataService dataService)
    : MessageHandlerBase<DeleteCustomCounterMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        DeleteCustomCounterMessage message, CancellationToken cancellationToken = default)
    {
        var result = await dataService.EditCharacterAsync(message.Name,
            new EditOperation(message.CharacterType, message.CcName), message.UserId, message.CharacterType,
            cancellationToken);

        await result.Handle(
            error => interaction.RespondAsync(error, ephemeral: true),
            updatedCharacter =>
            {
                EmbedBuilder embedBuilder = new();
                embedBuilder.WithAuthor(interaction.User);
                embedBuilder.WithTitle(
                    $"Removed '{message.CcName}' from {message.CharacterType.FriendlyName()} '{updatedCharacter.Name}'");

                return interaction.RespondAsync(embed: embedBuilder.Build());
            });

        return true;
    }

    private sealed class EditOperation(CharacterType characterType, string ccName)
        : SyncEditOperation<Database.Entities.Character, Database.Entities.Character>
    {
        public override EditResult<Database.Entities.Character> DoEdit(DataContext context, Database.Entities.Character character)
        {
            var maybeIndex = character.Sheet.CustomCounters
                ?.FindIndex(cc => cc.Name.Equals(ccName, StringComparison.OrdinalIgnoreCase));

            if (maybeIndex is not (>= 0 and { } index))
            {
                return CreateError(
                    $"The {characterType.FriendlyName()} '{character.Name}' has no custom counter named '{ccName}'. Perhaps it was already deleted?");
            }

            character.Sheet.CustomCounters!.RemoveAt(index);
            return new EditResult<Database.Entities.Character>(character);
        }
    }
}
