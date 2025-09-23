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
                "The target(s) in the current encounter (comma separated). Specify `vs` and the counter targeted.",
                isRequired: true)
            .AddOption("vs", ApplicationCommandOptionType.String, "The counter targeted.", isRequired: true);
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

        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
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
            VsNamePart = vsNamePart
        });
    }
}
