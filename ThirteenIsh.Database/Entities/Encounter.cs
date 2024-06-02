using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using ThirteenIsh.Database.Entities.Combatants;

namespace ThirteenIsh.Database.Entities;

/// <summary>
/// This entity describes an encounter, which happens within a channel in a guild,
/// and is associated with an adventure.
/// This is the JSON entity used so that all combatants can be packed into the one Encounter entity in the database.
/// TODO #30 Add as much unit testing as I can for the helper logic I'm including here
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

    /// <summary>
    /// All the combatants, in any order (quick access.)
    /// </summary>
    [NotMapped]
    public IEnumerable<CombatantBase> Combatants => State.Adventurers.OfType<CombatantBase>()
        .Concat(State.Monsters.OfType<CombatantBase>());

    /// <summary>
    /// All the combatants in turn order.
    /// </summary>
    [NotMapped]
    public IEnumerable<CombatantBase> CombatantsInTurnOrder => Combatants
        .Order(CombatantTurnOrderComparer.Instance);

    /// <summary>
    /// The Discord ID of the pinned message relating to this encounter, if any.
    /// </summary>
    public ulong? PinnedMessageId { get; set; }

    /// <summary>
    /// Which round the encounter is on (begins at 1.)
    /// </summary>
    public required int Round { get; set; }

    /// <summary>
    /// The encounter state.
    /// </summary>
    public EncounterState State { get; set; } = new();

    /// <summary>
    /// The alias of the combatant whose turn it is currently;
    /// null if the encounter has not yet been begun.
    /// </summary>
    public string? TurnAlias { get; set; }

    /// <summary>
    /// Gets the current combatant, if any.
    /// </summary>
    public CombatantBase? GetCurrentCombatant()
    {
        return Combatants.FirstOrDefault(c => c.Alias == TurnAlias);
    }

    /// <summary>
    /// Inserts a combatant into the right place in the turn order.
    /// Sets InitiativeAdjustment accordingly.
    /// </summary>
    public void InsertCombatantIntoTurnOrder(CombatantBase combatant)
    {
        if (Combatants.Any(c => c.Alias == combatant.Alias))
            throw new ArgumentException("Already have a combatant with this alias", nameof(combatant));

        foreach (var existingCombatant in CombatantsInTurnOrder)
        {
            if (existingCombatant.Initiative < combatant.Initiative)
            {
                combatant.InitiativeAdjustment = 0;
                AddCombatant(combatant);
                return;
            }

            if (existingCombatant.Initiative == combatant.Initiative)
            {
                // Slot this one in first
                combatant.InitiativeAdjustment = existingCombatant.InitiativeAdjustment + 1;
                AddCombatant(combatant);
                return;
            }
        }

        combatant.InitiativeAdjustment = 0;
        AddCombatant(combatant);
    }

    /// <summary>
    /// Moves to the next turn in the initiative, returning the next combatant.
    /// </summary>
    public CombatantBase? NextTurn(out bool newRound)
    {
        var allCombatants = CombatantsInTurnOrder.ToList();
        if (allCombatants.Count == 0)
        {
            newRound = false;
            return null;
        }

        var index = allCombatants.FindIndex(c => c.Alias == TurnAlias);
        if (index == allCombatants.Count - 1)
        {
            newRound = true;
            TurnAlias = allCombatants[0].Alias;
            return allCombatants[0];
        }
        else
        {
            newRound = false;
            TurnAlias = allCombatants[index + 1].Alias;
            return allCombatants[index + 1];
        }
    }

    /// <summary>
    /// Removes a combatant from combat, if possible.
    /// </summary>
    public CombatantRemoveResult RemoveCombatant(string alias)
    {
        var adventurerIndex = State.Adventurers.FindIndex(c => c.Alias == alias);
        if (adventurerIndex >= 0)
        {
            if (State.Adventurers[adventurerIndex].Alias == TurnAlias) return CombatantRemoveResult.IsTheirTurn;
            State.Adventurers.RemoveAt(adventurerIndex);
            return CombatantRemoveResult.Success;
        }

        var monsterIndex = State.Monsters.FindIndex(c => c.Alias == alias);
        if (monsterIndex >= 0)
        {
            if (State.Monsters[monsterIndex].Alias == TurnAlias) return CombatantRemoveResult.IsTheirTurn;
            State.Monsters.RemoveAt(monsterIndex);
            return CombatantRemoveResult.Success;
        }

        return CombatantRemoveResult.NotFound;
    }

    private void AddCombatant(CombatantBase combatant)
    {
        switch (combatant)
        {
            case AdventurerCombatant adventurerCombatant:
                State.Adventurers.Add(adventurerCombatant);
                break;

            case MonsterCombatant monsterCombatant:
                State.Monsters.Add(monsterCombatant);
                break;

            default:
                throw new InvalidOperationException($"Unrecognised combatant type: {combatant.GetType()}");
        }
    }
}

public enum CombatantRemoveResult
{
    Success,
    NotFound,
    IsTheirTurn
}

/// <summary>
/// The JSON portion of the encounter record, encompassing all the structured data.
/// I've decided to squash all encounter data in a single row to avoid issues with
/// concurrent joins and dealing with the initiative order -- like this all updates will
/// be atomic
/// </summary>
public class EncounterState : ICounterSheet
{
    public virtual IList<AdventurerCombatant> Adventurers { get; set; } = [];

    public virtual IList<MonsterCombatant> Monsters { get; set; } = [];

    /// <summary>
    /// The encounter variable values.
    /// </summary>
    public virtual IList<PropertyValue<int>> Counters { get; set; } = [];
}

