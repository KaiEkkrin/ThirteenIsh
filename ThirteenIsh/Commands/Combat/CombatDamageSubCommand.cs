using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

// TODO make this one accept an alias, like `combat-attack` does, as the attacker, and work off of that.
// Perhaps make the dice optional, because in 13th Age monster attacks are usually a fixed value?
internal sealed class CombatDamageSubCommand()
    : SubCommandBase("damage", "Deals damage to a player or monster in the encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("counter", ApplicationCommandOptionType.String, "An optional counter to add.")
            .AddOption("dice", ApplicationCommandOptionType.String, "The dice to roll.", isRequired: true)
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to roll for.")
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
            .AddOption("vs", ApplicationCommandOptionType.String, "The variable targeted.");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var counterNamePart = CommandUtil.TryGetOption<string>(option, "counter", out var counterNamePartValue)
            ? counterNamePartValue
            : null;

        if (!CommandUtil.TryGetOption<string>(option, "dice", out var diceString))
        {
            await command.RespondAsync("No dice supplied.", ephemeral: true);
            return;
        }

        ParseTreeBase parseTree = Parser.Parse(diceString);
        if (!string.IsNullOrEmpty(parseTree.ParseError))
        {
            await command.RespondAsync(parseTree.ParseError, ephemeral: true);
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
        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
            : null;

        var multiplier = CommandUtil.TryGetOption<int>(option, "multiplier", out var multiplierValue)
            ? multiplierValue
            : 1;

        var rollSeparately = CommandUtil.TryGetOption<bool>(option, "roll-separately", out var rollSeparatelyValue)
            && rollSeparatelyValue;

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new CombatDamageMessage
        {
            Alias = alias,
            ChannelId = channelId,
            CounterNamePart = counterNamePart,
            DiceParseTree = parseTree,
            GuildId = guildId,
            Multiplier = multiplier,
            RollSeparately = rollSeparately,
            Targets = targets,
            UserId = command.User.Id,
            VsNamePart = vsNamePart
        });
    }

    private static bool TryGetCounter(CharacterSystem characterSystem, IApplicationCommandInteractionDataOption option,
        CharacterSheet sheet, out GameCounter? counter, [MaybeNullWhen(true)] out string errorMessage)
    {
        if (!CommandUtil.TryGetOption<string>(option, "counter", out var namePart))
        {
            // This is okay -- no counter specified
            counter = null;
            errorMessage = null;
            return true;
        }

        counter = characterSystem.FindCounter(sheet, namePart, c => c.Options.HasFlag(GameCounterOptions.CanRoll));
        if (counter is null)
        {
            errorMessage = $"'{namePart}' does not uniquely match a rollable property.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
