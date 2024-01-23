using Discord.WebSocket;

namespace ThirteenIsh.Commands;

[ThirteenIshCommand("roll", "Makes basic dice rolls")]
internal sealed class RollCommand : IThirteenIshCommand
{
    public Task HandleAsync(SocketSlashCommand command)
    {
        return command.RespondAsync("TODO Roll command is not implemented yet");
    }
}
