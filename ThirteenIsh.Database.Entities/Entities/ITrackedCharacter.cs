namespace ThirteenIsh.Database.Entities;

/// <summary>
/// Encompasses any tracked character with variables, which can either be a player's adventurer
/// or a monster within an encounter.
/// </summary>
public interface ITrackedCharacter : ICharacterBase
{
    DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// The number of individuals this character is made up of -- used as a multiplier to
    /// the starting and maximum values of variables. By default should be 1 (not a swarm.)
    /// </summary>
    int SwarmCount { get; }

    FixesSheet GetFixes();

    VariablesSheet GetVariables();
}
