using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class MonsterHitPointsCounter(string name, string alias)
    : GameCounter(name, alias, options: GameCounterOptions.HasVariable)
{
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
}
