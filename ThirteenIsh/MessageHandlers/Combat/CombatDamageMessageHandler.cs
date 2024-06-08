using Discord;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Commands;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatDamageMessage))]
internal sealed class CombatDamageMessageHandler(SqlDataService dataService, DiscordService discordService,
    IRandomWrapper random) : MessageHandlerBase<CombatDamageMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        CombatDamageMessage message, CancellationToken cancellationToken = default)
    {
        var guild = await dataService.GetGuildAsync(message.GuildId, cancellationToken);
        var combatantResult = await dataService.GetCombatantResultAsync(guild, message.ChannelId, message.UserId,
            message.Alias, cancellationToken);

        await combatantResult.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            async output =>
            {
                var (adventure, encounter, combatant, character) = output;

                var gameSystem = GameSystem.Get(adventure.GameSystem);
                var characterSystem = gameSystem.GetCharacterSystem(combatant.CharacterType);

                if (!TryGetCounter(characterSystem, message.CounterNamePart, character.Sheet, out var counter,
                    out var errorMessage))
                {
                    await interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage);
                    return;
                }

                // If there is a counter it must have a value
                int? counterValue = counter is null
                    ? null
                    : counter.GetValue(character.Sheet) is { } realCounterValue
                        ? realCounterValue
                        : throw new GamePropertyException(counter.Name);

                List<CombatantBase> targetCombatants = [];
                if (!CommandUtil.TryFindCombatants(message.Targets, encounter, targetCombatants, out errorMessage))
                {
                    await interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage);
                    return;
                }

                // If we have a counter include that in the overall parse tree
                var parseTree = message.DiceParseTree;
                if (counter is not null && counterValue.HasValue)
                {
                    ParseTreeBase counterParseTree = message.Multiplier == 1
                        ? new IntegerParseTree(0, counterValue.Value, counter.Name)
                        : new BinaryOperationParseTree(0,
                            new IntegerParseTree(0, counterValue.Value, counter.Name),
                            new IntegerParseTree(0, message.Multiplier, "multiplier"),
                            '*');

                    parseTree = new BinaryOperationParseTree(0, parseTree, counterParseTree, '+');
                }

                DamageRoller damageRoller = message.RollSeparately
                    ? new DamageRoller(parseTree, random)
                    : new CombinedDamageRoller(parseTree, random);

                StringBuilder stringBuilder = new();
                SortedSet<string> vsCounterNames = []; // hopefully only one :P
                for (var i = 0; i < targetCombatants.Count; ++i)
                {
                    if (i > 0) stringBuilder.AppendLine(); // space things out
                    var targetCharacter = await dataService.GetCharacterAsync(targetCombatants[i], encounter, cancellationToken);
                    if (targetCharacter is null) continue;
                    await RollDamageVsAsync(targetCombatants[i], targetCharacter);
                }

                var vsCounterNameSummary = vsCounterNames.Count == 0
                    ? $"'{message.VsNamePart}'"
                    : string.Join(", ", vsCounterNames);

                var user = await discordService.GetGuildUserAsync(message.GuildId, message.UserId);
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(user)
                    .WithTitle($"{combatant.Alias} : Rolled damage to {vsCounterNameSummary}")
                    .WithDescription(stringBuilder.ToString());

                await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embedBuilder.Build());
                return;

                async Task RollDamageVsAsync(CombatantBase target, ITrackedCharacter targetCharacter)
                {
                    stringBuilder.Append(CultureInfo.CurrentCulture, $"vs {target.Alias}");

                    var result = damageRoller.Roll();
                    stringBuilder.AppendLine(CultureInfo.CurrentCulture, $" : {result.Roll}");
                    stringBuilder.AppendLine(result.Working);

                    var vsCharacterSystem = gameSystem.GetCharacterSystem(targetCharacter.Type);
                    var vsCounter = vsCharacterSystem.FindCounter(targetCharacter.Sheet, message.VsNamePart,
                        c => c.Options.HasFlag(GameCounterOptions.HasVariable));

                    if (vsCounter is null)
                    {
                        stringBuilder.AppendLine(CultureInfo.CurrentCulture,
                            $" : Target has no variable counter unambiguously matching '{message.VsNamePart}'");
                        return;
                    }

                    vsCounterNames.Add(vsCounter.Name);
                    EncounterDamageMessage promptMessage = new()
                    {
                        Alias = target.Alias,
                        ChannelId = message.ChannelId,
                        CharacterType = targetCharacter.Type,
                        Damage = result.Roll,
                        GuildId = message.GuildId,
                        Name = adventure.Name,
                        UserId = targetCharacter.UserId,
                        VariableName = vsCounter.Name
                    };
                    await dataService.AddMessageAsync(promptMessage, cancellationToken);

                    var component = new ComponentBuilder()
                        .WithButton("Take full", promptMessage.GetMessageId(EncounterDamageMessage.TakeFullControlId))
                        .WithButton("Take half", promptMessage.GetMessageId(EncounterDamageMessage.TakeHalfControlId))
                        .WithButton("Take none", promptMessage.GetMessageId(EncounterDamageMessage.TakeNoneControlId))
                        .WithButton("Take double", promptMessage.GetMessageId(EncounterDamageMessage.TakeDoubleControlId));

                    var targetUser = await discordService.GetGuildUserAsync(message.GuildId, targetCharacter.UserId);
                    await targetUser.SendMessageAsync(
                        $"{character.Name} dealt {result.Roll} damage to {target.Alias}'s {vsCounter.Name}",
                        components: component.Build());
                }
            });

        return true;
    }

    private static bool TryGetCounter(CharacterSystem characterSystem, string? counterNamePart,
        CharacterSheet sheet, out GameCounter? counter, [MaybeNullWhen(true)] out string errorMessage)
    {
        if (counterNamePart is null)
        {
            // This is okay -- no counter specified
            counter = null;
            errorMessage = null;
            return true;
        }

        counter = characterSystem.FindCounter(sheet, counterNamePart, c => c.Options.HasFlag(GameCounterOptions.CanRoll));
        if (counter is null)
        {
            errorMessage = $"'{counterNamePart}' does not uniquely match a rollable property.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    // Rolls separately each time
    private class DamageRoller(ParseTreeBase parseTree, IRandomWrapper random)
    {
        public virtual GameCounterRollResult Roll()
        {
            var rolledValue = parseTree.Evaluate(random, out var working);
            return new GameCounterRollResult
            {
                Roll = rolledValue,
                Working = working
            };
        }
    }

    // Rolls once and retains the result
    private class CombinedDamageRoller(ParseTreeBase parseTree, IRandomWrapper random)
        : DamageRoller(parseTree, random)
    {
        private GameCounterRollResult? _result;

        public override GameCounterRollResult Roll()
        {
            _result ??= base.Roll();
            return _result.Value;
        }
    }
}
