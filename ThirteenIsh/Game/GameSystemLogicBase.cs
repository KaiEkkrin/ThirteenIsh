using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// Base class for custom logic for the game system that does known things.
/// </summary>
internal abstract class GameSystemLogicBase
{
    /// <summary>
    /// Sets up the beginning of an encounter.
    /// </summary>
    public abstract void EncounterBegin(Encounter encounter);

    /// <summary>
    /// Has this adventurer join an encounter. Returns the roll result and emits
    /// the working.
    /// If this adventurer cannot join the encounter, returns null.
    /// </summary>
    public abstract GameCounterRollResult? EncounterJoin(
        Adventurer adventurer,
        Encounter encounter,
        IRandomWrapper random,
        int rerolls,
        ulong userId);

    /// <summary>
    /// Provides a one-line summary of the character for character list purposes.
    /// </summary>
    public abstract string GetCharacterSummary(CharacterSheet sheet);
}
