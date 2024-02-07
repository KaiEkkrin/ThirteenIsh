using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class RecoveriesCounter() : GameCounter("Recoveries", category: ThirteenthAgeSystem.General, hasVariable: true)
{
    public override bool CanStore => false;

    public override int? GetValue(CharacterSheet characterSheet)
    {
        // TODO implement a customised bonus for this
        return 8;
    }
}
