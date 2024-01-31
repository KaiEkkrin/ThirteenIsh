using Discord;
using Discord.WebSocket;

namespace ThirteenIsh.Commands;

/// <summary>
/// Implement sub-commands by extending this.
/// </summary>
internal abstract class SubCommandBase(string name, string description)
{
    public string Name => name;

    public virtual SlashCommandOptionBuilder CreateBuilder()
    {
        return new SlashCommandOptionBuilder()
            .WithName(name)
            .WithDescription(description)
            .WithType(ApplicationCommandOptionType.SubCommand);
    }

    public abstract Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
