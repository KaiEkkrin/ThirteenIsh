using Discord;

namespace ThirteenIsh.Game;

/// <summary>
/// A base class for all game properties.
/// </summary>
public abstract class GamePropertyBase(string name, string? alias = null, bool isHidden = false)
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

    /// <summary>
    /// If true, offer a dropdown to select this property's value on character creation.
    /// Only for text properties. Maximum of 3 per game system otherwise Discord will blow up
    /// </summary>
    public virtual bool ShowOnAdd => false;

    public abstract void AddPropertyValueChoiceOptions(SelectMenuBuilder builder, CharacterSheet sheet);

    public abstract string GetDisplayValue(ITrackedCharacter character);

    public abstract string GetDisplayValue(CharacterSheet sheet);

    public abstract bool TryEditCharacterProperty(string newValue, CharacterSheet sheet,
        [MaybeNullWhen(true)] out string errorMessage);

    /// <summary>
    /// Edits a character property, throwing an exception if the edit fails.
    /// This method is intended for test usage; ThirteenIsh itself should call TryEditCharacterProperty instead.
    /// </summary>
    /// <param name="newValue">The new value to set</param>
    /// <param name="sheet">The character sheet to modify</param>
    /// <exception cref="EditCharacterPropertyException">Thrown when the edit fails</exception>
    public void EditCharacterProperty(string newValue, CharacterSheet sheet)
    {
        if (!TryEditCharacterProperty(newValue, sheet, out var errorMessage))
        {
            throw new EditCharacterPropertyException(errorMessage);
        }
    }
}
