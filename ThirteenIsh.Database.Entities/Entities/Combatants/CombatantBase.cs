namespace ThirteenIsh.Database.Entities.Combatants;

public abstract class CombatantBase
{
    /// <summary>
    /// The character system name (optional - if null, uses default for CharacterType).
    /// </summary>
    public string? CharacterSystemName { get; set; }

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
    /// TODO can I make this required?
    /// </summary>
    public int Initiative { get; set; }

    /// <summary>
    /// An initiative adjustment, for organising combatants that share the same
    /// initiative roll.
    /// </summary>
    public int InitiativeAdjustment { get; set; }

    /// <summary>
    /// The initiative roll working, in case we want to display it again.
    /// </summary>
    public string InitiativeRollWorking { get; set; } = string.Empty;

    /// <summary>
    /// This combatant's name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The owning user ID.
    /// </summary>
    public required ulong UserId { get; set; }
}

/// <summary>
/// Provides turn ordering for combatants.
/// </summary>
public sealed class CombatantTurnOrderComparer : Comparer<CombatantBase>
{
    public static readonly CombatantTurnOrderComparer Instance = new();

    private CombatantTurnOrderComparer()
    {
    }

    public override int Compare(CombatantBase? x, CombatantBase? y) => (x, y) switch
    {
        (null, null) => 0,
        (null, _) => -1,
        (_, null) => 1,
        ({ } a, { } b) => a.Initiative.CompareTo(b.Initiative) is var cmp and not 0
            ? -cmp
            : -a.InitiativeAdjustment.CompareTo(b.InitiativeAdjustment)
    };
}
