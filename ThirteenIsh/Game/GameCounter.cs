using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// A game counter is a numeric value associated with a character, and possibly associated
/// with an adventurer variable.
/// The corresponding entities are a CharacterCounter and an AdventurerVariable.
/// </summary>
internal class GameCounter(string name, string? alias = null, int defaultValue = 0, int minValue = 0,
    int? maxValue = null, bool hasVariable = false)
{
    public string Name => name;

    public string? Alias => alias;

    /// <summary>
    /// True if this counter's value should be stored in the character sheet; false if it
    /// should not be, but instead should be calculated out of other values.
    /// </summary>
    public virtual bool CanStore => true;

    /// <summary>
    /// The default value for this counter.
    /// </summary>
    public int DefaultValue => defaultValue;

    /// <summary>
    /// The minimum value for this counter.
    /// </summary>
    public int MinValue => minValue;

    /// <summary>
    /// The maximum value for this counter, if there is one.
    /// </summary>
    public int? MaxValue => maxValue;

    public bool HasVariable => hasVariable;

    /// <summary>
    /// Gets the starting value of this counter's variable from the character sheet
    /// (only relevant if it has an associated variable.)
    /// </summary>
    public virtual int GetStartingValue(CharacterSheet characterSheet)
    {
        return GetValue(characterSheet);
    }

    /// <summary>
    /// Gets this counter's value from the character sheet.
    /// </summary>
    public virtual int GetValue(CharacterSheet characterSheet)
    {
        var counter = characterSheet.Counters.FirstOrDefault(o => o.Name == name);
        return counter != null ? counter.Value : defaultValue;
    }
}

