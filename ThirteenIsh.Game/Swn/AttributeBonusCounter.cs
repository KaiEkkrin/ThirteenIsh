

namespace ThirteenIsh.Game.Swn;

internal class AttributeBonusCounter(GameCounter attributeCounter)
    : GameCounter(GetBonusCounterName(attributeCounter.Name), options: GameCounterOptions.CanRoll)
{
    public override bool CanStore => false;

    protected override int? GetValueInternal(ICharacterBase character)
    {
        var score = attributeCounter.GetValue(character);
        return GetBonusValue(score);
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
