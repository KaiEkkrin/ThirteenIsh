using Microsoft.EntityFrameworkCore;

namespace ThirteenIsh.Database.Entities;

/// <summary>
/// This entity type describes our guild-specific state.
/// </summary>
[Index(nameof(GuildId), IsUnique = true)]
public class Guild : EntityBase
{
    /// <summary>
    /// The version of commands most recently registered to this guild.
    /// </summary>
    public int CommandVersion { get; set; }

    /// <summary>
    /// The guild's Discord ID.
    /// </summary>
    public required ulong GuildId { get; set; }

    /// <summary>
    /// This guild's current adventure, if any is selected.
    /// </summary>
    public string CurrentAdventureName { get; set; } = string.Empty;

    /// <summary>
    /// The Discord ID of the role that should have GM permissions in this guild.
    /// If null, falls back to requiring the ManageGuild permission.
    /// </summary>
    public ulong? GmRoleId { get; set; }

    public virtual ICollection<Adventure> Adventures { get; set; } = [];

    public virtual ICollection<Encounter> Encounters { get; set; } = [];
}
