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
    public static SlashCommandOptionBuilder AddRerollsOption(this SlashCommandOptionBuilder builder, string name)
    {
        return builder.AddOption(new SlashCommandOptionBuilder()
                .WithName(name)
                .WithDescription("A number of rerolls")
                .WithType(ApplicationCommandOptionType.Integer)
                .AddChoice("3", 3)
                .AddChoice("2", 2)
                .AddChoice("1", 1)
                .AddChoice("0", 0)
                .AddChoice("-1", -1)
                .AddChoice("-2", -2)
                .AddChoice("-3", -3));
    }

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
        embedBuilder.AddField("Game System", adventure.GameSystem);

        var gameSystem = GameSystem.Get(adventure.GameSystem);
        foreach (var (userId, adventurer) in adventure.Adventurers.OrderBy(pair => pair.Value.Name))
        {
            var guildUser = await discordService.GetGuildUserAsync(guild.NativeGuildId, userId);
            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName($"{adventurer.Name} [{guildUser.DisplayName}]")
                .WithValue(gameSystem.Logic.GetCharacterSummary(adventurer.Sheet)));
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    public static Task RespondWithAdventurerSummaryAsync(
        IDiscordInteraction command,
        Adventurer adventurer,
        GameSystem gameSystem,
        AdventurerSummaryOptions options)
    {
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle(options.Title);

        embedBuilder = options.OnlyVariables
            ? gameSystem.AddAdventurerVariableFields(embedBuilder, adventurer, options.OnlyTheseProperties)
            : gameSystem.AddAdventurerFields(embedBuilder, adventurer, options.OnlyTheseProperties);

        if (!options.OnlyVariables) embedBuilder.AddField("Last Updated", $"{adventurer.LastUpdated:F}");
        if (options.ExtraFields is not null)
        {
            foreach (var extraFieldBuilder in options.ExtraFields)
            {
                embedBuilder.AddField(extraFieldBuilder);
            }
        }

        return command.RespondAsync(embed: embedBuilder.Build());
    }

    public static Task RespondWithCharacterSheetAsync(
        IDiscordInteraction command,
        Entities.Character character,
        string title,
        params string[] onlyTheseProperties)
    {
        // TODO new-style character summaries
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle(title);
        embedBuilder.AddField("Game System", character.GameSystem);

        var gameSystem = GameSystem.Get(character.GameSystem);
        embedBuilder = gameSystem.AddCharacterSheetFields(embedBuilder, character.Sheet, onlyTheseProperties);

        embedBuilder.AddField("Last Edited", $"{character.LastEdited:F}");
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

    public readonly struct AdventurerSummaryOptions
    {
        /// <summary>
        /// If set, a collection of extra fields to add to the bottom of the embed.
        /// </summary>
        public IReadOnlyCollection<EmbedFieldBuilder>? ExtraFields { get; init; }

        /// <summary>
        /// If set and non-empty, only these properties will be returned.
        /// </summary>
        public IReadOnlyCollection<string>? OnlyTheseProperties { get; init; }

        /// <summary>
        /// If set, only variables will be returned. Otherwise, everything will be returned.
        /// </summary>
        public bool OnlyVariables { get; init; }

        /// <summary>
        /// The title to use.
        /// </summary>
        public string Title { get; init; }
    }
}