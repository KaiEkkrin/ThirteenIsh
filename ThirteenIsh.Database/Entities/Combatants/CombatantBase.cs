namespace ThirteenIsh.Database.Entities.Combatants;

public abstract class CombatantBase : EntityBase
{
    public long EncounterId { get; set; }
    public Encounter Encounter { get; set; } = null!;

    /// <summary>
    /// This combatant's character type.
    /// </summary>
    public abstract CharacterType CharacterType { get; }

    /// <summary>
    /// This combatant's alias, which identifies it uniquely amongst combatants,
    /// even if it's got the same name as another one (e.g. for monsters.)
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// This combatant's initiative roll result.
    /// </summary>
    public required int Initiative { get; set; }

    /// <summary>
    /// The initiative roll working, in case we want to display it again.
    /// </summary>
    public required string InitiativeRollWorking { get; set; }

    /// <summary>
    /// This combatant's name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// This property, derived from the initiative, determines the actual order
    /// in which the combatants act. It must be unique per encounter.
    /// </summary>
    public required int Order { get; set; }

    /// <summary>
    /// The owning user ID.
    /// </summary>
    public required ulong UserId { get; set; }
}
