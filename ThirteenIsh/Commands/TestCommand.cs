using Discord.WebSocket;

namespace ThirteenIsh.Commands;

[ThirteenIshCommand("test", "A test command")]
internal sealed class TestCommand : IThirteenIshCommand
{
    public Task HandleAsync(SocketSlashCommand command)
    {
        return command.RespondAsync("You ran the test command");
    }
}
