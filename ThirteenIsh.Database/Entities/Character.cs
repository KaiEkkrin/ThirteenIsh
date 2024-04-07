using System.ComponentModel.DataAnnotations;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Database.Entities;

public class Character : IHasLastEdited
{
    public required long Id { get; set; }

    /// <summary>
    /// The concurrency token -- see https://www.npgsql.org/efcore/modeling/concurrency.html?tabs=data-annotations
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }

    /// <summary>
    /// The character name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The character name in upper case, for searching.
    /// </summary>
    public string NameUpper => Name.ToUpperInvariant();

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

    /// <summary>
    /// The game system this character uses.
    /// </summary>
    public required string GameSystem { get; set; }

    /// <summary>
    /// The datetime last edited.
    /// </summary>
    public DateTimeOffset LastEdited { get; set; }
}
