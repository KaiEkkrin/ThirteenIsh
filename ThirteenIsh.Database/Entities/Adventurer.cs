using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThirteenIsh.Database.Entities;

/// <summary>
/// An Adventurer is a Character within an adventure and combines their sheet
/// (basic stats) with their state (what resources they've expended, etc).
/// The name is the name of the matching character.
/// For now, each user can join each adventure only once.
/// </summary>
[Index(nameof(AdventureId), nameof(Name), IsUnique = true)]
[Index(nameof(AdventureId), nameof(NameUpper), IsUnique = true)]
[Index(nameof(AdventureId), nameof(UserId), IsUnique = true)]
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

    [NotMapped]
    public int SwarmCount => 1;

    public CharacterType Type => CharacterType.PlayerCharacter;

    /// <summary>
    /// The owning user ID.
    /// </summary>
    public required ulong UserId { get; set; }

    public FixesSheet Fixes { get; set; } = new();

    public FixesSheet GetFixes()
    {
        // The Counters property can end up null in the database even though according
        // to our annotations it can't be
        Fixes.Counters ??= [];
        return Fixes;
    }

    /// <summary>
    /// This adventurer's variables. These are the current values of counters that
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
