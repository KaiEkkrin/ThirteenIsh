using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.Dragonbane;

internal class MovementCounter(GameProperty kinProperty, GameAbilityCounter agilityCounter)
    : GameCounter("Movement")
{
    public override bool CanStore => false;

    public override int GetValue(CharacterSheet characterSheet)
    {
        var baseMovement = kinProperty.GetValue(characterSheet) switch
        {
            DragonbaneSystem.Human => 10,
            DragonbaneSystem.Halfling => 8,
            DragonbaneSystem.Dwarf => 8,
            DragonbaneSystem.Elf => 10,
            DragonbaneSystem.Mallard => 8,
            DragonbaneSystem.Wolfkin => 12,
            var k => throw new InvalidOperationException($"Unrecognised kin: {k}")
        };

        var modifier = agilityCounter.GetValue(characterSheet) switch
        {
            >= 1 and <= 6 => -4,
            >= 7 and <= 9 => -2,
            >= 10 and <= 12 => 0,
            >= 13 and <= 15 => 2,
            >= 16 and <= 18 => 4,
            var v => throw new InvalidOperationException($"Invalid value for {agilityCounter.Name} : {v}")
        };

        return baseMovement + modifier;
    }
}
