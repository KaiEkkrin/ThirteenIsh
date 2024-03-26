using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
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

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        StringBuilder stringBuilder = new();
        for (var i = 0; i < targetCombatants.Count; ++i)
        {
            if (i > 0) stringBuilder.AppendLine(); // space things out
            RollVs(targetCombatants[i]);
        }

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(command.User)
            .WithTitle($"{adventurer.Name} : Rolled {counter.Name} vs {vsCounter.Name}")
            .WithDescription(stringBuilder.ToString());

        await command.RespondAsync(embed: embedBuilder.Build());
        return;

        void RollVs(CombatantBase target)
        {
            stringBuilder.Append(CultureInfo.CurrentCulture, $"vs {target.Name}");
            switch (target)
            {
                case AdventurerCombatant adventurerCombatant when adventure.Adventurers.TryGetValue(
                    adventurerCombatant.NativeUserId, out var targetAdventurer):
                    {
                        var dc = vsCounter.GetValue(targetAdventurer.Sheet);
                        if (!dc.HasValue)
                        {
                            stringBuilder.AppendLine(CultureInfo.CurrentCulture, $" : Target has no {vsCounter.Name}");
                            break;
                        }

                        var result = counter.Roll(adventurer, bonus, random, rerolls, ref dc);
                        stringBuilder.Append(CultureInfo.CurrentCulture, $" ({dc}) : {result.Roll}");
                        if (result.Success.HasValue)
                        {
                            var successString = result.Success.Value ? "Success!" : "Failure!";
                            stringBuilder.Append(" -- ").Append(successString);
                        }
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(result.Working);
                        break;
                    }

                // TODO add code for rolling vs monsters here

                default:
                    stringBuilder.AppendLine(" : Target unresolved");
                    break;
            }
        }
    }

    private static ParseTreeBase? GetBonus(SocketSlashCommandDataOption option)
    {
        if (!CommandUtil.TryGetOption<string>(option, "bonus", out var bonusString)) return null;
        return Parser.Parse(bonusString);
    }
}
