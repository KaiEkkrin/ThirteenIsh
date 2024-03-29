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

    public static async Task<Embed> BuildAdventureSummaryEmbedAsync(
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
                .WithValue(gameSystem.GetCharacterSummary(adventurer.Sheet, CharacterType.PlayerCharacter)));
        }

        return embedBuilder.Build();
    }

    public static Embed BuildCharacterSheetEmbed(
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

        var characterSystem = GameSystem.Get(character.GameSystem).GetCharacterSystem(character.CharacterType);
        embedBuilder = characterSystem.AddCharacterSheetFields(embedBuilder, character.Sheet, onlyTheseProperties);

        embedBuilder.AddField("Last Edited", $"{character.LastEdited:F}");
        return embedBuilder.Build();
    }

    public static Embed BuildTrackedCharacterSummaryEmbed(
        IDiscordInteraction command,
        ITrackedCharacter character,
        GameSystem gameSystem,
        AdventurerSummaryOptions options)
    {
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle(options.Title);

        var characterSystem = gameSystem.GetCharacterSystem(character.Type);
        embedBuilder = options.OnlyVariables
            ? characterSystem.AddTrackedCharacterVariableFields(embedBuilder, character, options.OnlyTheseProperties)
            : characterSystem.AddTrackedCharacterFields(embedBuilder, character, options.OnlyTheseProperties);

        if (!options.OnlyVariables) embedBuilder.AddField("Last Updated", $"{character.LastUpdated:F}");
        if (options.ExtraFields is not null)
        {
            foreach (var extraFieldBuilder in options.ExtraFields)
            {
                embedBuilder.AddField(extraFieldBuilder);
            }
        }

        return embedBuilder.Build();
    }

    public static async Task RespondWithAdventureSummaryAsync(
        this DiscordService discordService,
        IDiscordInteraction command,
        Guild guild,
        Adventure adventure,
        string title)
    {
        var embed = await discordService.BuildAdventureSummaryEmbedAsync(command, guild, adventure, title);
        await command.RespondAsync(embed: embed);
    }

    public static Task RespondWithAdventurerSummaryAsync(
        IDiscordInteraction command,
        Adventurer adventurer,
        GameSystem gameSystem,
        AdventurerSummaryOptions options)
    {
        var embed = BuildTrackedCharacterSummaryEmbed(command, adventurer, gameSystem, options);
        return command.RespondAsync(embed: embed);
    }

    public static Task RespondWithCharacterSheetAsync(
        IDiscordInteraction command,
        Entities.Character character,
        string title,
        params string[] onlyTheseProperties)
    {
        var embed = BuildCharacterSheetEmbed(command, character, title, onlyTheseProperties);
        return command.RespondAsync(embed: embed);
    }

    public static Dictionary<CharacterType, GameCounter> FindCounterByType(
        GameSystem gameSystem, string namePart, Func<GameCounter, bool> predicate, IEnumerable<CombatantBase> targets)
    {
        Dictionary<CharacterType, GameCounter> dictionary = [];
        foreach (var target in targets)
        {
            if (dictionary.ContainsKey(target.CharacterType)) continue;

            var characterSystem = gameSystem.GetCharacterSystem(target.CharacterType);
            var counter = characterSystem.FindCounter(namePart, predicate);
            if (counter is not null) dictionary.Add(target.CharacterType, counter);
        }

        return dictionary;
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

    public static bool TryFindCombatant(Encounter encounter,
        Func<CombatantBase, bool> predicate, [MaybeNullWhen(false)] out CombatantBase combatant)
    {
        var matchingCombatants = encounter.Combatants.Where(predicate).ToList();
        if (matchingCombatants.Count == 1)
        {
            // This is an unambiguous match.
            combatant = matchingCombatants[0];
            return true;
        }

        combatant = null;
        return false;
    }

    public static bool TryFindCombatants(IEnumerable<string> nameParts, Encounter encounter,
        IList<CombatantBase> combatants, [MaybeNullWhen(true)] out string errorMessage)
    {
        foreach (var namePart in nameParts)
        {
            // Try matching on alias case sensitively first, then on alias case insensitively,
            // and then finally on name prefix (case insensitively).
            if (TryFindCombatant(encounter,
                c => string.Equals(c.Alias, namePart, StringComparison.Ordinal),
                out var combatant))
            {
                if (!combatants.Contains(combatant)) combatants.Add(combatant);
                continue;
            }

            if (TryFindCombatant(encounter,
                c => string.Equals(c.Alias, namePart, StringComparison.OrdinalIgnoreCase),
                out combatant))
            {
                if (!combatants.Contains(combatant)) combatants.Add(combatant);
                continue;
            }

            if (TryFindCombatant(encounter,
                c => c.Name.StartsWith(namePart, StringComparison.OrdinalIgnoreCase),
                out combatant))
            {
                if (!combatants.Contains(combatant)) combatants.Add(combatant);
                continue;
            }

            // If we got here we failed to match anything to this name part
            errorMessage = $"'{namePart}' does not uniquely match any combatants in the current encounter.";
            return false;
        }

        if (combatants.Count == 0)
        {
            errorMessage = "No targets selected.";
            return false;
        }

        errorMessage = null;
        return true;
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

    public static bool TryGetCurrentCombatant(Guild guild, ulong channelId, ulong userId,
        [MaybeNullWhen(false)] out Adventure adventure,
        [MaybeNullWhen(false)] out Adventurer adventurer,
        [MaybeNullWhen(false)] out Encounter encounter,
        [MaybeNullWhen(true)] out string errorMessage)
    {
        if (!TryGetCurrentEncounter(guild, channelId, userId, out adventure, out encounter, out errorMessage))
        {
            adventurer = null;
            return false;
        }

        if (!adventure.Adventurers.TryGetValue(userId, out adventurer))
        {
            encounter = null;
            errorMessage = "You have not joined the current adventure.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static bool TryGetCurrentEncounter(Guild guild, ulong channelId, ulong userId,
        [MaybeNullWhen(false)] out Adventure adventure,
        [MaybeNullWhen(false)] out Encounter encounter,
        [MaybeNullWhen(true)] out string errorMessage)
    {
        adventure = guild.CurrentAdventure;
        if (adventure is null)
        {
            encounter = null;
            errorMessage = "There is no current adventure.";
            return false;
        }

        if (!guild.Encounters.TryGetValue(channelId, out encounter))
        {
            errorMessage = "No encounter is currently in progress in this channel.";
            return false;
        }

        if (encounter.AdventureName != adventure.Name)
        {
            errorMessage = "The current adventure does not match the encounter in progress.";
            return false;
        }

        errorMessage = null;
        return true;
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