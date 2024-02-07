using Discord;
using System.Reflection;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// Describes a game system, providing game-specific ways of interacting with it.
/// Game system instances should be immutable singletons and not part of the dependency
/// injection subsystem.
/// </summary>
internal abstract class GameSystemBase(string name)
{
    /// <summary>
    /// The "Other" category.
    /// </summary>
    public const string Other = "Other";

    /// <summary>
    /// The Unset label shown to the user when character properties haven't been set yet.
    /// </summary>
    public const string Unset = "(unset)";

    /// <summary>
    /// Access this to enumerate all game systems.
    /// </summary>
    public static readonly IReadOnlyCollection<GameSystemBase> AllGameSystems = BuildAllGameSystemsList();

    public string Name => name;

    /// <summary>
    /// Enumerates this game's character properties in the order they should appear.
    /// </summary>
    public abstract IReadOnlyList<GameProperty> Properties { get; }

    /// <summary>
    /// Enumerates this game's character counters in the order they should appear.
    /// </summary>
    public abstract IReadOnlyList<GameCounter> Counters { get; }

    /// <summary>
    /// Adds this character sheet's fields to the embed -- in their well-known
    /// order of declaration
    /// </summary>
    public EmbedBuilder AddCharacterSheetFields(EmbedBuilder builder, CharacterSheet sheet)
    {
        // Discord only allows adding up to 25 fields to an embed, so we group together
        // our categories of counters here, formatting a table for each one.
        foreach (var grouping in EnumerateFormattedProperties(sheet)
            .Concat(EnumerateFormattedCounters(sheet))
            .GroupBy(formattedProperty => formattedProperty.Name)
            .OrderBy(grouping => GetCategoryOrder(grouping.Key)))
        {
            var rows = grouping.OrderBy(format => GetPropertyOrder(format.Name))
                .Select(format => new[] { format.Name, format.Value });

            var table = DiscordUtil.BuildTable(2, rows, 1);
            builder.AddField(new EmbedFieldBuilder()
                .WithName(grouping.Key)
                .WithValue(table));
        }

        return builder;
    }

    // TODO no get rid of this entirely -- do it all with single commands
    // More awkward but clearly necessary it seems
    public MessageComponent BuildCharacterEditor(string customId, CharacterSheet? sheet)
    {
        ComponentBuilder builder = new();
        foreach (var property in Properties)
        {
            property.AddCharacterEditorComponent(builder, customId, sheet);
        }

        // TODO looks like I can't have a counters editor here sadly -- too big!
        // Going to have to do it in separate commands instead
        // (`character counter set` etc...)
        //foreach (var counter in Counters)
        //{
        //    counter.AddCharacterEditorComponent(builder, customId, sheet);
        //}

        return builder.Build();
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

    /// <summary>
    /// Gets the named game system.
    /// </summary>
    public static GameSystemBase Get(string name) => AllGameSystems.First(o => o.Name == name);

    /// <summary>
    /// Override to declare the print order of categories in this system.
    /// </summary>
    protected abstract int GetCategoryOrder(string categoryName);

    /// <summary>
    /// Override to declare the print order of properties in this system.
    /// </summary>
    protected abstract int GetPropertyOrder(string propertyName);

    /// <summary>
    /// Call at the end of the concrete class's constructor.
    /// </summary>
    protected void Validate()
    {
        // All properties and counters must have unique names. All counters with
        // aliases must have unique ones. All non-custom properties and counters, except
        // hidden ones, must be categorised.
        HashSet<string> names = [];
        HashSet<string> aliases = [];

        foreach (var property in Properties)
        {
            if (!names.Add(property.Name))
                throw new InvalidOperationException($"{Name}: Found two properties named {property.Name}");

            if (string.IsNullOrEmpty(property.Category))
                throw new InvalidOperationException($"{Name}: Property {property.Name} has no category");
        }

        foreach (var counter in Counters)
        {
            if (!names.Add(counter.Name))
                throw new InvalidOperationException($"{Name}: Found two properties or counters named {counter.Name}");

            if (counter.Alias is not null &&
                !aliases.Add(counter.Alias))
                throw new InvalidOperationException($"{Name}: Found two counters aliased {counter.Alias}");

            if (!counter.IsHidden && string.IsNullOrEmpty(counter.Category))
                throw new InvalidOperationException($"{Name}: Counter {counter.Name} has no category");
        }
    }

    private static List<GameSystemBase> BuildAllGameSystemsList()
    {
        List<GameSystemBase> list = [];
        foreach (var ty in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (!ty.IsClass || ty.IsAbstract || !ty.IsAssignableTo(typeof(GameSystemBase))) continue;
            if (Activator.CreateInstance(ty) is not GameSystemBase gameSystem)
                throw new InvalidOperationException($"Error instantiating {ty}");

            list.Add(gameSystem);
        }

        return list;
    }

    private HashSet<string> BuildKnownFieldNameSet()
    {
        HashSet<string> names = [];
        foreach (var property in Properties)
        {
            names.Add(property.Name);
        }

        foreach (var counter in Counters)
        {
            names.Add(counter.Name);
        }

        return names;
    }

    private IEnumerable<FormattedProperty> EnumerateFormattedCounters(CharacterSheet sheet)
    {
        Dictionary<string, int> sheetCounters = new(sheet.Counters);

        // Emit the standard counters that aren't hidden (they may not all be in the sheet)
        foreach (var counter in Counters)
        {
            sheetCounters.Remove(counter.Name);
            if (counter.IsHidden) continue;
            var formattedValue = counter.GetValue(sheet) is { } value
                ? $"{value}"
                : Unset;

            yield return new FormattedProperty(counter.Category ?? Other, counter.Name, formattedValue);
        }

        // Emit any other counters on the sheet -- these are custom counters
        foreach (var (name, value) in sheetCounters)
        {
            yield return new FormattedProperty(Other, name, $"{value}");
        }
    }

    private IEnumerable<FormattedProperty> EnumerateFormattedProperties(CharacterSheet sheet)
    {
        Dictionary<string, string> sheetProperties = new(sheet.Properties);

        // Emit the standard properties (they may not all be in the sheet)
        foreach (var property in Properties)
        {
            sheetProperties.Remove(property.Name);
            var formattedValue = property.GetValue(sheet) is { Length: > 0 } value
                ? value
                : Unset;

            yield return new FormattedProperty(property.Category ?? Other, property.Name, formattedValue);
        }

        // Emit any other properties on the sheet -- these are custom properties
        foreach (var (name, value) in sheetProperties)
        {
            yield return new FormattedProperty(Other, name, value is { Length: > 0 } ? value : Unset);
        }
    }

    private readonly record struct FormattedProperty(string Category, string Name, string Value);
}
