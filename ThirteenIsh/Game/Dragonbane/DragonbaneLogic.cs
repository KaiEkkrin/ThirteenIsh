using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.Dragonbane;

internal sealed class DragonbaneLogic(
    GameProperty kinProperty,
    GameProperty professionProperty
    ) : GameSystemLogicBase
{
    public override void EncounterBegin(Encounter encounter)
    {
        // There's nothing in particular to do here
    }

    public override string GetCharacterSummary(CharacterSheet sheet)
    {
        var kin = kinProperty.GetValue(sheet);
        var profession = professionProperty.GetValue(sheet);
        return $"{kin} {profession}";
    }
}
