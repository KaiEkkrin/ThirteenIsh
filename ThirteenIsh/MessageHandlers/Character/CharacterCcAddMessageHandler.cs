using Discord;
using ThirteenIsh.ChannelMessages.Character;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Character;

[MessageHandler(MessageType = typeof(CharacterCcAddMessage))]
internal sealed class CharacterCcAddMessageHandler(SqlDataService dataService) : MessageHandlerBase<CharacterCcAddMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        CharacterCcAddMessage message, CancellationToken cancellationToken = default)
    {
        var character = await dataService.GetCharacterAsync(message.Name, message.UserId, message.CharacterType,
            cancellationToken: cancellationToken);
        if (character is null)
        {
            await interaction.ModifyOriginalResponseAsync(properties => properties.Content =
                $"Error getting {message.CharacterType.FriendlyName()} '{message.Name}'. Perhaps they do not exist, or there is more than one character or monster matching that name?");

            return true;
        }

        var result = await dataService.EditCharacterAsync(
            message.Name, new EditOperation(message.CcName, message.DefaultValue, message.GameCounterOptions), message.UserId,
            message.CharacterType, cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            updatedCharacter =>
            {
                var embed = CommandUtil.BuildCharacterSheetEmbed(interaction, updatedCharacter,
                    $"Edited {message.CharacterType.FriendlyName()} '{updatedCharacter.Name}'", [message.CcName]);

                return interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    private sealed class EditOperation(string ccName, int defaultValue, GameCounterOptions options)
        : SyncEditOperation<Database.Entities.Character, Database.Entities.Character>
    {
        public override EditResult<Database.Entities.Character> DoEdit(DataContext context,
            Database.Entities.Character character)
        {
            var existingCc = character.Sheet.CustomCounters
                ?.FirstOrDefault(cc => cc.Name.Equals(ccName, StringComparison.OrdinalIgnoreCase));

            if (existingCc != null)
            {
                return CreateError(
                    $"The character '{character.Name}' already has a custom counter named '{existingCc.Name}'");
            }

            character.Sheet.CustomCounters ??= [];
            character.Sheet.CustomCounters.Add(new CustomCounter(ccName, defaultValue, options));
            return new EditResult<Database.Entities.Character>(character);
        }
    }
}
