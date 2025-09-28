using Discord;
using System.Collections.Frozen;

namespace ThirteenIsh.Game;

/// <summary>
/// Indicates which character types a character system supports.
/// </summary>
[Flags]
public enum CharacterTypeCompatibility
{
    None = 0,
    PlayerCharacter = 1,
    Monster = 2,
    Both = PlayerCharacter | Monster
}

/// <summary>
/// Describes a game system's idea of a particular type of characters.
/// </summary>
public abstract class CharacterSystem
{
    private readonly string _gameSystemName;

    private readonly ImmutableList<GamePropertyGroup> _propertyGroups;
    private readonly FrozenDictionary<string, GamePropertyBase> _properties;
    private readonly ImmutableList<GamePropertyGroup<GameCounter>> _variableCounterGroups;

    protected CharacterSystem(string name, string gameSystemName, CharacterTypeCompatibility compatibility,
        CharacterType? defaultForType, ImmutableList<GamePropertyGroup> propertyGroups)
    {
        Name = name;
        _gameSystemName = gameSystemName;
        Compatibility = compatibility;
        DefaultForType = defaultForType;
        _propertyGroups = propertyGroups;

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

        _properties = properties.ToFrozenDictionary();
        _variableCounterGroups = variableCounterGroupsBuilder.ToImmutable();
    }

    /// <summary>
    /// The "Custom" property group name.
    /// </summary>
    public const string Custom = "Custom";

    /// <summary>
    /// The name of this character system.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Indicates which character types this system supports.
    /// </summary>
    public CharacterTypeCompatibility Compatibility { get; }

    /// <summary>
    /// The character type this system is the default for, if any.
    /// </summary>
    public CharacterType? DefaultForType { get; }

    /// <summary>
    /// Adds this character sheet's fields to the embed -- in their well-known
    /// order of declaration
    /// </summary>
    public EmbedBuilder AddCharacterSheetFields(EmbedBuilder builder, ICharacterBase character,
        IReadOnlyCollection<string>? onlyTheseProperties)
    {
        // Discord only allows adding up to 25 fields to an embed, so we group together
        // our categories of counters here, formatting a table for each one.
        foreach (var group in EnumeratePropertyGroups(character.Sheet))
        {
            var fieldBuilder = group.BuildEmbedField(character, onlyTheseProperties);
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
        IReadOnlyCollection<string>? onlyTheseProperties, bool withTags)
    {
        foreach (var group in EnumeratePropertyGroups(character.Sheet))
        {
            var fieldBuilder = group.BuildEmbedField(character, onlyTheseProperties);
            if (fieldBuilder is null) continue;
            builder.AddField(fieldBuilder);
        }

        if (withTags) AddTagsFields(builder, character);
        return builder;
    }

    /// <summary>
    /// Adds this tracked character's fields to the embed, in their well-known order of declaration.
    /// Only fields with variables.
    /// Like AddCharacterSheetFields.
    /// </summary>
    public EmbedBuilder AddTrackedCharacterVariableFields(EmbedBuilder builder, ITrackedCharacter character,
        IReadOnlyCollection<string>? onlyTheseProperties, bool withTags)
    {
        foreach (var group in EnumerateVariableCounterGroups(character.Sheet))
        {
            var fieldBuilder = group.BuildEmbedField(character, onlyTheseProperties);
            if (fieldBuilder is null) continue;
            builder.AddField(fieldBuilder);
        }

        if (withTags) AddTagsFields(builder, character);
        return builder;
    }

    /// <summary>
    /// Provides the opportunity to write anything short that should come immediately after the
    /// character alias in the encounter initiative table. Used for displaying swarm (mook) count
    /// in 13th Age
    /// </summary>
    public virtual void DecorateCharacterAlias(StringBuilder builder, ITrackedCharacter character)
    {
    }

    /// <summary>
    /// Finds a counter by unambiguous prefix match and a predicate.
    /// This requires the character sheet as well, to support custom counters.
    /// </summary>
    public GameCounter? FindCounter(CharacterSheet sheet, string namePart, Func<GameCounter, bool> predicate)
    {
        // Admit exact match of alias first
        var aliasMatchCounter = EnumeratePropertyGroups(sheet)
            .SelectMany(group => group.Properties)
            .OfType<GameCounter>()
            .Where(counter => predicate(counter) &&
                counter.Alias?.Equals(namePart, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        if (aliasMatchCounter.Count == 1) return aliasMatchCounter[0];

        var matchingCounters = EnumeratePropertyGroups(sheet)
            .SelectMany(group => group.Properties)
            .OfType<GameCounter>()
            .Where(counter => predicate(counter) &&
                counter.Name.StartsWith(namePart, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matchingCounters.Count == 1 ? matchingCounters[0] : null;
    }

    /// <summary>
    /// Finds a property by unambiguous prefix match.
    /// </summary>
    public GamePropertyBase? FindStorableProperty(CharacterSheet sheet, string namePart)
    {
        // Admit exact match of alias first
        var aliasMatchCounter = EnumeratePropertyGroups(sheet)
            .SelectMany(group => group.Properties)
            .Where(property => property is { CanStore: true, IsHidden: false } &&
                property.Alias?.Equals(namePart, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        if (aliasMatchCounter.Count == 1) return aliasMatchCounter[0];

        var matchingProperties = EnumeratePropertyGroups(sheet)
            .SelectMany(group => group.Properties)
            .Where(property => property is { CanStore: true, IsHidden: false } &&
                property.Name.StartsWith(namePart, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matchingProperties.Count == 1 ? matchingProperties[0] : null;
    }

    /// <summary>
    /// Gets the attack bonus to use in the given circumstances.
    /// (Obvious place to extend for game systems that do different things with attacks. More
    /// parameters likely needed.)
    /// </summary>
    public virtual ParseTreeBase? GetAttackBonus(ITrackedCharacter character, Encounter? encounter, ParseTreeBase? bonus)
    {
        return bonus;
    }

    /// <summary>
    /// Gets the default counter to apply combat damage to if none is specified. (Typically hit points)
    /// </summary>
    public virtual GameCounter? GetDefaultDamageCounter(CharacterSheet sheet)
    {
        return null;
    }

    /// <summary>
    /// Gets the counters to show in the encounter table for this character.
    /// </summary>
    public IEnumerable<GameCounter> GetEncounterTableCounters(CharacterSheet sheet)
    {
        return EnumerateVariableCounterGroups(sheet)
            .SelectMany(group => group.GetProperties(c => !string.IsNullOrEmpty(c.Alias)));
    }

    /// <summary>
    /// Gets a property by exact name match (non-custom only.)
    /// </summary>
    public GamePropertyBase? GetProperty(string name)
    {
        return _properties.TryGetValue(name, out var property) ? property : null;
    }

    /// <summary>
    /// Enumerates all property groups.
    /// </summary>
    public IEnumerable<GamePropertyGroup> GetPropertyGroups()
    {
        return _propertyGroups;
    }

    /// <summary>
    /// Gets a property of a known type by exact name match, or throws if not found.
    /// For game logic use :)
    /// </summary>
    public TProperty GetProperty<TProperty>(ICharacterBase character, string name) where TProperty : GamePropertyBase
    {
        return TryGetProperty<TProperty>(character.Sheet, name, out var property)
            ? property
            : throw new ArgumentOutOfRangeException(nameof(name));
    }

    /// <summary>
    /// All the properties to show when adding a new character in this game system.
    /// </summary>
    public IEnumerable<GamePropertyBase> GetShowOnAddProperties()
    {
        return _propertyGroups
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
        foreach (var group in EnumerateVariableCounterGroups(character.Sheet))
        {
            foreach (var counter in group.Properties)
            {
                if (counter.GetStartingValue(character) is { } counterValue)
                    variables.Counters.SetValue(counter.Name, counterValue);
            }
        }

        character.LastUpdated = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Removes any values assigned for custom counters that the character no longer has.
    /// </summary>
    public void ScrubCustomCounters(ITrackedCharacter character)
    {
        var fixes = character.GetFixes();
        fixes.Counters.RemoveValues(c => !TryGetProperty<GameCounter>(character.Sheet, c.Name, out var counter) ||
                                         counter.Options.HasFlag(GameCounterOptions.IsHidden));

        var variables = character.GetVariables();
        variables.Counters.RemoveValues(v => !TryGetProperty<GameCounter>(character.Sheet, v.Name, out var counter) ||
                                             !counter.Options.HasFlag(GameCounterOptions.HasVariable));

        foreach (var counter in EnumerateVariableCounterGroups(character.Sheet).SelectMany(group => group.Properties))
        {
            if (counter.GetVariableValue(character) is not { } variableValue ||
                counter.GetMaxVariableValue(character) is not { } maxValue ||
                variableValue <= maxValue) continue;

            variables.Counters.SetValue(counter.Name, maxValue);
        }
    }

    /// <summary>
    /// Sets the starting values for a new character, if applicable to this game system.
    /// </summary>
    public virtual void SetNewCharacterStartingValues(Character character)
    {
    }

    public bool TryBuildPropertyValueChoiceComponent(string messageId, string propertyName, ICharacterBase character,
        [MaybeNullWhen(false)] out SelectMenuBuilder? menuBuilder, [MaybeNullWhen(true)] out string? errorMessage)
    {
        if (!TryGetProperty(character.Sheet, propertyName, out var property))
        {
            menuBuilder = null;
            errorMessage =
                $"No property '{propertyName}' found in {_gameSystemName} {Name}.";

            return false;
        }

        menuBuilder = new SelectMenuBuilder()
            .WithCustomId(messageId)
            .WithMinValues(1)
            .WithMaxValues(1)
            .WithPlaceholder($"-- {property.Name} --");

        property.AddPropertyValueChoiceOptions(menuBuilder, character);
        errorMessage = null;
        return true;
    }

    protected abstract GameCounter BuildCustomCounter(CustomCounter cc);

    private static void AddTagsFields(EmbedBuilder builder, ITrackedCharacter character)
    {
        if (character.GetVariables().Tags is not { Count: > 0 } tags) return;

        var rows = tags.Select(tag => new TableRow(new TableCell(tag))).ToList();
        var table = TableHelper.BuildTable(rows);

        builder.AddField("Tags", table);
    }

    /// <summary>
    /// Enumerates all property groups for this character sheet, including custom.
    /// </summary>
    private IEnumerable<GamePropertyGroup> EnumeratePropertyGroups(CharacterSheet sheet)
    {
        var groups = _propertyGroups;
        if (sheet.CustomCounters is not { Count: > 0 } ccs) return groups;

        GamePropertyGroupBuilder customBuilder = new(Custom);
        foreach (var cc in ccs)
        {
            customBuilder.AddProperty(BuildCustomCounter(cc));
        }

        customBuilder.OrderByName();
        return groups.Append(customBuilder.Build());
    }

    /// <summary>
    /// Enumerates all variable counter groups for this character sheet, including custom.
    /// </summary>
    private IEnumerable<GamePropertyGroup<GameCounter>> EnumerateVariableCounterGroups(CharacterSheet sheet)
    {
        var groups = _variableCounterGroups;
        if (sheet.CustomCounters is not { Count: > 0 } ccs) return groups;

        var builder = ImmutableList.CreateBuilder<GameCounter>();
        foreach (var cc in ccs)
        {
            if (!cc.Options.HasFlag(GameCounterOptions.HasVariable)) continue;
            builder.Add(BuildCustomCounter(cc));
        }

        builder.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return groups.Append(new GamePropertyGroup<GameCounter>(Custom, builder.ToImmutable()));
    }

    /// <summary>
    /// Gets a property, including custom counters.
    /// </summary>
    private bool TryGetProperty(CharacterSheet sheet, string name, [MaybeNullWhen(false)] out GamePropertyBase property)
    {
        // Search for predefined properties first
        if (_properties.TryGetValue(name, out property)) return true;

        // Then custom counters
        var customCounter = sheet.CustomCounters?.FirstOrDefault(cc => cc.Name == name);
        if (customCounter != null)
        {
            property = BuildCustomCounter(customCounter);
            return true;
        }

        property = null;
        return false;
    }

    /// <summary>
    /// Gets a property of a particular type, including custom counters.
    /// </summary>
    private bool TryGetProperty<TProperty>(CharacterSheet sheet, string name,
        [MaybeNullWhen(false)] out TProperty property)
        where TProperty : GamePropertyBase
    {
        if (TryGetProperty(sheet, name, out var baseProperty) &&
            baseProperty is TProperty typedProperty)
        {
            property = typedProperty;
            return true;
        }

        property = null;
        return false;
    }
}
