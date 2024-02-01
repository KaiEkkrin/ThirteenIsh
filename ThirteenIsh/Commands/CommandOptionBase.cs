using Discord;
using Discord.WebSocket;

namespace ThirteenIsh.Commands;

internal abstract class CommandOptionBase(string name, string description, ApplicationCommandOptionType optionType)
{
    public string Name => name;
    public string Description => description;

    public virtual SlashCommandOptionBuilder CreateBuilder()
    {
        return new SlashCommandOptionBuilder()
            .WithName(name)
            .WithDescription(description)
            .WithType(optionType);
    }

    public abstract Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
