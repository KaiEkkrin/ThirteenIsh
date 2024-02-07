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
    /// Call at the end of the concrete class's constructor.
    /// </summary>
    protected void Validate()
    {
        // All properties and counters must have unique names. All counters with
        // aliases must have unique ones.
        HashSet<string> names = [];
        HashSet<string> aliases = [];

        foreach (var property in Properties)
        {
            if (!names.Add(property.Name))
                throw new InvalidOperationException($"{name}: Found two properties named {property.Name}");
        }

        foreach (var counter in Counters)
        {
            if (!names.Add(counter.Name))
                throw new InvalidOperationException($"{name}: Found two properties or counters named {counter.Name}");

            if (counter.Alias is not null &&
                !aliases.Add(counter.Alias))
                throw new InvalidOperationException($"{name}: Found two counters aliased {counter.Alias}");
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
}
