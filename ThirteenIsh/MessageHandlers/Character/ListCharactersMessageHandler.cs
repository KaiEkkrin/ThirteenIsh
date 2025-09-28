using Discord;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Character;

[MessageHandler(MessageType = typeof(ListCharactersMessage))]
internal sealed class ListCharactersMessageHandler(SqlDataService dataService) : MessageHandlerBase<ListCharactersMessage>
{
    protected override async Task<bool> HandleInternalAsync(
        IDiscordInteraction interaction,
        string controlId,
        ListCharactersMessage message,
        CancellationToken cancellationToken = default)
    {
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(interaction.User);
        embedBuilder.WithTitle(
            message.CharacterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter | FriendlyNameOptions.Plural));

        if (controlId == ListCharactersMessage.DoneControlId)
        {
            // Do nothing more.
            embedBuilder.WithDescription("Done listing characters.");
            await RespondOrModifyAsync(interaction, message, embed: embedBuilder.Build());
            return true;
        }

        var list = await dataService.GetCharactersPageAsync(message.UserId, message.CharacterType, message.Name, message.After,
            message.PageSize, cancellationToken);

        if (list.Count == 0)
        {
            embedBuilder.WithDescription(message.After ? "No more characters" : "No characters found");
        }
        else
        {
            foreach (var character in list)
            {
                var gameSystem = GameSystem.Get(character.GameSystem);
                var summary = gameSystem.GetCharacterSummary(character);

                embedBuilder.AddField(new EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName(character.Name)
                    .WithValue($"[{gameSystem.Name}] {summary}"));
            }
        }

        if (list.Count < message.PageSize)
        {
            await RespondOrModifyAsync(interaction, message, embed: embedBuilder.Build());
            return true;
        }

        // If we got here, we need to send a new message for the next page, including the "more" and "done" buttons:
        ListCharactersMessage newMessage = new()
        {
            After = true,
            CharacterType = message.CharacterType,
            Name = list[^1].Name,
            PageSize = message.PageSize,
            UserId = message.UserId
        };

        await dataService.AddMessageAsync(newMessage, cancellationToken);

        ComponentBuilder componentBuilder = new();
        componentBuilder.WithButton("Done", newMessage.GetMessageId(ListCharactersMessage.DoneControlId))
            .WithButton("More", newMessage.GetMessageId(ListCharactersMessage.MoreControlId));

        await RespondOrModifyAsync(interaction, message, embed: embedBuilder.Build(), components: componentBuilder.Build());
        return true;
    }
}
