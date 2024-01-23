using Discord.WebSocket;

namespace ThirteenIsh.Commands;

internal sealed class TestCommand : CommandBase
{
    public TestCommand() : base("test", "A test command")
    {
    }

    public override Task HandleAsync(SocketSlashCommand command)
    {
        return command.RespondAsync("You ran the test command");
    }
}
