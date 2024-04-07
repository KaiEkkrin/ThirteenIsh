using Microsoft.EntityFrameworkCore;

namespace ThirteenIsh.Database.Entities;

/// <summary>
/// An Adventure has a collection of characters with state.
/// It exists within a Guild.
/// </summary>
[Index(nameof(GuildId), nameof(Name), IsUnique = true)]
[Index(nameof(GuildId), nameof(NameUpper), IsUnique = true)]
public class Adventure : SearchableNamedEntityBase
{
    public long GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    /// <summary>
    /// A long description of the adventure.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The game system this adventure uses.
    /// </summary>
    public required string GameSystem { get; set; }

    public ICollection<Adventurer> Adventurers { get; set; } = [];
}
