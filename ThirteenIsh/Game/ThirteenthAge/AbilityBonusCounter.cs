using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class AbilityBonusCounter(GameCounter scoreCounter) : GameCounter($"{scoreCounter.Name} {Suffix}")
{
    public const string Suffix = "Bonus";

    public override bool CanStore => false;

    public override int GetValue(CharacterSheet characterSheet)
    {
        var score = scoreCounter.GetValue(characterSheet);

        // Always round this down, rather than towards zero
        var (div, rem) = Math.DivRem(score - 10, 2);
        return rem < 0 ? div - 1 : div;
    }
}
