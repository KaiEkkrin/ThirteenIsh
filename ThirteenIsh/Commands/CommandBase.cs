using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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

    protected static bool TryConvertTo<T>(object? value, [MaybeNullWhen(false)] out T result)
    {
        try
        {
            if (Convert.ChangeType(value, typeof(T), CultureInfo.CurrentCulture) is T convertedValue)
            {
                result = convertedValue;
                return true;
            }
        }
        catch (Exception)
        {
        }

        result = default;
        return false;
    }

    protected static bool TryGetOption<T>(
        SocketSlashCommandData data,
        string name,
        [MaybeNullWhen(false)] out T typedValue)
    {
        if (data.Options.FirstOrDefault(o => o.Name == name) is not { Value: var value })
        {
            typedValue = default;
            return false;
        }

        return TryConvertTo(value, out typedValue);
    }
}
