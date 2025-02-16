using Microsoft.EntityFrameworkCore;

namespace ThirteenIsh.Database.Entities.Combatants;

/// <summary>
/// This combatant is an adventurer. They use the adventurer record's counters.
/// This is the JSON entity used so that all combatants can be packed into the one Encounter entity in the database.
/// </summary>
public class AdventurerCombatant : CombatantBase
{
    public override CharacterType CharacterType => CharacterType.PlayerCharacter;

    public override async Task<ITrackedCharacter?> GetCharacterAsync(
        DataContext dataContext,
        Encounter encounter,
        CancellationToken cancellationToken = default)
    {
        var adventurer = await dataContext.Adventurers.SingleOrDefaultAsync(
            a => a.Adventure.GuildId == encounter.GuildId &&
                 a.Adventure.Name == encounter.AdventureName &&
                 a.Name == Name,
            cancellationToken);

        return adventurer;
    }
}
