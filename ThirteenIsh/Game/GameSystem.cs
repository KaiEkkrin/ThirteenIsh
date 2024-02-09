using Discord;
using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// Describes a game system, providing game-specific ways of interacting with it.
/// </summary>
internal class GameSystem
{
    public static readonly IReadOnlyList<GameSystem> AllGameSystems = new[]
    {
        ThirteenthAge.ThirteenthAgeSystem.Build(),
        Dragonbane.DragonbaneSystem.Build()
    };

    public GameSystem(string name, ImmutableList<GamePropertyGroup> propertyGroups)
    {
        Name = name;
        PropertyGroups = propertyGroups;

        Dictionary<string, GamePropertyBase> properties = [];
        foreach (var propertyGroup in propertyGroups)
        {
            foreach (var property in propertyGroup.Properties)
            {
                properties.Add(property.Name, property);
            }
        }

        Properties = properties;
    }
    
    /// <summary>
    /// The "Custom" category.
    /// </summary>
    public const string Custom = "Custom";

    /// <summary>
    /// The Unset label shown to the user when character properties haven't been set yet.
    /// </summary>
    public const string Unset = "(unset)";

    /// <summary>
    /// This game system's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// This game system's properties grouped in display order.
    /// </summary>
    public ImmutableList<GamePropertyGroup> PropertyGroups { get; }

    /// <summary>
    /// This game system's properties indexed by name.
    /// </summary>
    public IReadOnlyDictionary<string, GamePropertyBase> Properties { get; }

    /// <summary>
    /// Adds this character sheet's fields to the embed -- in their well-known
    /// order of declaration
    /// </summary>
    public EmbedBuilder AddCharacterSheetFields(EmbedBuilder builder, CharacterSheet sheet,
        string[] onlyTheseProperties)
    {
        // Discord only allows adding up to 25 fields to an embed, so we group together
        // our categories of counters here, formatting a table for each one.
        foreach (var group in PropertyGroups)
        {
            var fieldBuilder = group.BuildEmbedField(sheet, onlyTheseProperties);
            if (fieldBuilder is null) continue;
            builder.AddField(fieldBuilder);
        }

        var customPropertyGroup = BuildCustomPropertyGroup(sheet);
        var customFieldBuilder = customPropertyGroup.BuildEmbedField(sheet, onlyTheseProperties);
        return customFieldBuilder is null
            ? builder
            : builder.AddField(customFieldBuilder);
    }

    /// <summary>
    /// Builds a game properties group containing the custom properties on this character sheet.
    /// </summary>
    public GamePropertyGroup BuildCustomPropertyGroup(CharacterSheet sheet)
    {
        GamePropertyGroupBuilder builder = new(Custom);
        foreach (var (name, _) in sheet.Properties)
        {
            if (Properties.ContainsKey(name)) continue;
            builder.AddProperty(new GameProperty(name, Array.Empty<string>()));
        }

        foreach (var (name, _) in sheet.Counters)
        {
            if (Properties.ContainsKey(name)) continue;
            builder.AddProperty(new GameCounter(name));
        }

        return builder.OrderByName().Build();
    }

    public static SlashCommandOptionBuilder BuildGameSystemChoiceOption(string name)
    {
        var builder = new SlashCommandOptionBuilder()
            .WithName(name)
            .WithDescription("The game system.")
            .WithRequired(true)
            .WithType(ApplicationCommandOptionType.String);

        foreach (var gameSystem in AllGameSystems)
        {
            builder.AddChoice(gameSystem.Name, gameSystem.Name);
        }

        return builder;
    }

    public SelectMenuBuilder BuildPropertyChoiceComponent(string messageId, Func<GamePropertyBase, bool> predicate,
        string? propertyGroupName = null)
    {
        var menuBuilder = new SelectMenuBuilder()
            .WithCustomId(messageId);

        foreach (var propertyGroup in PropertyGroups)
        {
            if (!string.IsNullOrEmpty(propertyGroupName) && propertyGroup.GroupName != propertyGroupName) continue;
            propertyGroup.AddPropertyChoiceOptions(menuBuilder, predicate);
        }

        return menuBuilder;
    }

    public SelectMenuBuilder BuildPropertyGroupChoiceComponent(string messageId, Func<GamePropertyBase, bool> predicate)
    {
        var menuBuilder = new SelectMenuBuilder()
            .WithCustomId(messageId);

        foreach (var propertyGroup in PropertyGroups)
        {
            propertyGroup.AddPropertyGroupChoiceOptions(menuBuilder, predicate);
        }

        return menuBuilder;
    }

    /// <summary>
    /// Edits a character property, writing it to the sheet. Wrap into a closure to pass to
    /// DataService.UpdateCharacterAsync
    /// Here I'll throw if there's a validation error because it's unlikely to occur in practice
    /// (the user should have come through limited select menus etc)
    /// </summary>
    public void EditCharacterProperty(string propertyName, string newValue, CharacterSheet sheet)
    {
        if (!Properties.TryGetValue(propertyName, out var property))
        {
            throw new GamePropertyException($"No property '{propertyName}' found in {Name}.");
        }

        property.EditCharacterProperty(newValue, sheet);
    }

    /// <summary>
    /// Gets the named game system.
    /// </summary>
    public static GameSystem Get(string name) => AllGameSystems.First(o => o.Name == name);

    public bool TryBuildPropertyValueChoiceComponent(string messageId, string propertyName, CharacterSheet sheet,
        [MaybeNullWhen(false)] out SelectMenuBuilder? menuBuilder, [MaybeNullWhen(true)] out string? errorMessage)
    {
        if (!Properties.TryGetValue(propertyName, out var property))
        {
            menuBuilder = null;
            errorMessage = $"No property '{propertyName}' found in {Name}.";
            return false;
        }

        menuBuilder = new SelectMenuBuilder()
            .WithCustomId(messageId);

        property.AddPropertyValueChoiceOptions(menuBuilder, sheet);
        errorMessage = null;
        return true;
    }
}

