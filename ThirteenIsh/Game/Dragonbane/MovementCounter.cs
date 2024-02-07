using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.Dragonbane;

internal class MovementCounter(GameProperty kinProperty, GameAbilityCounter agilityCounter)
    : GameCounter("Movement", category: DragonbaneSystem.General)
{
    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        int? baseMovement = kinProperty.GetValue(characterSheet) switch
        {
            DragonbaneSystem.Human => 10,
            DragonbaneSystem.Halfling => 8,
            DragonbaneSystem.Dwarf => 8,
            DragonbaneSystem.Elf => 10,
            DragonbaneSystem.Mallard => 8,
            DragonbaneSystem.Wolfkin => 12,
            _ => null
        };

        if (!baseMovement.HasValue) return null;
        int? modifier = agilityCounter.GetValue(characterSheet) switch
        {
            >= 1 and <= 6 => -4,
            >= 7 and <= 9 => -2,
            >= 10 and <= 12 => 0,
            >= 13 and <= 15 => 2,
            >= 16 and <= 18 => 4,
            _ => null
        };

        return baseMovement + modifier;
    }
}
