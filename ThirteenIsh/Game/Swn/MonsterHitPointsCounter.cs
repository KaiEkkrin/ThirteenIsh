using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class MonsterHitPointsCounter(GameCounter hitDiceCounter)
    : GameCounter(SwnSystem.HitPoints, SwnSystem.HitPointsAlias, options: GameCounterOptions.HasVariable)
{
    public override int? GetValue(ICounterSheet sheet)
    {
        var hitDice = hitDiceCounter.GetValue(sheet);
        return GetMonsterHitPoints(hitDice);
    }

    public override int? GetValue(ITrackedCharacter character)
    {
        var hitDice = hitDiceCounter.GetValue(character);
        return GetMonsterHitPoints(hitDice);
    }

    public override int? GetMaxVariableValue(ITrackedCharacter character)
    {
        // A swarm's maximum number of hit points is multiplied by the swarm count
        return base.GetMaxVariableValue(character) * Math.Max(1, character.SwarmCount);
    }

    public override int? GetStartingValue(ITrackedCharacter character)
    {
        // A swarm's starting number of hit points is multiplied by the swarm count
        return base.GetStartingValue(character) * Math.Max(1, character.SwarmCount);
    }

    private static int? GetMonsterHitPoints(int? hitDice)
    {
        if (!hitDice.HasValue) return null;

        // For monsters, hit points are typically hit dice × 4.5 (average of d8)
        // Following the same house rule pattern as PC HP but simpler calculation
        return Math.Max(1, hitDice.Value * 45 / 10);
    }
}