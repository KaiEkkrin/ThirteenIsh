using Microsoft.EntityFrameworkCore;

namespace ThirteenIsh.Database.Entities.Combatants;

/// <summary>
/// This combatant is an adventurer. They use the adventurer record's counters.
/// </summary>
public class AdventurerCombatant : CombatantBase
{
    public override CharacterType CharacterType => CharacterType.PlayerCharacter;

    public override async Task<ITrackedCharacter?> GetCharacterAsync(
        DataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        var encounter = Encounter ?? await dataContext.Encounters.SingleOrDefaultAsync(
            e => e.Id == EncounterId, cancellationToken);

        if (encounter is null) return null;

        var adventurer = await dataContext.Adventurers.SingleOrDefaultAsync(
            a => a.Adventure.GuildId == encounter.GuildId &&
                 a.Adventure.Name == encounter.AdventureName &&
                 a.Name == Name,
            cancellationToken);

        return adventurer;
    }
}
