using Discord;
using Discord.WebSocket;

namespace ThirteenIsh.Commands;

/// <summary>
/// Implement slash commands by extending this -- all concrete implementations will be
/// instantiated and registered at runtime.
/// Each class will only be instantiated once, as a singleton.
/// TODO I already have unmanageably many commands -- make `character-*`, `adventure-*` etc into
/// sub-commands.
/// </summary>
internal abstract class CommandBase(string name, string description, params CommandOptionBase[] subOptions)
{
    /// <summary>
    /// Whenever I make any changes that would affect command registrations I should increment
    /// this -- this will cause us to re-register commands with guilds. Otherwise, we won't
    /// (it's time consuming and I suspect Discord would eventually throttle us.)
    /// </summary>
    public const int Version = 9;

    public string Name => $"13-{name}";

    public virtual SlashCommandBuilder CreateBuilder()
    {
        SlashCommandBuilder builder = new();
        builder.WithName(Name);
        builder.WithDescription(description);

        foreach (var option in subOptions)
        {
            builder.AddOption(option.CreateBuilder());
        }

        return builder;
    }

    /// <summary>
    /// Handles a slash command. The default implementation tries to delegate to the
    /// selected sub-command group, or if none does nothing.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="serviceProvider">A scoped service provider to get services from.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The handler task.</returns>
    public virtual Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var option = command.Data.Options.FirstOrDefault();
        if (option is null) return Task.CompletedTask;

        var subOption = subOptions.FirstOrDefault(o => o.Name == option.Name);
        return subOption != null
            ? subOption.HandleAsync(command, option, serviceProvider, cancellationToken)
            : Task.CompletedTask;
    }
}
