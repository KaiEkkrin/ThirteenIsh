using Microsoft.EntityFrameworkCore;

namespace ThirteenIsh.Database.Entities;

[Index(nameof(UserId), nameof(Name), IsUnique = true)]
[Index(nameof(UserId), nameof(NameUpper), IsUnique = true)]
public class Character : SearchableNamedEntityBase, IHasLastEdited
{
    /// <summary>
    /// The owning user ID.
    /// </summary>
    public required ulong UserId { get; set; }

    /// <summary>
    /// The character sheet.
    /// </summary>
    public required CharacterSheet Sheet { get; set; }

    /// <summary>
    /// The character type.
    /// </summary>
    public required CharacterType CharacterType { get; set; }

    /// <summary>
    /// The game system this character uses.
    /// </summary>
    public required string GameSystem { get; set; }

    /// <summary>
    /// The datetime last updated.
    /// </summary>
    public DateTimeOffset LastEdited { get; set; }
}
