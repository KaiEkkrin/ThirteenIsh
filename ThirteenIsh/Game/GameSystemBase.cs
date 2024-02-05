namespace ThirteenIsh.Game;

/// <summary>
/// Describes a game system, providing game-specific ways of interacting with it.
/// Game systems are singletons registered in GameSystemRegistration.
/// </summary>
internal abstract class GameSystemBase(string name)
{
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
}
