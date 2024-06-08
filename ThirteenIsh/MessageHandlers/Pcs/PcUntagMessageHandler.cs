using Discord;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Pcs;

[MessageHandler(MessageType = typeof(PcUntagMessage))]
internal sealed class PcUntagMessageHandler(SqlDataService dataService) : MessageHandlerBase<PcUntagMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        PcUntagMessage message, CancellationToken cancellationToken = default)
    {
        RemoveTagOperation editOperation = new(message.TagValue);

        var result = message.Name != null
            ? await dataService.EditAdventurerAsync(message.GuildId, message.Name, editOperation, cancellationToken)
            : await dataService.EditAdventurerAsync(message.GuildId, message.UserId, editOperation, cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            output =>
            {
                var embed = CommandUtil.BuildTrackedCharacterSummaryEmbed(interaction, output.Adventurer, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        OnlyTheseProperties = [],
                        Flags = CommandUtil.AdventurerSummaryFlags.OnlyVariables | CommandUtil.AdventurerSummaryFlags.WithTags,
                        Title = $"Removed tag '{message.TagValue}' from {output.Adventurer.Name}"
                    });

                return interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    private sealed class RemoveTagOperation(string tagValue) : SyncEditOperation<RemoveTagResult, Adventurer>
    {
        public override EditResult<RemoveTagResult> DoEdit(DataContext context, Adventurer adventurer)
        {
            var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
            if (!adventurer.RemoveTag(tagValue))
                return CreateError($"Cannot remove the tag '{tagValue}' from this adventurer. Perhaps they do not have it?");

            return new EditResult<RemoveTagResult>(new RemoveTagResult(adventurer, gameSystem));
        }
    }

    private record RemoveTagResult(Adventurer Adventurer, GameSystem GameSystem);
}
