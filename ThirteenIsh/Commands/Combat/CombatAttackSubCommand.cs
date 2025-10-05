using Discord;
using Discord.WebSocket;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

// This is like `pc-roll` or `combat-roll`, but instead of rolling against a specified DC, here we roll against
// the attribute of another player (or monster) in the current encounter
internal sealed class CombatAttackSubCommand()
    : SubCommandBase("attack", "Rolls against a player or monster in the encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("counter", ApplicationCommandOptionType.String, "The counter name to roll.",
                isRequired: true)
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to roll for.")
            .AddOption("bonus", ApplicationCommandOptionType.String, "A bonus dice expression to add.")
            .AddRerollsOption("rerolls")
            .AddOption("target", ApplicationCommandOptionType.String,
                "The target(s) in the current encounter (comma separated). Specify `vs` and the counter targeted.")
            .AddOption("vs", ApplicationCommandOptionType.String, "The counter targeted.")
            .AddOption("second", ApplicationCommandOptionType.String, "The secondary property for this roll, e.g. for SWN skill checks.");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;
        if (!CommandUtil.TryGetOption<string>(option, "counter", out var namePart))
        {
            await command.RespondAsync("No name part supplied.", ephemeral: true);
            return;
        }

        var bonus = CommandUtil.GetBonus(option);
        if (!string.IsNullOrEmpty(bonus?.ParseError))
        {
            await command.RespondAsync(bonus.ParseError, ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "rerolls", out var rerolls)) rerolls = 0;

        var hasTarget = CommandUtil.TryGetOption<string>(option, "target", out var targetString);
        var hasVs = CommandUtil.TryGetOption<string>(option, "vs", out var vsString);

        // Validate target/vs pairing
        if (hasTarget != hasVs)
        {
            await command.RespondAsync("Both 'target' and 'vs' must be specified together, or neither should be specified.", ephemeral: true);
            return;
        }

        string[]? targets = null;
        string? vsNamePart = null;

        if (hasTarget && hasVs)
        {
            targets = targetString!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (targets.Length == 0)
            {
                await command.RespondAsync("No target supplied.", ephemeral: true);
                return;
            }

            vsNamePart = vsString!;
            if (string.IsNullOrWhiteSpace(vsNamePart))
            {
                await command.RespondAsync("No vs supplied.", ephemeral: true);
                return;
            }
        }

        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
            : null;

        var secondaryNamePart = CommandUtil.TryGetOption<string>(option, "second", out var secondString)
            ? secondString
            : null;

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new CombatAttackMessage
        {
            Alias = alias,
            Bonus = bonus,
            ChannelId = channelId,
            GuildId = guildId,
            NamePart = namePart,
            Rerolls = rerolls,
            Targets = targets,
            UserId = command.User.Id,
            VsNamePart = vsNamePart,
            SecondaryNamePart = secondaryNamePart
        });
    }
}
