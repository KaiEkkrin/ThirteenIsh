

namespace ThirteenIsh.Game.ThirteenthAge;

internal class RecoveriesCounter() : GameCounter("Recoveries", options: GameCounterOptions.HasVariable)
{
    public override bool CanStore => false;

    protected override int? GetValueInternal(ICharacterBase character)
    {
        return 8;
    }
}
