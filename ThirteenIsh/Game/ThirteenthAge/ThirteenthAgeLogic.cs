using ThirteenIsh.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.ThirteenthAge;

internal sealed class ThirteenthAgeLogic(
    GameProperty classProperty,
    AbilityBonusCounter dexterityBonusCounter,
    HitPointsCounter hitPointsCounter,
    GameCounter levelCounter
    ) : GameSystemLogicBase
{
    private const string EscalationDie = "EscalationDie";

    public override void EncounterBegin(Encounter encounter)
    {
        encounter.Variables[EscalationDie] = 0;
    }

    public override GameCounterRollResult? EncounterJoin(
        Adventurer adventurer,
        Encounter encounter,
        IRandomWrapper random,
        int rerolls,
        ulong userId)
    {
        int? targetValue = null;
        var initiative = dexterityBonusCounter.Roll(adventurer, null, random, rerolls, ref targetValue);
        encounter.AddCombatant(new AdventurerCombatant
        {
            Initiative = initiative.Roll,
            Name = adventurer.Name,
            UserId = (long)userId
        });

        return initiative;
    }

    public override string GetCharacterSummary(CharacterSheet sheet)
    {
        var characterClass = classProperty.GetValue(sheet);
        var level = levelCounter.GetValue(sheet);
        return $"Level {level} {characterClass}";
    }

    protected override void AddEncounterHeadingRow(List<string[]> data, Encounter encounter)
    {
        base.AddEncounterHeadingRow(data, encounter);
        data.Add(["Escalation Die", $"{encounter.Variables[EscalationDie]}"]);
    }

    protected override void BuildEncounterInitiativeTableRow(Adventure adventure, CombatantBase combatant,
        List<string> row)
    {
        base.BuildEncounterInitiativeTableRow(adventure, combatant, row);

        var hitPointsCell = BuildPointsEncounterTableCell(adventure, combatant, hitPointsCounter);
        row.Add(hitPointsCell);
    }

    protected override bool EncounterNextRound(Encounter encounter, IRandomWrapper random)
    {
        // When we roll over to the next round, increase the escalation die, to a maximum of 6.
        encounter.Variables[EscalationDie] = Math.Min(6, encounter.Variables[EscalationDie] + 1);
        return true;
    }
}
