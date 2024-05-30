namespace ThirteenIsh.Database.Entities.Combatants;

/// <summary>
/// This combatant is a monster. It contains the full copy of the monster stats,
/// because each instance of a monster (several of the same name can be added to
/// one encounter) has its own variables, and each one is not persisted beyond
/// that encounter.
/// The alias will be unique. The name will correspond to the monster of this name,
/// owned by the owning user, in the Characters collection.
/// </summary>
public class MonsterCombatant : CombatantBase, ITrackedCharacter
{
    public override CharacterType CharacterType => CharacterType.Monster;

    public required DateTimeOffset LastUpdated { get; set; }

    public required CharacterSheet Sheet { get; set; }

    CharacterType ITrackedCharacter.Type => CharacterType.Monster;

    /// <summary>
    /// This monster's variables. These are the current values of counters that
    /// can have them.
    /// </summary>
    public VariablesSheet Variables { get; set; } = new();

    public ICounterSheet GetVariables() => Variables;

    public override Task<ITrackedCharacter?> GetCharacterAsync(
        DataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((ITrackedCharacter?)this);
    }
}
