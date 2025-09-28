using Microsoft.EntityFrameworkCore;

namespace ThirteenIsh.Database.Entities;

[Index(nameof(UserId), nameof(CharacterType), nameof(Name), IsUnique = true)]
[Index(nameof(UserId), nameof(CharacterType), nameof(NameUpper), IsUnique = true)]
public class Character : SearchableNamedEntityBase, ICharacter, IHasLastEdited
{
    /// <summary>
    /// The owning user ID.
    /// </summary>
    public required ulong UserId { get; set; }

    /// <summary>
    /// The character sheet.
    /// </summary>
    public CharacterSheet Sheet { get; set; } = new();

    /// <summary>
    /// The character type.
    /// </summary>
    public required CharacterType CharacterType { get; set; }

    CharacterType ICharacterBase.Type => CharacterType;

    /// <summary>
    /// The game system this character uses.
    /// </summary>
    public required string GameSystem { get; set; }

    /// <summary>
    /// The character system name (optional - if null, uses default for CharacterType).
    /// </summary>
    public string? CharacterSystemName { get; set; }

    /// <summary>
    /// The datetime last updated.
    /// </summary>
    public DateTimeOffset LastEdited { get; set; }

    public bool TryGetFix(string name, out int fixValue)
    {
        fixValue = 0;
        return false;
    }
}
