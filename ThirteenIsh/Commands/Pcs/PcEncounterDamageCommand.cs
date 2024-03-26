using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcEncounterDamageCommand()
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
                isRequired: true);
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

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (!CommandUtil.TryGetCurrentCombatant(guild, channelId, command.User.Id, out var adventure,
            out var adventurer, out var encounter, out var errorMessage))
        {
            await command.RespondAsync(errorMessage);
            return;
        }

        var gameSystem = GameSystem.Get(adventure.GameSystem);
        if (!TryGetCounter(gameSystem, option, out var counter, out errorMessage))
        {
            await command.RespondAsync(errorMessage);
            return;
        }

        // If there is a counter it must have a value
        int? counterValue = counter is null
            ? null
            : counter.GetValue(adventurer.Sheet) is { } realCounterValue
                ? realCounterValue
                : throw new GamePropertyException(counter.Name);

        if (!CommandUtil.TryGetOption<int>(option, "multiplier", out var multiplier)) multiplier = 1;
        if (!CommandUtil.TryGetOption<bool>(option, "roll-separately", out var rollSeparately)) rollSeparately = false;

        List<CombatantBase> targetCombatants = [];
        if (!CommandUtil.TryFindCombatantsByName(targets, encounter, targetCombatants, out errorMessage))
        {
            await command.RespondAsync(errorMessage);
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

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        DamageRoller damageRoller = rollSeparately
            ? new DamageRoller(parseTree, random)
            : new CombinedDamageRoller(parseTree, random);

        // TODO : Spawn one message per target, to go to them to ask if they accept the damage
        StringBuilder stringBuilder = new();
        for (var i = 0; i < targetCombatants.Count; ++i)
        {
            if (i > 0) stringBuilder.AppendLine(); // space things out
            RollDamageVs(targetCombatants[i]);
        }

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(command.User)
            .WithTitle($"{adventure.Name} : Rolled damage")
            .WithDescription(stringBuilder.ToString());

        await command.RespondAsync(embed: embedBuilder.Build());
        return;

        void RollDamageVs(CombatantBase target)
        {
            stringBuilder.Append(CultureInfo.CurrentCulture, $"vs {target.Name}");
            // TODO should this switch turn into a virtual method on CombatantBase?
            switch (target)
            {
                case AdventurerCombatant adventurerCombatant when adventure.Adventurers.TryGetValue(
                    adventurerCombatant.NativeUserId, out var targetAdventurer):
                    {
                        var result = damageRoller.Roll();
                        stringBuilder.AppendLine(CultureInfo.CurrentCulture, $" : {result.Roll}");
                        stringBuilder.AppendLine(result.Working);

                        // TODO Append the message to go to the target's player to acknowledge the damage
                        break;
                    }

                // TODO add code for rolling vs monsters here

                default:
                    stringBuilder.AppendLine(" : Target unresolved");
                    break;
            }
        }
    }

    private static bool TryGetCounter(GameSystem gameSystem, SocketSlashCommandDataOption option,
        out GameCounter? counter, [MaybeNullWhen(true)] out string errorMessage)
    {
        if (!CommandUtil.TryGetOption<string>(option, "counter", out var namePart))
        {
            // This is okay -- no counter specified
            counter = null;
            errorMessage = null;
            return true;
        }

        counter = gameSystem.FindCounter(namePart, c => c.Options.HasFlag(GameCounterOptions.CanRoll));
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
