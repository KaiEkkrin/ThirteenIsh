using Discord;
using Discord.WebSocket;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatFixSubCommand(bool asGm) : SubCommandBase("fix", "Fixes a counter value for a combatant.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to edit.",
                isRequired: asGm)
            .AddOption("counter-name", ApplicationCommandOptionType.String, "The counter name to fix.")
            .AddOption("value", ApplicationCommandOptionType.Integer, "The fix value.");
    }

    public override async Task HandleAsync(
        SocketSlashCommand command,
        SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
            : null;

        if (!CommandUtil.TryGetOption<string>(option, "counter-name", out var counterNamePart))
        {
            await command.RespondAsync("No counter name part supplied.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "value", out var fixValue))
        {
            await command.RespondAsync("No value supplied.", ephemeral: true);
            return;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new CombatFixMessage
        {
            GuildId = guildId,
            ChannelId = channelId,
            AsGm = asGm,
            Alias = alias,
            CounterNamePart = counterNamePart,
            FixValue = fixValue,
            UserId = command.User.Id
        });
    }
}
