

namespace ThirteenIsh.Game.Swn;

internal class SwnCharacterSystem(string name, CharacterTypeCompatibility compatibility, CharacterType? defaultForType,
    ImmutableList<GamePropertyGroup> propertyGroups)
    : CharacterSystem(name, SwnSystem.SystemName, compatibility, defaultForType, propertyGroups)
{
    public override void SetNewCharacterStartingValues(Character character)
    {
        if (character.CharacterType == CharacterType.PlayerCharacter)
        {
            // Character level begins at 1
            var level = GetProperty<GameCounter>(character, SwnSystem.Level);
            level.EditCharacterProperty("1", character);

            // Armor value begins at 10
            var armorValue = GetProperty<GameCounter>(character, SwnSystem.ArmorValue);
            armorValue.EditCharacterProperty("10", character);
        }
        else if (character.CharacterType == CharacterType.Monster)
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

    protected override GameCounter BuildCustomCounter(CustomCounter cc)
    {
        AttackBonusCounter? attackBonusCounter = null;
        if (Compatibility.HasFlag(CharacterTypeCompatibility.PlayerCharacter))
        {
            // Get the attack bonus counter by its well-known name for PCs
            attackBonusCounter = (AttackBonusCounter)GetProperty(SwnSystem.AttackBonus)!;
        }
        // For monsters, attackBonusCounter remains null

        return new SwnCustomCounter(cc, attackBonusCounter);
    }
}
