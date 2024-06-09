using Discord;
using Discord.WebSocket;
using ThirteenIsh.ChannelMessages.Gm;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmCombatSwitchCommand() : SubCommandBase("switch", "Sets the current combatant in an encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("alias", ApplicationCommandOptionType.String, "The alias of the combatant to switch to.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;
        if (!CommandUtil.TryGetOption<string>(option, "alias", out var alias))
        {
            await command.RespondAsync("No alias supplied.", ephemeral: true);
            return;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new GmCombatSwitchMessage
        {
            Alias = alias,
            ChannelId = channelId,
            GuildId = guildId,
            UserId = command.User.Id
        });
    }
}
