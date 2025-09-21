
namespace ThirteenIsh.Game;

/// <summary>
/// Abstraction for accessing character data, allowing the Game logic to be decoupled from database implementation.
/// </summary>
public interface ICharacterDataService
{
    /// <summary>
    /// Gets the character associated with a combatant.
    /// </summary>
    Task<ITrackedCharacter?> GetCharacterAsync(
        CombatantBase combatant,
        Encounter encounter,
        CancellationToken cancellationToken = default);
}