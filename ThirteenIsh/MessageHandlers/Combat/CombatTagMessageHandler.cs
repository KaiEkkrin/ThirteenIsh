using Discord;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatTagMessage))]
internal sealed class CombatTagMessageHandler(SqlDataService dataService, DiscordService discordService,
    IServiceProvider serviceProvider)
    : MessageHandlerBase<CombatTagMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        CombatTagMessage message, CancellationToken cancellationToken = default)
    {
        AddTagOperation editOperation = new(message.TagValue);

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
                        Title = $"Added tag '{message.TagValue}' to {output.CombatantResult.Combatant.Alias}"
                    });

                return await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    private sealed class AddTagOperation(string tagValue) : SyncEditOperation<AddTagResult, CombatantResult>
    {
        public override EditResult<AddTagResult> DoEdit(DataContext context, CombatantResult param)
        {
            var gameSystem = GameSystem.Get(param.Adventure.GameSystem);
            if (!param.Character.AddTag(tagValue))
                return CreateError($"Cannot add the tag '{tagValue}' to this combatant. Perhaps they already have it?");

            return new EditResult<AddTagResult>(new AddTagResult(param, gameSystem));
        }
    }

    private record AddTagResult(CombatantResult CombatantResult, GameSystem GameSystem);
}
