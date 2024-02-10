using ThirteenIsh.Entities;

namespace ThirteenIsh.Game.Dragonbane;

internal sealed class DragonbaneLogic(
    GameProperty kinProperty,
    GameProperty professionProperty
    ) : GameSystemLogicBase
{
    public override string GetCharacterSummary(CharacterSheet sheet)
    {
        var kin = kinProperty.GetValue(sheet);
        var profession = professionProperty.GetValue(sheet);
        return $"{kin} {profession}";
    }
}
