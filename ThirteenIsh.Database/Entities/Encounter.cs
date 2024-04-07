using ThirteenIsh.Database.Entities.Combatants;

namespace ThirteenIsh.Database.Entities;

/// <summary>
/// This entity describes an encounter, which happens within a channel in a guild,
/// and is associated with an adventure.
/// </summary>
public class Encounter : EntityBase
{
    public long GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    /// <summary>
    /// The name of the adventure associated with this encounter.
    /// </summary>
    public required string AdventureName { get; set; }

    /// <summary>
    /// The channel ID.
    /// </summary>
    public required ulong ChannelId { get; set; }

    public ICollection<CombatantBase> Combatants { get; set; } = [];

    /// <summary>
    /// The Discord ID of the pinned message relating to this encounter, if any.
    /// </summary>
    public ulong? PinnedMessageId { get; set; }

    /// <summary>
    /// Which round the encounter is on (begins at 1.)
    /// </summary>
    public required int Round { get; set; }

    /// <summary>
    /// Whose turn it is currently; null if the encounter has not yet been begun.
    /// </summary>
    public int? TurnIndex { get; set; }

    /// <summary>
    /// This encounter's variables (game system dependent.)
    /// </summary>
    public Variables Variables { get; set; } = new();
}
