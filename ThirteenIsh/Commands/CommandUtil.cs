using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal static class CommandUtil
{
    public static async Task RespondWithAdventureSummaryAsync(
        this DiscordService discordService,
        SocketSlashCommand command,
        Guild guild,
        Adventure adventure,
        string title)
    {
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle(adventure.Name == guild.CurrentAdventureName ? $"{title} [Current]" : title);
        embedBuilder.WithDescription(adventure.Description);

        foreach (var (userId, adventurer) in adventure.Adventurers.OrderBy(kv => kv.Value.Name))
        {
            var guildUser = await discordService.GetGuildUserAsync(guild.NativeGuildId, userId);
            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName($"{adventurer.Name} [{guildUser.DisplayName}]")
                .WithValue($"Level {adventurer.Sheet.Level} {adventurer.Sheet.Class}"));
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    public static Task RespondWithCharacterSheetAsync(
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

    public static bool TryConvertTo<T>(object? value, [MaybeNullWhen(false)] out T result)
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

    public static bool TryGetCanonicalizedMultiPartOption(
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

    public static bool TryGetCanonicalizedMultiPartOption(
        SocketSlashCommandDataOption option, string name, [MaybeNullWhen(false)] out string canonicalizedValue)
    {
        if (TryGetOption<string>(option, name, out var value) &&
            AttributeName.TryCanonicalizeMultiPart(value, out canonicalizedValue))
        {
            return true;
        }

        canonicalizedValue = default;
        return false;
    }

    public static bool TryGetCanonicalizedOption(
        SocketSlashCommandData data, string name, [MaybeNullWhen(false)] out string canonicalizedValue)
    {
        if (TryGetOption<string>(data, name, out var value) &&
            AttributeName.TryCanonicalize(value, out canonicalizedValue))
        {
            return true;
        }

        canonicalizedValue = default;
        return false;
    }

    public static bool TryGetCanonicalizedOption(
        SocketSlashCommandDataOption option, string name, [MaybeNullWhen(false)] out string canonicalizedValue)
    {
        if (TryGetOption<string>(option, name, out var value) &&
            AttributeName.TryCanonicalize(value, out canonicalizedValue))
        {
            return true;
        }

        canonicalizedValue = default;
        return false;
    }

    public static bool TryGetOption<T>(
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

    public static bool TryGetOption<T>(
        SocketSlashCommandDataOption option,
        string name,
        [MaybeNullWhen(false)] out T typedValue)
    {
        if (option.Options.FirstOrDefault(o => o.Name == name) is not { Value: var value })
        {
            typedValue = default;
            return false;
        }

        return TryConvertTo(value, out typedValue);
    }

    public static bool TryGetSelectedAdventure(Guild guild, SocketSlashCommandDataOption option,
        string name, [MaybeNullWhen(false)] out Adventure adventure)
    {
        var adventureName = TryGetCanonicalizedMultiPartOption(option, name, out var optionName)
            ? optionName
            : guild.CurrentAdventureName;

        if (string.IsNullOrWhiteSpace(adventureName))
        {
            adventure = null;
            return false;
        }
        else if (guild.Adventures.FirstOrDefault(o => o.Name == adventureName) is { } selectedAdventure)
        {
            adventure = selectedAdventure;
            return true;
        }

        adventure = null;
        return false;
    }
}