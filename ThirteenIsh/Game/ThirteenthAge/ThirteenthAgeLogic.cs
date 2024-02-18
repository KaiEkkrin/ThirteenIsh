using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.ThirteenthAge;

internal sealed class ThirteenthAgeLogic(
    GameProperty classProperty,
    GameCounter levelCounter
    ) : GameSystemLogicBase
{
    private const string EscalationDie = "EscalationDie";

    public override void EncounterBegin(Encounter encounter)
    {
        encounter.Variables[EscalationDie] = 0;
    }

    public override string GetCharacterSummary(CharacterSheet sheet)
    {
        var characterClass = classProperty.GetValue(sheet);
        var level = levelCounter.GetValue(sheet);
        return $"Level {level} {characterClass}";
    }
}
