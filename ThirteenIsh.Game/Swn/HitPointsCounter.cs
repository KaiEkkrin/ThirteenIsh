
namespace ThirteenIsh.Game.Swn;

internal class HitPointsCounter(GameCounter levelCounter, GameProperty class1Property, GameProperty class2Property,
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
        var baseValue = GetHitPoints(
            levelCounter.GetValue(character),
            class1Property.GetValue(character.Sheet),
            class2Property.GetValue(character.Sheet),
            constitutionBonusCounter.GetValue(character));

        return AddFix(baseValue, character);
    }

    private static int? GetHitPoints(int? level, string class1, string class2, int? conBonus)
    {
        // Require both classes and con bonus for PCs, as well as level
        if (level == null || string.IsNullOrEmpty(class1) || string.IsNullOrEmpty(class2) || conBonus == null)
            return null;

        var classBonus = class1 == SwnSystem.Warrior || class2 == SwnSystem.Warrior ? 2 : 0;

        // HOUSE RULE : Rather than rolling, base hit points shall be 6 at level 1, plus
        // an extra 3.5 at every following level, rounded down.
        var baseHitPoints = 6 + Math.Max(0, level.Value - 1) * 35 / 10;
        return Math.Max(1, baseHitPoints + (classBonus + conBonus.Value) * level.Value);
    }
}
