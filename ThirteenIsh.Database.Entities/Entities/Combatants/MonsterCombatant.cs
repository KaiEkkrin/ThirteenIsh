namespace ThirteenIsh.Database.Entities.Combatants;

/// <summary>
/// This combatant is a monster. It contains the full copy of the monster stats,
/// because each instance of a monster (several of the same name can be added to
/// one encounter) has its own variables, and each one is not persisted beyond
/// that encounter.
/// The alias will be unique. The name will correspond to the monster of this name,
/// owned by the owning user, in the Characters collection.
/// This is the JSON entity used so that all combatants can be packed into the one Encounter entity in the database.
/// </summary>
public class MonsterCombatant : CombatantBase, ITrackedCharacter
{
    public override CharacterType CharacterType => CharacterType.Monster;

    public required DateTimeOffset LastUpdated { get; set; }

    public required CharacterSheet Sheet { get; set; }

    /// <summary>
    /// The character system name (optional - if null, uses default for CharacterType).
    /// </summary>
    public string? CharacterSystemName { get; set; }

    public int SwarmCount { get; set; } = 1;

    CharacterType ITrackedCharacter.Type => CharacterType.Monster;

    public FixesSheet Fixes { get; set; } = new();

    public FixesSheet GetFixes()
    {
        // The Counters property can end up null in the database even though according
        // to our annotations it can't be
        Fixes.Counters ??= [];
        return Fixes;
    }

    /// <summary>
    /// This monster's variables. These are the current values of counters that
    /// can have them.
    /// </summary>
    public VariablesSheet Variables { get; set; } = new();

    public VariablesSheet GetVariables()
    {
        // The Counters property can end up null in the database even though according
        // to our annotations it can't be
        Variables.Counters ??= [];
        return Variables;
    }

}
