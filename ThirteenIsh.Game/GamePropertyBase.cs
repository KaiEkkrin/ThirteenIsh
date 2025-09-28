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

    public abstract void AddPropertyValueChoiceOptions(SelectMenuBuilder builder, ICharacterBase character);

    public abstract string GetDisplayValue(ICharacterBase character);

    public abstract bool TryEditCharacterProperty(string newValue, ICharacterBase character,
        [MaybeNullWhen(true)] out string errorMessage);

    /// <summary>
    /// Edits a character property, throwing an exception if the edit fails.
    /// This method is mostly intended for test usage; ThirteenIsh itself should call TryEditCharacterProperty
    /// instead. Use this one only when we definitely expect the edit to succeed.
    /// </summary>
    /// <param name="newValue">The new value to set</param>
    /// <param name="character">The character to modify</param>
    /// <exception cref="GamePropertyException">Thrown when the edit fails</exception>
    public void EditCharacterProperty(string newValue, ICharacterBase character)
    {
        if (!TryEditCharacterProperty(newValue, character, out var errorMessage))
        {
            throw new GamePropertyException($"Failed to set {name} to {newValue} : {errorMessage}");
        }
    }
}
