using System.Globalization;
using System.Text;
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

    public override void DecorateCharacterAlias(StringBuilder builder, ITrackedCharacter character)
    {
        if (character.SwarmCount <= 1) return;

        var hitPointsCounter = GetDefaultDamageCounter(character.Sheet)
            ?? throw new InvalidOperationException("Failed to find hit points counter");

        if (hitPointsCounter.GetValue(character) is not { } individualHitPoints ||
            hitPointsCounter.GetVariableValue(character) is not { } currentHitPoints) return;

        var currentSwarmCount = Math.DivRem(currentHitPoints, individualHitPoints, out var rem);
        if (rem > 0) ++currentSwarmCount; // round up

        builder.Append(CultureInfo.CurrentCulture, $" x{currentSwarmCount}");
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

    public override GameCounter? GetDefaultDamageCounter(CharacterSheet sheet)
    {
        return FindCounter(sheet, ThirteenthAgeSystem.HitPoints, _ => true);
    }
}
