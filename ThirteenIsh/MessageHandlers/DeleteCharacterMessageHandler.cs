using Discord;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(DeleteCharacterMessage))]
internal sealed class DeleteCharacterMessageHandler(SqlDataService dataService) : MessageHandlerBase<DeleteCharacterMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        DeleteCharacterMessage message, CancellationToken cancellationToken = default)
    {
        var character = await dataService.DeleteCharacterAsync(message.Name, message.UserId, message.CharacterType,
            cancellationToken);
        if (character == null)
        {
            await interaction.RespondAsync(
                $"Cannot delete a {message.CharacterType.FriendlyName()} named '{message.Name}'. Perhaps they were already deleted, or there is more than one character or monster matching that name.",
                ephemeral: true);
            return true;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(interaction.User);
        embedBuilder.WithTitle($"Deleted {message.CharacterType.FriendlyName()} '{character.Name}'");

        await interaction.RespondAsync(embed: embedBuilder.Build());
        return true;
    }
}
