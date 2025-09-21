using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class RecoveriesCounter() : GameCounter("Recoveries", options: GameCounterOptions.HasVariable)
{
    public override bool CanStore => false;

    public override int? GetValue(ICounterSheet sheet)
    {
        return 8;
    }
}
