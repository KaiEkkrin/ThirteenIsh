namespace ThirteenIsh.Game.Swn;

internal class SwnMonsterCharacterSystem(ImmutableList<GamePropertyGroup> propertyGroups)
    : SwnCharacterSystem(SwnSystem.Monster, CharacterTypeCompatibility.Monster,
    CharacterType.Monster, propertyGroups)
{
    public override void SetNewCharacterStartingValues(Character character)
    {
        // Hit Dice begins at 1 for monsters
        var hitDice = GetProperty<GameCounter>(character, SwnSystem.HitDice);
        hitDice.EditCharacterProperty("1", character);

        // Monster AC begins at 10
        var armorClass = GetProperty<GameCounter>(character, SwnSystem.ArmorClass);
        armorClass.EditCharacterProperty("10", character);

        // Morale begins at 7 (typical value)
        var morale = GetProperty<GameCounter>(character, SwnSystem.Morale);
        morale.EditCharacterProperty("7", character);
    }
}