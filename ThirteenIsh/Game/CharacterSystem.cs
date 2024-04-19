using Discord;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// Describes a game system's idea of a particular type of characters.
/// </summary>
internal class CharacterSystem
{
    private readonly string _gameSystemName;

    public CharacterSystem(CharacterType characterType, string gameSystemName,
        ImmutableList<GamePropertyGroup> propertyGroups)
    {
        CharacterType = characterType;
        _gameSystemName = gameSystemName;
        PropertyGroups = propertyGroups;

        Dictionary<string, GamePropertyBase> properties = [];
        var variableCounterGroupsBuilder = ImmutableList.CreateBuilder<GamePropertyGroup<GameCounter>>();
        foreach (var propertyGroup in propertyGroups)
        {
            var variableCountersBuilder = ImmutableList.CreateBuilder<GameCounter>();
            foreach (var property in propertyGroup.Properties)
            {
                properties.Add(property.Name, property);
                if (property is GameCounter counter && counter.Options.HasFlag(GameCounterOptions.HasVariable))
                    variableCountersBuilder.Add(counter);
            }

            if (variableCountersBuilder.Count > 0)
                variableCounterGroupsBuilder.Add(new GamePropertyGroup<GameCounter>(
                    propertyGroup.GroupName, variableCountersBuilder.ToImmutable()));
        }

        Properties = properties.ToFrozenDictionary();
        VariableCounterGroups = variableCounterGroupsBuilder.ToImmutable();
    }

    /// <summary>
    /// The character type.
    /// </summary>
    public CharacterType CharacterType { get; }

    /// <summary>
    /// This character system's properties grouped in display order.
    /// </summary>
    public ImmutableList<GamePropertyGroup> PropertyGroups { get; }

    /// <summary>
    /// This character system's properties indexed by name.
    /// </summary>
    public FrozenDictionary<string, GamePropertyBase> Properties { get; }

    /// <summary>
    /// A filtered copy of PropertyGroups consisting only of those counters with
    /// associated variables.
    /// </summary>
    public ImmutableList<GamePropertyGroup<GameCounter>> VariableCounterGroups { get; }

    /// <summary>
    /// Adds this character sheet's fields to the embed -- in their well-known
    /// order of declaration
    /// </summary>
    public EmbedBuilder AddCharacterSheetFields(EmbedBuilder builder, CharacterSheet sheet,
        IReadOnlyCollection<string>? onlyTheseProperties)
    {
        // Discord only allows adding up to 25 fields to an embed, so we group together
        // our categories of counters here, formatting a table for each one.
        foreach (var group in PropertyGroups)
        {
            var fieldBuilder = group.BuildEmbedField(sheet, onlyTheseProperties);
            if (fieldBuilder is null) continue;
            builder.AddField(fieldBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Adds all this tracked character's fields to the embed, in their well-known order of declaration.
    /// Like AddCharacterSheetFields.
    /// </summary>
    public EmbedBuilder AddTrackedCharacterFields(EmbedBuilder builder, ITrackedCharacter character,
        IReadOnlyCollection<string>? onlyTheseProperties)
    {
        foreach (var group in PropertyGroups)
        {
            var fieldBuilder = group.BuildEmbedField(character, onlyTheseProperties);
            if (fieldBuilder is null) continue;
            builder.AddField(fieldBuilder);
        }

        return builder;
    }

    /// <summary>
    /// Adds this tracked character's fields to the embed, in their well-known order of declaration.
    /// Only fields with variables.
    /// Like AddCharacterSheetFields.
    /// </summary>
    public EmbedBuilder AddTrackedCharacterVariableFields(EmbedBuilder builder, ITrackedCharacter character,
        IReadOnlyCollection<string>? onlyTheseProperties)
    {
        foreach (var group in VariableCounterGroups)
        {
            var fieldBuilder = group.BuildEmbedField(character, onlyTheseProperties);
            if (fieldBuilder is null) continue;
            builder.AddField(fieldBuilder);
        }

        return builder;
    }

    public SelectMenuBuilder BuildPropertyChoiceComponent(string messageId,
        Func<GamePropertyBase, bool> predicate, string? propertyGroupName = null)
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

    public SelectMenuBuilder BuildPropertyGroupChoiceComponent(string messageId,
        Func<GamePropertyBase, bool> predicate)
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
    /// Finds a counter by unambiguous prefix match and a predicate.
    /// </summary>
    public GameCounter? FindCounter(string namePart, Func<GameCounter, bool> predicate)
    {
        // Admit exact match of alias first
        var aliasMatchCounter = Properties.Values
            .OfType<GameCounter>()
            .Where(counter => predicate(counter) &&
                counter.Alias?.Equals(namePart, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        if (aliasMatchCounter.Count == 1) return aliasMatchCounter[0];

        var matchingCounters = Properties.Values
            .OfType<GameCounter>()
            .Where(counter => predicate(counter) &&
                counter.Name.StartsWith(namePart, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matchingCounters.Count == 1 ? matchingCounters[0] : null;
    }

    /// <summary>
    /// Finds a property by unambiguous prefix match.
    /// </summary>
    public GamePropertyBase? FindStorableProperty(string namePart)
    {
        // Admit exact match of alias first
        var aliasMatchCounter = Properties.Values
            .Where(property => property is { CanStore: true, IsHidden: false } &&
                property.Alias?.Equals(namePart, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        if (aliasMatchCounter.Count == 1) return aliasMatchCounter[0];

        var matchingProperties = Properties
            .Where(pair => pair.Value is { CanStore: true, IsHidden: false } &&
                pair.Key.StartsWith(namePart, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matchingProperties.Count == 1 ? matchingProperties[0].Value : null;
    }

    /// <summary>
    /// Gets a property by exact name match.
    /// </summary>
    public GamePropertyBase? GetProperty(string name)
    {
        if (!Properties.TryGetValue(name, out var property)) return null;
        return property;
    }

    /// <summary>
    /// Gets a property of a known type by exact name match, or throws if not found.
    /// For game logic use :)
    /// </summary>
    public TProperty GetProperty<TProperty>(string name) where TProperty : GamePropertyBase
    {
        return (TProperty)Properties[name];
    }

    /// <summary>
    /// All the properties to show when adding a new character in this game system.
    /// </summary>
    public IEnumerable<GamePropertyBase> GetShowOnAddProperties()
    {
        return PropertyGroups
            .SelectMany(group => group.Properties)
            .Where(property => property.ShowOnAdd);
    }

    /// <summary>
    /// Resets all this tracked character's variables to default values.
    /// </summary>
    public void ResetVariables(ITrackedCharacter character)
    {
        // TODO support custom counters (I'll need to declare information about those somewhere)
        var variables = character.GetVariables();
        variables.Counters.Clear();
        foreach (var group in VariableCounterGroups)
        {
            foreach (var counter in group.Properties)
            {
                if (counter.GetValue(character.Sheet) is { } counterValue)
                    variables.Counters.SetValue(counter.Name, counterValue);
            }
        }

        character.LastUpdated = DateTimeOffset.UtcNow;
    }

    public bool TryBuildPropertyValueChoiceComponent(string messageId, string propertyName, CharacterSheet sheet,
        [MaybeNullWhen(false)] out SelectMenuBuilder? menuBuilder, [MaybeNullWhen(true)] out string? errorMessage)
    {
        if (!Properties.TryGetValue(propertyName, out var property))
        {
            menuBuilder = null;
            errorMessage =
                $"No property '{propertyName}' found in {_gameSystemName} {CharacterType.FriendlyName(FriendlyNameOptions.Plural)}.";

            return false;
        }

        menuBuilder = new SelectMenuBuilder()
            .WithCustomId(messageId)
            .WithMinValues(1)
            .WithMaxValues(1)
            .WithPlaceholder($"-- {property.Name} --");

        property.AddPropertyValueChoiceOptions(menuBuilder, sheet);
        errorMessage = null;
        return true;
    }
}
