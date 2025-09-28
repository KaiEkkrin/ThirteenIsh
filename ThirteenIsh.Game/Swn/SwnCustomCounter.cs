

namespace ThirteenIsh.Game.Swn;

internal class SwnCustomCounter(CustomCounter customCounter, AttackBonusCounter? attackBonusCounter) : SkillCounter(customCounter.Name,
           attackBonusCounter,
           defaultValue: customCounter.DefaultValue,
           minValue: Math.Min(0, customCounter.DefaultValue),
           maxValue: Math.Max(0, customCounter.DefaultValue),
           options: customCounter.Options)
{
    protected override int? GetValueInternal(ICharacterBase character)
    {
        return character.Sheet.Counters.TryGetValue(Name, out var value) ? value : DefaultValue;
    }
}