using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using ThirteenIsh.Database.Entities.Combatants;

namespace ThirteenIsh.Database.Entities;

/// <summary>
/// This entity describes an encounter, which happens within a channel in a guild,
/// and is associated with an adventure.
/// </summary>
[Index(nameof(GuildId), nameof(ChannelId), IsUnique = true)]
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

    public virtual IList<CombatantBase> Combatants { get; set; } = [];

    /// <summary>
    /// The Discord ID of the pinned message relating to this encounter, if any.
    /// </summary>
    public ulong? PinnedMessageId { get; set; }

    /// <summary>
    /// Which round the encounter is on (begins at 1.)
    /// </summary>
    public required int Round { get; set; }

    /// <summary>
    /// The alias of the combatant whose turn it is currently;
    /// null if the encounter has not yet been begun.
    /// </summary>
    public string? TurnAlias { get; set; }

    /// <summary>
    /// This encounter's variables (game system dependent.)
    /// </summary>
    public EncounterVariables Variables { get; set; } = new();

    /// <summary>
    /// The combatants, in the current initiative order.
    /// </summary>
    [NotMapped]
    public IEnumerable<CombatantBase> CombatantsInTurnOrder => Variables.TurnOrder
        .Join(Combatants, o => o.Alias, c => c.Alias, (_, c) => c);

    /// <summary>
    /// Gets the current combatant, if any.
    /// </summary>
    public CombatantBase? GetCurrentCombatant()
    {
        return Combatants.FirstOrDefault(c => c.Alias == TurnAlias);
    }

    /// <summary>
    /// Inserts a combatant into the right place in the turn order.
    /// </summary>
    public void InsertCombatantIntoTurnOrder(CombatantBase combatant)
    {
        CombatantAlias combatantAlias = new() { Alias = combatant.Alias };
        if (Variables.TurnOrder.Contains(combatantAlias)) return;

        var insertBefore = Combatants.FirstOrDefault(c => c.Initiative < combatant.Initiative);
        if (insertBefore != null &&
            Variables.TurnOrder.IndexOf(new CombatantAlias { Alias = insertBefore.Alias }) is >= 0 and var insertIndex)
        {
            Variables.TurnOrder.Insert(insertIndex, combatantAlias);
        }
        else
        {
            Variables.TurnOrder.Add(combatantAlias);
        }
    }

    /// <summary>
    /// Moves to the next turn in the initiative, returning the next combatant.
    /// </summary>
    public CombatantBase? NextTurn(out bool newRound)
    {
        var currentIndex = !string.IsNullOrEmpty(TurnAlias)
            ? Variables.TurnOrder.IndexOf(new CombatantAlias { Alias = TurnAlias })
            : -1;

        var nextIndex = currentIndex + 1;
        if (nextIndex >= Variables.TurnOrder.Count)
        {
            nextIndex = 0;
            ++Round;
            newRound = true;
        }
        else
        {
            newRound = false;
        }

        TurnAlias = Variables.TurnOrder.ElementAtOrDefault(nextIndex)?.Alias;
        return GetCurrentCombatant();
    }

    /// <summary>
    /// Rebuilds the turn order from the combatants.
    /// </summary>
    public void RebuildTurnOrder()
    {
        Variables.TurnOrder.Clear();
        foreach (var combatant in Combatants.OrderByDescending(c => c.Initiative).ThenBy(c => c.Alias))
        {
            Variables.TurnOrder.Add(new CombatantAlias { Alias = combatant.Alias });
        }
    }
}

public class EncounterVariables : ICounterSheet
{
    /// <summary>
    /// The encounter variable values.
    /// </summary>
    public virtual IList<PropertyValue<int>> Counters { get; set; } = [];

    /// <summary>
    /// The aliases in the initiative in the order in which turns are taken.
    /// </summary>
    public virtual IList<CombatantAlias> TurnOrder { get; set; } = [];
}

public record CombatantAlias
{
    public required string Alias { get; set; }
}
