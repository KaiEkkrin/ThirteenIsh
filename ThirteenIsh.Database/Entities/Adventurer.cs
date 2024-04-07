using Microsoft.EntityFrameworkCore;

namespace ThirteenIsh.Database.Entities;

/// <summary>
/// An Adventurer is a Character within an adventure and combines their sheet
/// (basic stats) with their state (what resources they've expended, etc).
/// The name is the name of the matching character.
/// </summary>
[Index(nameof(AdventureId), nameof(Name), IsUnique = true)]
[Index(nameof(AdventureId), nameof(NameUpper), IsUnique = true)]
public class Adventurer : SearchableNamedEntityBase, ITrackedCharacter
{
    public long AdventureId { get; set; }
    public Adventure Adventure { get; set; } = null!;

    /// <summary>
    /// The datetime this adventurer was last updated from their character.
    /// </summary>
    public required DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// The copy of the character's sheet as of the last updated time.
    /// </summary>
    public required CharacterSheet Sheet { get; set; }

    public CharacterType Type => CharacterType.PlayerCharacter;

    /// <summary>
    /// The owning user ID.
    /// </summary>
    public required ulong UserId { get; set; }

    /// <summary>
    /// This adventurer's variables. These are the current values of counters that
    /// can have them.
    /// </summary>
    public Variables Variables { get; set; } = new();

    IList<CharacterCounter> ITrackedCharacter.Variables => Variables.Counters;
}
