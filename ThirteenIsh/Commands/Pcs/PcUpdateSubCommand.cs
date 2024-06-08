using Discord.WebSocket;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcUpdateSubCommand() : SubCommandBase("update", "Syncs the base character sheet with an adventurer.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new PcUpdateMessage
        {
            GuildId = guildId,
            UserId = command.User.Id
        });
    }
}
