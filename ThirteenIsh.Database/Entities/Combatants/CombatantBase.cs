using Microsoft.EntityFrameworkCore;

namespace ThirteenIsh.Database.Entities.Combatants;

[Index(nameof(EncounterId), nameof(Alias), IsUnique = true)]
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
    public int Initiative { get; set; }

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

    public abstract Task<ITrackedCharacter?> GetCharacterAsync(
        DataContext dataContext,
        CancellationToken cancellationToken = default);
}
