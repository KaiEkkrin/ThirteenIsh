

namespace ThirteenIsh.Game.Swn;

internal class SwnCustomCounter(CustomCounter customCounter, AttackBonusCounter? attackBonusCounter) : SkillCounter(customCounter.Name,
           attackBonusCounter,
           defaultValue: customCounter.DefaultValue,
           minValue: Math.Min(0, customCounter.DefaultValue),
           maxValue: Math.Max(0, customCounter.DefaultValue),
           options: customCounter.Options)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        return base.GetValue(sheet) ?? DefaultValue;
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        return base.GetValue(character) ?? DefaultValue;
    }
}