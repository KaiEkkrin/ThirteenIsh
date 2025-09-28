

namespace ThirteenIsh.Game.Swn;

internal class EffortCounter(
    GameProperty class1Property,
    GameProperty class2Property,
    AttributeBonusCounter constitutionBonusCounter,
    AttributeBonusCounter wisdomBonusCounter,
    params GameCounter[] psychicSkillCounters)
    : GameCounter(SwnSystem.Effort, options: GameCounterOptions.HasVariable)
{
    protected override int? GetValueInternal(ICharacterBase character)
    {
        return GetEffort(
            class1Property.GetValue(character),
            class2Property.GetValue(character),
            constitutionBonusCounter.GetValue(character),
            wisdomBonusCounter.GetValue(character),
            psychicSkillCounters.Select(counter => counter.GetValue(character)));
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
