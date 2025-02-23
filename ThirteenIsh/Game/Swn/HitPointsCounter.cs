using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class HitPointsCounter(GameProperty class1Property, GameProperty class2Property, GameCounter levelCounter,
    AttributeBonusCounter constitutionBonusCounter)
    : GameCounter(SwnSystem.HitPoints, SwnSystem.HitPointsAlias, options: GameCounterOptions.HasVariable)
{
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

        // HOUSE RULE : Rather than rolling, base hit points shall be 6 at level 1, plus
        // an extra 3.5 at every following level, rounded down.
        var baseHitPoints = 6 + Math.Max(0, level.Value - 1) * 35 / 10;
        return Math.Max(1, baseHitPoints + (classBonus + conBonus.Value) * level.Value);
    }
}
