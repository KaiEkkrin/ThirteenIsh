using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class SavingThrowCounter(string name, params AttributeBonusCounter[] attributeBonuses)
    : GameCounter(name, options: GameCounterOptions.CanRoll)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        return GetSavingThrow(attributeBonuses.Select(a => a.GetValue(sheet)));
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        return GetSavingThrow(attributeBonuses.Select(a => a.GetValue(character.Sheet)));
    }

    private static int? GetSavingThrow(IEnumerable<int?> bonuses)
    {
        // The saving throw is 15 minus the higher of the two bonuses
        List<int> bonusValues = new(2);
        foreach (var bonus in bonuses)
        {
            if (bonus.HasValue) bonusValues.Add(bonus.Value);
        }

        int? savingThrowValue = bonusValues.Count == 0 ? null : bonusValues.Max();
        return savingThrowValue;
    }
}
