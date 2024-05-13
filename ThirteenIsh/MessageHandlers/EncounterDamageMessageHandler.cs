using Discord;
using Discord.WebSocket;
using ThirteenIsh.Commands;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(EncounterDamageMessage))]
internal sealed class EncounterDamageMessageHandler(SqlDataService dataService, IRandomWrapper random)
    : MessageHandlerBase<EncounterDamageMessage>
{
    protected override async Task<bool> HandleInternalAsync(SocketMessageComponent component, string controlId,
        EncounterDamageMessage message, CancellationToken cancellationToken = default)
    {
        var guild = await dataService.GetGuildAsync(message.GuildId, cancellationToken);
        var encounter = await dataService.GetEncounterAsync(guild, message.ChannelId, cancellationToken);
        if (encounter == null)
        {
            await component.RespondAsync("There is no encounter in progress in the designated channel.", ephemeral: true);
            return true;
        }

        var combatant = encounter.Combatants.SingleOrDefault(c => c.Alias == message.Alias);
        if (combatant == null)
        {
            await component.RespondAsync($"There is no combatant '{message.Alias}' in the current encounter.",
                ephemeral: true);
            return true;
        }

        var adventure = await dataService.GetAdventureAsync(guild, encounter.AdventureName, cancellationToken);
        if (adventure == null || adventure.Name != guild.CurrentAdventureName)
        {
            await component.RespondAsync("The current encounter does not match the current adventure.", ephemeral: true);
            return true;
        }

        var result = await dataService.EditAsync(
            new EditOperation(random), new EditParam(adventure, combatant, message, controlId), cancellationToken);

        return await result.HandleAsync(
            async errorMessage =>
            {
                await component.RespondAsync(errorMessage, ephemeral: true);
                return true;
            },
            async output =>
            {
                // TODO :
                // - Write a summary of what happened to the channel.
                // - Update the pinned combat message.
                await CommandUtil.RespondWithTrackedCharacterSummaryAsync(component, output.Character, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        ExtraFields =
                        [
                            new EmbedFieldBuilder().WithName("Damage").WithValue(output.Working)
                        ],
                        OnlyTheseProperties = [output.Counter.Name],
                        OnlyVariables = true,
                        Title = $"{combatant.Alias} took damage to {output.Counter.Name}"
                    });

                return true;
            });
    }

    private static ParseTreeBase BuildDamageParseTree(string controlId, EncounterDamageMessage message)
    {
        switch (controlId)
        {
            case EncounterDamageMessage.TakeHalfControlId:
                return new BinaryOperationParseTree(0,
                    new IntegerParseTree(0, message.Damage),
                    new IntegerParseTree(0, 2),
                    '/');

            case EncounterDamageMessage.TakeNoneControlId:
                return new BinaryOperationParseTree(0,
                    new IntegerParseTree(0, message.Damage),
                    new IntegerParseTree(0, 0),
                    '*');

            default:
                return new IntegerParseTree(0, message.Damage);
        }
    }

    private sealed class EditOperation(IRandomWrapper random) : EditOperation<DamageResult, EditParam>
    {
        public override async Task<EditResult<DamageResult>> DoEditAsync(DataContext context, EditParam param,
            CancellationToken cancellationToken)
        {
            var (adventure, combatant, message, controlId) = param;

            var character = await param.Combatant.GetCharacterAsync(context, cancellationToken);
            if (character is null) return CreateError($"No character sheet found for combatant '{combatant.Alias}.");

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            var characterSystem = gameSystem.GetCharacterSystem(combatant.CharacterType);
            var counter = characterSystem.FindCounter(message.VariableName,
                c => c.Options.HasFlag(GameCounterOptions.HasVariable));

            if (counter == null)
                return CreateError($"'{message.VariableName}' does not uniquely match a variable name.");

            // Illustrating this as a parse tree should make it clearer what has happened
            var currentValue = counter.GetVariableValue(character)
                ?? counter.GetStartingValue(character.Sheet)
                ?? throw new InvalidOperationException($"Variable {counter.Name} has no current or starting value");

            var parseTree = new BinaryOperationParseTree(0,
                new IntegerParseTree(0, currentValue),
                BuildDamageParseTree(controlId, message),
                '+');

            var newValue = parseTree.Evaluate(random, out var working);
            counter.SetVariableClamped(newValue, character);
            return new EditResult<DamageResult>(new DamageResult(character, counter, gameSystem, working));
        }
    }

    private record EditParam(Adventure Adventure, CombatantBase Combatant, EncounterDamageMessage Message, string ControlId);

    private record DamageResult(ITrackedCharacter Character, GameCounter Counter, GameSystem GameSystem, string Working);
}
