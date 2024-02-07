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
        IDiscordInteraction command,
        Guild guild,
        Adventure adventure,
        string title)
    {
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle(adventure.Name == guild.CurrentAdventureName ? $"{title} [Current]" : title);
        embedBuilder.WithDescription(adventure.Description);

        // TODO new-style adventurer summaries
        //foreach (var (userId, adventurer) in adventure.Adventurers.OrderBy(kv => kv.Value.Name))
        //{
        //    var guildUser = await discordService.GetGuildUserAsync(guild.GuildId.Value, userId);
        //    embedBuilder.AddField(new EmbedFieldBuilder()
        //        .WithIsInline(true)
        //        .WithName($"{adventurer.Name} [{guildUser.DisplayName}]")
        //        .WithValue($"Level {adventurer.Sheet.Level} {adventurer.Sheet.Class}"));
        //}

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    public static Task RespondWithAdventurerSummaryAsync(
        IDiscordInteraction command,
        Adventurer adventurer,
        string title)
    {
        // TODO new-style adventurer summaries
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle(title);
//        embedBuilder.WithDescription(@$"Level {adventurer.Sheet.Level} {adventurer.Sheet.Class}
//Last updated on {adventurer.LastUpdated:F}");

        // TODO get game system, etc (this should be configured in the adventure.)
        // AddCharacterSheetFields(embedBuilder, adventurer.Sheet);
        return command.RespondAsync(embed: embedBuilder.Build());
    }

    public static Task RespondWithCharacterSheetAsync(
        IDiscordInteraction command,
        Entities.Character character,
        string title)
    {
        // TODO new-style character summaries
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle(title);

        var gameSystem = GameSystemBase.Get(character.GameSystem);
        embedBuilder = gameSystem.AddCharacterSheetFields(embedBuilder, character.Sheet);
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