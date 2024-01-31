using Discord;
using Discord.WebSocket;

namespace ThirteenIsh.Commands;

internal class SubCommandGroupBase(string name, string description, params SubCommandBase[] subCommands)
{
    public string Name => name;

    public virtual SlashCommandOptionBuilder CreateBuilder()
    {
        var builder = new SlashCommandOptionBuilder()
            .WithName(name)
            .WithDescription(description)
            .WithType(ApplicationCommandOptionType.SubCommandGroup);

        return subCommands.Aggregate(builder, (b, s) => b.AddOption(s.CreateBuilder()));
    }

    public virtual Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var subCommandOption = option.Options.FirstOrDefault();
        if (subCommandOption is null) return Task.CompletedTask;

        var subCommand = subCommands.FirstOrDefault(o => o.Name == subCommandOption.Name);
        return subCommand is not null
            ? subCommand.HandleAsync(command, subCommandOption, serviceProvider, cancellationToken)
            : Task.CompletedTask;
    }
}
