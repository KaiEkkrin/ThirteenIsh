using Discord;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatFixMessage))]
internal sealed class CombatFixMessageHandler(SqlDataService dataService, DiscordService discordService,
    IServiceProvider serviceProvider)
    : MessageHandlerBase<CombatFixMessage>
{
    protected override async Task<bool> HandleInternalAsync(
        IDiscordInteraction interaction,
        string controlId,
        CombatFixMessage message,
        CancellationToken cancellationToken = default)
    {
        FixOperation editOperation = new(message.CounterNamePart, message.FixValue);

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

                var embed = CommandUtil.BuildTrackedCharacterSummaryEmbed(interaction, output.CombatantResult.Character, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        OnlyTheseProperties = [output.Counter.Name],
                        Title = $"'{output.CombatantResult.Combatant.Alias}' : Fixed value of {output.Counter.Name} by {message.FixValue}"
                    });

                return await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    private sealed class FixOperation(string counterNamePart, int fixValue) : SyncEditOperation<FixResult, CombatantResult>
    {
        public override EditResult<FixResult> DoEdit(DataContext context, CombatantResult param)
        {
            var gameSystem = GameSystem.Get(param.Adventure.GameSystem);
            var characterSystem = gameSystem.GetCharacterSystem(param.Combatant.CharacterType, param.Character.CharacterSystemName);

            // Important -- don't allow fixing hidden counters! Then you wouldn't be able to tell they
            // had been fixed (no display), which would be super confusing
            var counter = characterSystem.FindCounter(param.Character.Sheet, counterNamePart,
                c => !c.Options.HasFlag(GameCounterOptions.IsHidden));

            if (counter is null)
                return CreateError($"'{counterNamePart}' does not uniquely match a counter name.");

            counter.SetFixValue(fixValue, param.Character);
            characterSystem.ScrubCustomCounters(param.Character);

            return new EditResult<FixResult>(new FixResult(param, counter, gameSystem));
        }
    }

    private record FixResult(CombatantResult CombatantResult, GameCounter Counter, GameSystem GameSystem);
}
