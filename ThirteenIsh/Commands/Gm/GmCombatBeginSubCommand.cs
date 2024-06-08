using Discord.WebSocket;
using ThirteenIsh.ChannelMessages.Gm;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmCombatBeginSubCommand() : SubCommandBase("begin", "Begins an encounter in this channel.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new GmCombatBeginMessage
        {
            ChannelId = channelId,
            GuildId = guildId,
            UserId = command.User.Id
        });
    }
}
