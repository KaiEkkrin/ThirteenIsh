using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class HitPointsCounter(GameProperty class1Property, GameProperty class2Property, GameCounter levelCounter,
    AttributeBonusCounter constitutionBonusCounter)
    : GameCounter(SwnSystem.HitPoints, SwnSystem.HitPointsAlias, options: GameCounterOptions.HasVariable)
{
    // I'm putting a line in the sand here and changing the hit points rule from a rolled 1d6 to a flat 4,
    // because I know that's how I'll want to run the game.
    private const int BaseHitPoints = 4;

    public override int? GetValue(ICounterSheet sheet)
    {
        if (sheet is not CharacterSheet characterSheet) return null;
        return GetHitPoints(
            levelCounter.GetValue(sheet),
            class1Property.GetValue(characterSheet),
            class2Property.GetValue(characterSheet),
            constitutionBonusCounter.GetValue(sheet));
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        return GetHitPoints(
            levelCounter.GetValue(character.Sheet),
            class1Property.GetValue(character.Sheet),
            class2Property.GetValue(character.Sheet),
            constitutionBonusCounter.GetValue(character.Sheet));
    }

    private static int? GetHitPoints(int? level, string class1, string class2, int? conBonus)
    {
        if (level == null || conBonus == null) return null;
        var classBonus = class1 == SwnSystem.Warrior || class2 == SwnSystem.Warrior ? 2 : 0;
        return Math.Max(1, (BaseHitPoints + classBonus + conBonus.Value) * level.Value);
    }
}
