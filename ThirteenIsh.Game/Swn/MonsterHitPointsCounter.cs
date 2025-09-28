
namespace ThirteenIsh.Game.Swn;

internal class MonsterHitPointsCounter(GameCounter hitDiceCounter)
    : GameCounter(SwnSystem.HitPoints, SwnSystem.HitPointsAlias, options: GameCounterOptions.HasVariable)
{
    protected override int? GetValueInternal(ICharacterBase character)
    {
        var hitDice = hitDiceCounter.GetValue(character);
        var value = GetMonsterHitPoints(hitDice);

        // A swarm's maximum number of hit points is multiplied by the swarm count
        if (character is ITrackedCharacter trackedCharacter)
            return value * Math.Max(1, trackedCharacter.SwarmCount);

        return value;
    }

    private static int? GetMonsterHitPoints(int? hitDice)
    {
        if (!hitDice.HasValue) return null;

        // For monsters, hit points are typically hit dice × 4.5 (average of d8)
        // Following the same house rule pattern as PC HP but simpler calculation
        return Math.Max(1, hitDice.Value * 45 / 10);
    }
}