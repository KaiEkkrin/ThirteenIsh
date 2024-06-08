using Discord;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Commands;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

internal abstract class CombatVSubMessageHandlerBase<TMessage>(SqlDataService dataService, DiscordService discordService,
    IRandomWrapper random, IServiceProvider serviceProvider)
    : MessageHandlerBase<TMessage> where TMessage : CombatVSubMessageBase
{
    protected IRandomWrapper Random => random;

    protected sealed override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        TMessage message, CancellationToken cancellationToken = default)
    {
        var editOperation = CreateEditOperation(message);
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
                var embed = CommandUtil.BuildTrackedCharacterSummaryEmbed(null, character, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        ExtraFields =
                        [
                            new EmbedFieldBuilder().WithName("Roll").WithValue(output.Working)
                        ],
                        OnlyTheseProperties = [output.GameCounter.Name],
                        Flags = CommandUtil.AdventurerSummaryFlags.OnlyVariables,
                        Title = $"Set {output.GameCounter.Name} on {combatant.Alias}"
                    });

                return await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    protected abstract CombatEditVariableOperation CreateEditOperation(TMessage message);
}
