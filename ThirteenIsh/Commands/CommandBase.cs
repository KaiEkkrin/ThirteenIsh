using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace ThirteenIsh.Commands;

/// <summary>
/// Implement slash commands by extending this -- all concrete implementations will be
/// instantiated and registered at runtime.
/// Each class will only be instantiated once, as a singleton.
/// </summary>
internal abstract class CommandBase(string name, string description)
{
    public virtual SlashCommandBuilder CreateBuilder()
    {
        SlashCommandBuilder builder = new();
        builder.WithName($"13-{name}");
        builder.WithDescription(description);
        return builder;
    }

    public abstract Task HandleAsync(SocketSlashCommand command);
}
