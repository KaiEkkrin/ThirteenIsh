using Discord;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatUntagMessage))]
internal sealed class CombatUntagMessageHandler(SqlDataService dataService, DiscordService discordService,
    IServiceProvider serviceProvider)
    : MessageHandlerBase<CombatUntagMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        CombatUntagMessage message, CancellationToken cancellationToken = default)
    {
        RemoveTagOperation editOperation = new(message.TagValue);

        var result = await dataService.EditCombatantAsync(
            message.GuildId, message.ChannelId, message.AsGm ? null : message.UserId, editOperation, message.Alias,
            cancellationToken);

        await result.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            async output =>
            {
                var (adventure, encounter, combatant, character) = output.CombatantResult;
                var channel = await discordService.GetGuildMessageChannelAsync(message.GuildId, message.ChannelId)
                    ?? throw new InvalidOperationException($"No channel for guild {message.GuildId}, channel {message.ChannelId}");

                // Update the encounter table
                var encounterTable = await CommandUtil.UpdateEncounterMessageAsync(serviceProvider, message.GuildId,
                    channel, encounter, output.GameSystem, cancellationToken);

                // If this wasn't a simple integer, show the working
                var embed = CommandUtil.BuildTrackedCharacterSummaryEmbed(interaction, output.CombatantResult.Character,
                    output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        OnlyTheseProperties = [],
                        Flags = CommandUtil.AdventurerSummaryFlags.OnlyVariables | CommandUtil.AdventurerSummaryFlags.WithTags,
                        Title = $"Removed tag '{message.TagValue}' from {output.CombatantResult.Combatant.Alias}"
                    });

                await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    private sealed class RemoveTagOperation(string tagValue) : SyncEditOperation<RemoveTagResult, CombatantResult>
    {
        public override EditResult<RemoveTagResult> DoEdit(DataContext context, CombatantResult param)
        {
            var gameSystem = GameSystem.Get(param.Adventure.GameSystem);
            if (!param.Character.RemoveTag(tagValue))
                return CreateError($"Cannot remove the tag '{tagValue}' from this combatant. Perhaps they do not have it?");

            return new EditResult<RemoveTagResult>(new RemoveTagResult(param, gameSystem));
        }
    }

    private record RemoveTagResult(CombatantResult CombatantResult, GameSystem GameSystem);
}
