using Discord;
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

        Dictionary<string, GameProperty> properties = [];
        Dictionary<string, GameCounter> counters = [];
        foreach (var propertyGroup in propertyGroups)
        {
            foreach (var property in propertyGroup.Properties)
            {
                switch (property)
                {
                    case GameCounter counter:
                        counters.Add(counter.Name, counter);
                        break;

                    case GameProperty property1:
                        properties.Add(property1.Name, property1);
                        break;
                }
            }
        }

        Properties = properties;
        Counters = counters;
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
    public IReadOnlyDictionary<string, GameProperty> Properties { get; }

    /// <summary>
    /// This game system's counters indexed by name.
    /// </summary>
    public IReadOnlyDictionary<string, GameCounter> Counters { get; }

    /// <summary>
    /// Adds this character sheet's fields to the embed -- in their well-known
    /// order of declaration
    /// </summary>
    public EmbedBuilder AddCharacterSheetFields(EmbedBuilder builder, CharacterSheet sheet)
    {
        // Discord only allows adding up to 25 fields to an embed, so we group together
        // our categories of counters here, formatting a table for each one.
        foreach (var group in PropertyGroups)
        {
            builder.AddField(group.BuildEmbedField(sheet));
        }

        var customPropertyGroup = BuildCustomPropertyGroup(sheet);
        return builder.AddField(customPropertyGroup.BuildEmbedField(sheet));
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
            if (Counters.ContainsKey(name)) continue;
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

    /// <summary>
    /// Gets the named game system.
    /// </summary>
    public static GameSystem Get(string name) => AllGameSystems.First(o => o.Name == name);
}

