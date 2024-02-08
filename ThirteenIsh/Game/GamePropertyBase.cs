using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// A base class for all game properties.
/// </summary>
internal abstract class GamePropertyBase(string name, string? alias = null, bool isHidden = false)
{
    /// <summary>
    /// Used to indicate in a display that a property needs a value and doesn't yet have one.
    /// </summary>
    public const string Unset = "(unset)";

    public string Name => name;
    public string? Alias => alias;
    public bool IsHidden => isHidden;

    public abstract string GetDisplayValue(CharacterSheet sheet);
}
