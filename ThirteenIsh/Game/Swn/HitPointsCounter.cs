using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class HitPointsCounter(GameCounter levelCounter, GameProperty? class1Property = null, GameProperty? class2Property = null,
    AttributeBonusCounter? constitutionBonusCounter = null)
    : GameCounter(SwnSystem.HitPoints, SwnSystem.HitPointsAlias, options: GameCounterOptions.HasVariable)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        if (sheet is not CharacterSheet characterSheet) return null;
        return GetHitPoints(
            levelCounter.GetValue(sheet),
            class1Property?.GetValue(characterSheet),
            class2Property?.GetValue(characterSheet),
            constitutionBonusCounter?.GetValue(sheet));
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        var baseValue = GetHitPoints(
            levelCounter.GetValue(character),
            class1Property?.GetValue(character.Sheet),
            class2Property?.GetValue(character.Sheet),
            constitutionBonusCounter?.GetValue(character));

        return AddFix(baseValue, character);
    }

    private static int? GetHitPoints(int? level, string? class1, string? class2, int? conBonus)
    {
        if (level == null) return null;

        // For monsters (no class or con bonus), use simpler calculation
        if (class1 == null && class2 == null && conBonus == null)
        {
            // Simple monster HP: level * 4.5 (average of d8)
            return Math.Max(1, level.Value * 45 / 10);
        }

        // For PCs, require con bonus
        if (conBonus == null) return null;

        var classBonus = class1 == SwnSystem.Warrior || class2 == SwnSystem.Warrior ? 2 : 0;

        // HOUSE RULE : Rather than rolling, base hit points shall be 6 at level 1, plus
        // an extra 3.5 at every following level, rounded down.
        var baseHitPoints = 6 + Math.Max(0, level.Value - 1) * 35 / 10;
        return Math.Max(1, baseHitPoints + (classBonus + conBonus.Value) * level.Value);
    }
}
