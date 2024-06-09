using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.ThirteenthAge;

internal class ThirteenthAgeCharacterSystem(CharacterType characterType, string gameSystemName,
    ImmutableList<GamePropertyGroup> propertyGroups) : CharacterSystem(characterType, gameSystemName, propertyGroups)
{
    protected override GameCounter BuildCustomCounter(CustomCounter cc)
    {
        return new ThirteenthAgeCustomCounter(cc);
    }

    public override ParseTreeBase? GetAttackBonus(ITrackedCharacter character, Encounter? encounter, ParseTreeBase? bonus)
    {
        // In 13th Age, player characters gain the escalation die as a bonus to attack during combat.
        // (TODO flag on particular monsters if they gain the escalation die, somehow; typically dragons?)
        if (character.Type != CharacterType.PlayerCharacter || encounter == null) return bonus;

        var escalationDie = encounter.State.Counters.TryGetValue(ThirteenthAgeSystem.EscalationDie, out var escalationDieValue)
            ? escalationDieValue
            : 0;

        IntegerParseTree escalationDieParseTree = new(0, escalationDie, "Escalation die");
        return bonus == null
            ? escalationDieParseTree
            : new BinaryOperationParseTree(0, bonus, escalationDieParseTree, '+');
    }
}
