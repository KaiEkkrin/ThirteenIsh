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
}
