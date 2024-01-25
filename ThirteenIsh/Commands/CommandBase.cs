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

    /// <summary>
    /// Handles a slash command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="serviceProvider">A scoped service provider to get services from.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The handler task.</returns>
    public abstract Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
