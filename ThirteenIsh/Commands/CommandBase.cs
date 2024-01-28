﻿using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.Commands;

/// <summary>
/// Implement slash commands by extending this -- all concrete implementations will be
/// instantiated and registered at runtime.
/// Each class will only be instantiated once, as a singleton.
/// </summary>
internal abstract class CommandBase(string name, string description)
{
    /// <summary>
    /// Whenever I make any changes that would affect command registrations I should increment
    /// this -- this will cause us to re-register commands with guilds. Otherwise, we won't
    /// (it's time consuming and I suspect Discord would eventually throttle us.)
    /// </summary>
    public const int Version = 1;

    public string Name => $"13-{name}";

    public virtual SlashCommandBuilder CreateBuilder()
    {
        SlashCommandBuilder builder = new();
        builder.WithName(Name);
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

    protected static Task RespondWithCharacterSheetAsync(
        SocketSlashCommand command,
        CharacterSheet sheet,
        string title)
    {
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle(title);
        embedBuilder.WithDescription($"Level {sheet.Level} {sheet.Class}");

        foreach (var (abilityName, abilityScore) in sheet.AbilityScores)
        {
            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName(abilityName)
                .WithValue(abilityScore));
        }

        return command.RespondAsync(embed: embedBuilder.Build());
    }

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

    protected static bool TryGetCanonicalizedMultiPartOption(
        SocketSlashCommandData data, string name, [MaybeNullWhen(false)] out string canonicalizedValue)
    {
        if (TryGetOption<string>(data, name, out var value) &&
            AttributeName.TryCanonicalizeMultiPart(value, out canonicalizedValue))
        {
            return true;
        }

        canonicalizedValue = default;
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
