using Discord.WebSocket;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcEncounterJoinSubCommand() : SubCommandBase("join", "Joins the current encounter.")
{
    public override Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
