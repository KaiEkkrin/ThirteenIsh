

namespace ThirteenIsh.Game.Dragonbane;

internal class MovementCounter(GameProperty kinProperty, GameAbilityCounter agilityCounter)
    : GameCounter("Movement")
{
    public override bool CanStore => false;

    protected override int? GetValueInternal(ICharacterBase character)
    {
        int? baseMovement = kinProperty.GetValue(character) switch
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
        int? modifier = agilityCounter.GetValue(character) switch
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
