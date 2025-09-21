using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class EffortCounter(
    GameProperty class1Property,
    GameProperty class2Property,
    AttributeBonusCounter constitutionBonusCounter,
    AttributeBonusCounter wisdomBonusCounter,
    params GameCounter[] psychicSkillCounters)
    : GameCounter(SwnSystem.Effort, options: GameCounterOptions.HasVariable)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        if (sheet is not CharacterSheet characterSheet) return null;
        return GetEffort(
            class1Property.GetValue(characterSheet),
            class2Property.GetValue(characterSheet),
            constitutionBonusCounter.GetValue(sheet),
            wisdomBonusCounter.GetValue(sheet),
            psychicSkillCounters.Select(counter => counter.GetValue(sheet)));
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        var baseValue = GetEffort(
            class1Property.GetValue(character.Sheet),
            class2Property.GetValue(character.Sheet),
            constitutionBonusCounter.GetValue(character),
            wisdomBonusCounter.GetValue(character),
            psychicSkillCounters.Select(counter => counter.GetValue(character)));

        return AddFix(baseValue, character);
    }

    private static int? GetEffort(string class1, string class2, int? conBonus, int? wisBonus, IEnumerable<int?> psychicSkills)
    {
        if (conBonus == null || wisBonus == null) return null;

        // If a character has no psychic skills, they have no Effort score
        var psychicSkillBonus = psychicSkills.Max(s => s ?? -1);
        if (psychicSkillBonus == -1) return null;

        // If a character has at least partial Psychic class, they get the highest of their Con or Wis bonus
        // added to their effort
        var attributeBonus = class1 == SwnSystem.Psychic || class2 == SwnSystem.Psychic
            ? Math.Max(conBonus.Value, wisBonus.Value)
            : 0;

        var effort = 1 + attributeBonus + psychicSkillBonus;
        return effort;
    }
}
