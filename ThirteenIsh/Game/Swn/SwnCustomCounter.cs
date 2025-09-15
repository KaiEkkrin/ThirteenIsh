using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class SwnCustomCounter(CustomCounter customCounter, AttackBonusCounter attackBonusCounter)
    : SkillCounter(customCounter.Name, attackBonusCounter, customCounter.DefaultValue, customCounter.Options)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        return base.GetValue(sheet) ?? DefaultValue;
    }
}