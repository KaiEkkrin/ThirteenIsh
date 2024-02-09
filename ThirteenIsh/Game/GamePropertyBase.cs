using Discord;
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

    /// <summary>
    /// True if this property's value should be stored in the character sheet; false if it
    /// should not be, but instead should be calculated out of other values.
    /// </summary>
    public virtual bool CanStore => true;

    public abstract void AddPropertyValueChoiceOptions(SelectMenuBuilder builder, CharacterSheet sheet);

    public abstract void EditCharacterProperty(string newValue, CharacterSheet sheet);

    public abstract string GetDisplayValue(CharacterSheet sheet);
}
