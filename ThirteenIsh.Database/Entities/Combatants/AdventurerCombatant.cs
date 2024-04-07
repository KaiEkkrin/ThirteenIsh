namespace ThirteenIsh.Database.Entities.Combatants;

/// <summary>
/// This combatant is an adventurer. They use the adventurer record's counters.
/// </summary>
public class AdventurerCombatant : CombatantBase
{
    public override CharacterType CharacterType => CharacterType.PlayerCharacter;
}
