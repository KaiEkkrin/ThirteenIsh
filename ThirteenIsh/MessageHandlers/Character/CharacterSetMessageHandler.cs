using Discord;
using ThirteenIsh.ChannelMessages.Character;
using ThirteenIsh.Commands;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Character;

[MessageHandler(MessageType = typeof(CharacterSetMessage))]
internal sealed class CharacterSetMessageHandler(SqlDataService dataService) : MessageHandlerBase<CharacterSetMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        CharacterSetMessage message, CancellationToken cancellationToken = default)
    {
        var character = await dataService.GetCharacterAsync(message.Name, message.UserId, message.CharacterType,
            cancellationToken: cancellationToken);
        if (character is null)
        {
            await interaction.ModifyOriginalResponseAsync(properties => properties.Content =
                $"Error getting {message.CharacterType.FriendlyName()} '{message.Name}'. Perhaps they do not exist, or there is more than one character or monster matching that name?");

            return true;
        }

        var gameSystem = GameSystem.Get(character.GameSystem);
        var characterSystem = gameSystem.GetCharacterSystem(message.CharacterType);
        var property = characterSystem.FindStorableProperty(character.Sheet, message.PropertyName);
        if (property is null)
        {
            await interaction.ModifyOriginalResponseAsync(properties => properties.Content =
                $"'{message.PropertyName}' does not uniquely match a settable property name.");

            return true;
        }

        var result = await dataService.EditCharacterAsync(
            message.Name, new SetCharacterPropertyOperation(property, message.NewValue), message.UserId, message.CharacterType,
            cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            updatedCharacter =>
            {
                var embed = CommandUtil.BuildCharacterSheetEmbed(interaction, updatedCharacter,
                    $"Edited {message.CharacterType.FriendlyName()} '{updatedCharacter.Name}'", [property.Name]);

                return interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }
}
