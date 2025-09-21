using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class AttributeBonusCounter(GameCounter attributeCounter)
    : GameCounter(GetBonusCounterName(attributeCounter.Name), options: GameCounterOptions.CanRoll | GameCounterOptions.IsHidden)
{
    public override bool CanStore => false;

    public override int? GetValue(ICounterSheet sheet)
    {
        var score = attributeCounter.GetValue(sheet);
        return GetBonusValue(score);
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        var score = attributeCounter.GetValue(character);
        var baseValue = GetBonusValue(score);
        return AddFix(baseValue, character);
    }

    public static string GetBonusCounterName(string counterName) => $"{counterName} Bonus";

    private static int? GetBonusValue(int? score)
    {
        return score switch
        {
            null => null,
            <= 3 => -2,
            <= 7 => -1,
            <= 13 => 0,
            <= 17 => 1,
            _ => 2
        };
    }
}
