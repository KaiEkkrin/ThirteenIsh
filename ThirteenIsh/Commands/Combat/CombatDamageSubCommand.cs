﻿using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

// TODO Make an equivalent for dealing damage with a monster?
// (or move this stuff to just `13-combat` and have it work off of the current combatant,
// or a named one, without necessarily being bound to the PC?)
// TODO maybe change this to `13-combat damage`, have it use the current combatant (if it's
// added with your user ID) or the named combatant if an optional name is supplied (again if it's
// added with your user ID.)
internal sealed class CombatDamageSubCommand()
    : SubCommandBase("damage", "Deals damage to a player or monster in the encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("counter", ApplicationCommandOptionType.String, "A counter to add.")
            .AddOption("dice", ApplicationCommandOptionType.String, "The dice to roll.", isRequired: true)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("multiplier")
                .WithDescription("A multiplier to the counter")
                .WithType(ApplicationCommandOptionType.Integer)
                .AddChoice("1", 1)
                .AddChoice("2", 2)
                .AddChoice("3", 3)
                .AddChoice("4", 4)
                .AddChoice("5", 5)
                .AddChoice("6", 6))
            .AddOption("roll-separately", ApplicationCommandOptionType.Boolean, "Roll separately for each target")
            .AddOption("target", ApplicationCommandOptionType.String,
                "The target(s) in the current encounter (comma separated). Specify `vs` and the counter targeted.",
                isRequired: true)
            .AddOption("vs", ApplicationCommandOptionType.String, "The variable targeted.", isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;
        if (!CommandUtil.TryGetOption<string>(option, "dice", out var diceString))
        {
            await command.RespondAsync("No dice supplied.", ephemeral: true);
            return;
        }

        ParseTreeBase parseTree = Parser.Parse(diceString);
        if (!string.IsNullOrEmpty(parseTree.Error))
        {
            await command.RespondAsync(parseTree.Error);
            return;
        }

        var targets = CommandUtil.TryGetOption<string>(option, "target", out var targetString)
            ? targetString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : null;
        if (targets is not { Length: > 0 })
        {
            await command.RespondAsync("No target supplied.", ephemeral: true);
            return;
        }

        var vsNamePart = CommandUtil.TryGetOption<string>(option, "vs", out var vsString) ? vsString : null;
        if (string.IsNullOrWhiteSpace(vsNamePart))
        {
            await command.RespondAsync("No vs supplied.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);

        // TODO This code should be able to change to handle the current monster, too (?)
        // Should I be able to share this with PcCombatAttackCommand (generalise to making attacks with
        // monsters, too)?
        // For now, this only deals damage as the current player's character
        var encounterResult = await dataService.GetEncounterResultAsync(guild, channelId, cancellationToken);

        await encounterResult.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            async output =>
            {
                var (adventure, encounter) = output;
                var combatant = encounter.Combatants.FirstOrDefault(c =>
                    c.CharacterType == CharacterType.PlayerCharacter &&
                    c.UserId == command.User.Id);

                if (combatant == null ||
                    await dataService.GetCharacterAsync(combatant, cancellationToken) is not { } character)
                {
                    await command.RespondAsync("You have not joined this encounter.", ephemeral: true);
                    return;
                }

                var gameSystem = GameSystem.Get(adventure.GameSystem);
                var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
                if (!TryGetCounter(characterSystem, option, out var counter, out var message))
                {
                    await command.RespondAsync(message);
                    return;
                }

                // If there is a counter it must have a value
                int? counterValue = counter is null
                    ? null
                    : counter.GetValue(character.Sheet) is { } realCounterValue
                        ? realCounterValue
                        : throw new GamePropertyException(counter.Name);

                if (!CommandUtil.TryGetOption<int>(option, "multiplier", out var multiplier)) multiplier = 1;
                if (!CommandUtil.TryGetOption<bool>(option, "roll-separately", out var rollSeparately))
                    rollSeparately = false;

                List<CombatantBase> targetCombatants = [];
                if (!CommandUtil.TryFindCombatants(targets, encounter, targetCombatants, out message))
                {
                    await command.RespondAsync(message);
                    return;
                }

                var vsCounterByType = CommandUtil.FindCounterByType(gameSystem, vsNamePart,
                    c => c.Options.HasFlag(GameCounterOptions.HasVariable), targetCombatants);

                if (vsCounterByType.Count == 0)
                {
                    await command.RespondAsync($"'{vsNamePart}' does not uniquely match a variable property.",
                        ephemeral: true);
                    return;
                }

                // If we have a counter include that in the overall parse tree
                if (counter is not null && counterValue.HasValue)
                {
                    ParseTreeBase counterParseTree = multiplier == 1
                        ? new IntegerParseTree(0, counterValue.Value, counter.Name)
                        : new BinaryOperationParseTree(0,
                            new IntegerParseTree(0, counterValue.Value, counter.Name),
                            new IntegerParseTree(0, multiplier, "multiplier"),
                            '*');

                    parseTree = new BinaryOperationParseTree(0, parseTree, counterParseTree, '+');
                }

                // The next bit could take a while (sending messages to potentially many targets)
                // so we'll defer our response
                await command.DeferAsync();

                var random = serviceProvider.GetRequiredService<IRandomWrapper>();
                DamageRoller damageRoller = rollSeparately
                    ? new DamageRoller(parseTree, random)
                    : new CombinedDamageRoller(parseTree, random);

                var discordService = serviceProvider.GetRequiredService<DiscordService>();
                StringBuilder stringBuilder = new();
                for (var i = 0; i < targetCombatants.Count; ++i)
                {
                    if (i > 0) stringBuilder.AppendLine(); // space things out
                    var targetCharacter = await dataService.GetCharacterAsync(targetCombatants[i], cancellationToken);
                    if (targetCharacter is null) continue;
                    await RollDamageVsAsync(targetCombatants[i], targetCharacter);
                }

                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(command.User)
                    .WithTitle($"{adventure.Name} : Rolled damage to {vsCounterByType.Values.First().Name}")
                    .WithDescription(stringBuilder.ToString());

                await command.ModifyOriginalResponseAsync(properties => properties.Embed = embedBuilder.Build());
                return;

                async Task RollDamageVsAsync(CombatantBase target, ITrackedCharacter targetCharacter)
                {
                    stringBuilder.Append(CultureInfo.CurrentCulture, $"vs {target.Alias}");

                    var result = damageRoller.Roll();
                    stringBuilder.AppendLine(CultureInfo.CurrentCulture, $" : {result.Roll}");
                    stringBuilder.AppendLine(result.Working);

                    var vsCounter = vsCounterByType[target.CharacterType];
                    EncounterDamageMessage message = new()
                    {
                        Alias = target.Alias,
                        ChannelId = channelId,
                        CharacterType = targetCharacter.Type,
                        Damage = -result.Roll,
                        GuildId = guildId,
                        Name = adventure.Name,
                        UserId = targetCharacter.UserId,
                        VariableName = vsCounter.Name
                    };
                    await dataService.AddMessageAsync(message, cancellationToken);

                    var component = new ComponentBuilder()
                        .WithButton("Take full", message.GetMessageId(EncounterDamageMessage.TakeFullControlId))
                        .WithButton("Take half", message.GetMessageId(EncounterDamageMessage.TakeHalfControlId))
                        .WithButton("Take none", message.GetMessageId(EncounterDamageMessage.TakeNoneControlId));

                    var targetUser = await discordService.GetGuildUserAsync(guildId, targetCharacter.UserId);
                    await targetUser.SendMessageAsync(
                        $"{character.Name} dealt {result.Roll} damage to {target.Alias}'s {vsCounter.Name}",
                        components: component.Build());
                }
            });
    }

    private static bool TryGetCounter(CharacterSystem characterSystem, SocketSlashCommandDataOption option,
        out GameCounter? counter, [MaybeNullWhen(true)] out string errorMessage)
    {
        if (!CommandUtil.TryGetOption<string>(option, "counter", out var namePart))
        {
            // This is okay -- no counter specified
            counter = null;
            errorMessage = null;
            return true;
        }

        counter = characterSystem.FindCounter(namePart, c => c.Options.HasFlag(GameCounterOptions.CanRoll));
        if (counter is null)
        {
            errorMessage = $"'{namePart}' does not uniquely match a rollable property.";
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