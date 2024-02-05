using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// A game counter is a numeric value associated with a character, and possibly associated
/// with an adventurer variable.
/// The corresponding entities are a CharacterCounter and an AdventurerVariable.
/// </summary>
internal class GameCounter(string name)
{
    public string Name => name;

    /// <summary>
    /// True if this counter's value should be stored in the character sheet; false if it
    /// should not be, but instead should be calculated out of other values.
    /// </summary>
    public virtual bool CanStore => true;

    /// <summary>
    /// The minimum value for this counter.
    /// </summary>
    public int? MinValue { get; init; }

    /// <summary>
    /// The maximum value for this counter.
    /// </summary>
    public int? MaxValue { get; init; }

    /// <summary>
    /// If this counter has a variable associated with it, the starting/reset value
    /// of the variable.
    /// </summary>
    public int? StartingValue { get; init; }

    /// <summary>
    /// Gets this counter's value from the character sheet.
    /// </summary>
    public virtual int GetValue(CharacterSheet characterSheet)
    {
        var counter = characterSheet.Counters.First(o => o.Name == name);
        return counter.Value;
    }
}

