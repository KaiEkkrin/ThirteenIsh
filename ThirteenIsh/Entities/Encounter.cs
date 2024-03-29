using MongoDB.Bson.Serialization.Attributes;
using ThirteenIsh.Game;

namespace ThirteenIsh.Entities;

/// <summary>
/// This entity describes an encounter, which happens within a channel in a guild.
/// </summary>
public class Encounter
{
    /// <summary>
    /// The name of the adventure this encounter is set in.
    /// </summary>
    public string AdventureName { get; set; } = string.Empty;

    /// <summary>
    /// The list of combatants in turn order.
    /// Call AddCombatant() to add one in the right place.
    /// </summary>
    public List<CombatantBase> Combatants { get; set; } = [];

    /// <summary>
    /// The Discord ID of the pinned message relating to this encounter, if any.
    /// </summary>
    public long? PinnedMessageId { get; set; }

    [BsonIgnore]
    public ulong? NativePinnedMessageId => (ulong?)PinnedMessageId;

    /// <summary>
    /// Which round the encounter is on (begins at 1.)
    /// </summary>
    public int Round { get; set; } = 1;

    /// <summary>
    /// Whose turn it is currently; null if the encounter has not yet been begun.
    /// </summary>
    public int? TurnIndex { get; set; }

    /// <summary>
    /// This encounter's variables (game system dependent.)
    /// </summary>
    public Dictionary<string, int> Variables { get; set; } = [];

    /// <summary>
    /// Adds a combatant into the place in the list determined by their initiative.
    /// </summary>
    public void AddCombatant(CombatantBase combatant)
    {
        for (var i = 0; i < Combatants.Count; i++)
        {
            if (combatant.Initiative <= Combatants[i].Initiative) continue;

            Combatants.Insert(i, combatant);
            if (TurnIndex >= i) ++TurnIndex; // keep it the same combatant's turn
            return;
        }

        Combatants.Add(combatant);
    }

    /// <summary>
    /// Builds a name alias collection representing the current combatants in this encounter,
    /// to enable us to generate unique aliases for new combatants.
    /// </summary>
    internal NameAliasCollection BuildNameAliasCollection()
    {
        return new NameAliasCollection(Combatants.Select(c => (c.Alias, c.Name)));
    }
}
