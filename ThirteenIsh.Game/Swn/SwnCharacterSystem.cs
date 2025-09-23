

namespace ThirteenIsh.Game.Swn;

internal class SwnCharacterSystem(CharacterType characterType, ImmutableList<GamePropertyGroup> propertyGroups)
    : CharacterSystem(characterType, SwnSystem.SystemName, propertyGroups)
{
    public override void SetNewCharacterStartingValues(Character character)
    {
        if (CharacterType == CharacterType.PlayerCharacter)
        {
            // Character level begins at 1
            var level = GetProperty<GameCounter>(character.Sheet, SwnSystem.Level);
            level.EditCharacterProperty("1", character.Sheet);

            // Armor value begins at 10
            var armorValue = GetProperty<GameCounter>(character.Sheet, SwnSystem.ArmorValue);
            armorValue.EditCharacterProperty("10", character.Sheet);
        }
        else if (CharacterType == CharacterType.Monster)
        {
            // Hit Dice begins at 1 for monsters
            var hitDice = GetProperty<GameCounter>(character.Sheet, SwnSystem.HitDice);
            hitDice.EditCharacterProperty("1", character.Sheet);

            // Monster AC begins at 10
            var armorClass = GetProperty<GameCounter>(character.Sheet, SwnSystem.ArmorClass);
            armorClass.EditCharacterProperty("10", character.Sheet);

            // Morale begins at 7 (typical value)
            var morale = GetProperty<GameCounter>(character.Sheet, "Morale");
            morale.EditCharacterProperty("7", character.Sheet);
        }
    }

    protected override GameCounter BuildCustomCounter(CustomCounter cc)
    {
        AttackBonusCounter? attackBonusCounter = null;
        if (CharacterType == CharacterType.PlayerCharacter)
        {
            // Get the attack bonus counter by its well-known name for PCs
            attackBonusCounter = (AttackBonusCounter)GetProperty(SwnSystem.AttackBonus)!;
        }
        // For monsters, attackBonusCounter remains null

        return new SwnCustomCounter(cc, attackBonusCounter);
    }
}
