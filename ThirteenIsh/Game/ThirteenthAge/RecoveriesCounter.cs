using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class RecoveriesCounter() : GameCounter("Recoveries")
{
    public override bool CanStore => false;

    public override int GetValue(CharacterSheet characterSheet)
    {
        return 8;
    }
}
