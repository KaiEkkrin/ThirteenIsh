using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

// This is like `pc-roll`, but instead of rolling against a specified DC, here we roll against
// the attribute of another player (or monster) in the current encounter
internal class PcEncounterAttackCommand() : SubCommandBase("attack", "Rolls against a player or monster in the encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The property name to roll.",
                isRequired: true)
            .AddOption("bonus", ApplicationCommandOptionType.String, "A bonus dice expression to add.")
            .AddRerollsOption("rerolls")
            .AddOption("target", ApplicationCommandOptionType.String,
                "The target(s) in the current encounter (comma separated). Specify `vs` and the counter targeted.",
                isRequired: true)
            .AddOption("vs", ApplicationCommandOptionType.String, "If `target` is specified, the counter targeted.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;
        if (!CommandUtil.TryGetOption<string>(option, "name", out var namePart))
        {
            await command.RespondAsync("No name part supplied.", ephemeral: true);
            return;
        }

        var bonus = GetBonus(option);
        if (!string.IsNullOrEmpty(bonus?.Error))
        {
            await command.RespondAsync(bonus.Error, ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "rerolls", out var rerolls)) rerolls = 0;

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

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (!CommandUtil.TryGetCurrentCombatant(guild, channelId, command.User.Id, out var adventure,
            out var adventurer, out var encounter, out var errorMessage))
        {
            await command.RespondAsync(errorMessage);
            return;
        }

        var gameSystem = GameSystem.Get(adventure.GameSystem);
        var counter = gameSystem.FindCounter(namePart, c => c.Options.HasFlag(GameCounterOptions.CanRoll));
        if (counter is null)
        {
            await command.RespondAsync($"'{namePart}' does not uniquely match a rollable property.",
                ephemeral: true);
            return;
        }

        List<CombatantBase> targetCombatants = [];
        if (!CommandUtil.TryFindCombatantsByName(targets, encounter, targetCombatants, out errorMessage))
        {
            await command.RespondAsync(errorMessage);
            return;
        }

        var vsCounter = gameSystem.FindCounter(vsNamePart, _ => true);
        if (vsCounter is null)
        {
            await command.RespondAsync($"'{vsNamePart}' does not uniquely match a counter property.", ephemeral: true);
            return;
        }

        // TODO
        throw new NotImplementedException();
    }

    private static ParseTreeBase? GetBonus(SocketSlashCommandDataOption option)
    {
        if (!CommandUtil.TryGetOption<string>(option, "bonus", out var bonusString)) return null;
        return Parser.Parse(bonusString);
    }
}
