using System.Globalization;
using System.Text;
using ThirteenIsh.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game.ThirteenthAge;

internal sealed class ThirteenthAgeLogic(
    GameProperty classProperty,
    AbilityBonusCounter dexterityBonusCounter,
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
        IntegerParseTree levelBonus = new(0, levelCounter.GetValue(adventurer.Sheet) ?? 0, "level");
        int? targetValue = null;
        var initiative = dexterityBonusCounter.Roll(adventurer, levelBonus, random, rerolls, ref targetValue);

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
}
