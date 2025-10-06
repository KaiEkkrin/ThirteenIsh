using Discord;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Pcs;

[MessageHandler(MessageType = typeof(PcTagMessage))]
internal sealed class PcTagMessageHandler(SqlDataService dataService) : MessageHandlerBase<PcTagMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        PcTagMessage message, CancellationToken cancellationToken = default)
    {
        AddTagOperation editOperation = new(message.TagValue);

        // If AsGm is true, pass null for userId to bypass permission check; otherwise enforce userId check
        var result = message.AsGm && message.Name != null
            ? await dataService.EditAdventurerAsync(message.GuildId, null, editOperation, message.Name, cancellationToken)
            : await dataService.EditAdventurerAsync(message.GuildId, message.UserId, editOperation, message.Name, cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            output =>
            {
                var embed = CommandUtil.BuildTrackedCharacterSummaryEmbed(interaction, output.Adventurer, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        OnlyTheseProperties = [],
                        Flags = CommandUtil.AdventurerSummaryFlags.OnlyVariables | CommandUtil.AdventurerSummaryFlags.WithTags,
                        Title = $"Added tag '{message.TagValue}' to {output.Adventurer.Name}"
                    });

                return interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    private sealed class AddTagOperation(string tagValue) : SyncEditOperation<AddTagResult, Adventurer>
    {
        public override EditResult<AddTagResult> DoEdit(DataContext context, Adventurer adventurer)
        {
            var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
            if (!adventurer.AddTag(tagValue))
                return CreateError($"Cannot add the tag '{tagValue}' to this adventurer. Perhaps they already have it?");

            return new EditResult<AddTagResult>(new AddTagResult(adventurer, gameSystem));
        }
    }

    private record AddTagResult(Adventurer Adventurer, GameSystem GameSystem);
}
