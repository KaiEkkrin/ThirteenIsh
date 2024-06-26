﻿using Discord;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Pcs;

[MessageHandler(MessageType = typeof(PcFixMessage))]
internal sealed class PcFixMessageHandler(SqlDataService dataService) : MessageHandlerBase<PcFixMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        PcFixMessage message, CancellationToken cancellationToken = default)
    {
        FixOperation editOperation = new(message.CounterNamePart, message.FixValue);

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
                        OnlyTheseProperties = [output.Counter.Name],
                        Title = $"'{output.Adventurer.Name}' : Fixed value of {output.Counter.Name} by {message.FixValue}"
                    });

                return interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });

        return true;
    }

    private sealed class FixOperation(string counterNamePart, int fixValue) : SyncEditOperation<FixResult, Adventurer>
    {
        public override EditResult<FixResult> DoEdit(DataContext context, Adventurer adventurer)
        {
            var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
            var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);

            // Important -- don't allow fixing hidden counters! Then you wouldn't be able to tell they
            // had been fixed (no display), which would be super confusing
            var counter = characterSystem.FindCounter(adventurer.Sheet, counterNamePart,
                c => !c.Options.HasFlag(GameCounterOptions.IsHidden));

            if (counter is null)
                return CreateError($"'{counterNamePart}' does not uniquely match a counter name.");

            counter.SetFixValue(fixValue, adventurer);
            characterSystem.ScrubCustomCounters(adventurer);
            return new EditResult<FixResult>(new FixResult(adventurer, counter, gameSystem));
        }
    }

    private record FixResult(Adventurer Adventurer, GameCounter Counter, GameSystem GameSystem);
}
