namespace ThirteenIsh.Game.Swn;

internal class SwnPlayerCharacterSystem(ImmutableList<GamePropertyGroup> propertyGroups)
    : SwnCharacterSystem(SwnSystem.PlayerCharacter, CharacterTypeCompatibility.PlayerCharacter,
    CharacterType.PlayerCharacter, propertyGroups)
{
    public override void SetNewCharacterStartingValues(Character character)
    {
        // Character level begins at 1
        var level = GetProperty<GameCounter>(character, SwnSystem.Level);
        level.EditCharacterProperty("1", character);

        // Armor value begins at 10
        var armorValue = GetProperty<GameCounter>(character, SwnSystem.ArmorValue);
        armorValue.EditCharacterProperty("10", character);
    }
}