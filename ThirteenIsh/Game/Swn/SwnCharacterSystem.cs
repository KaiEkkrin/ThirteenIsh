using ThirteenIsh.Database.Entities;

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
            if (!level.TryEditCharacterProperty("1", character.Sheet, out var errorMessage))
            {
                throw new InvalidOperationException($"Failed to set character level to 1 : {errorMessage}");
            }

            // Armor value begins at 10
            var armorValue = GetProperty<GameCounter>(character.Sheet, SwnSystem.ArmorValue);
            if (!armorValue.TryEditCharacterProperty("10", character.Sheet, out errorMessage))
            {
                throw new InvalidOperationException($"Failed to set armor value to 10 : {errorMessage}");
            }
        }
        else if (CharacterType == CharacterType.Monster)
        {
            // Hit Dice begins at 1 for monsters
            var hitDice = GetProperty<GameCounter>(character.Sheet, SwnSystem.HitDice);
            if (!hitDice.TryEditCharacterProperty("1", character.Sheet, out var errorMessage))
            {
                throw new InvalidOperationException($"Failed to set hit dice to 1 : {errorMessage}");
            }

            // Monster AC begins at 10
            var armorClass = GetProperty<GameCounter>(character.Sheet, SwnSystem.ArmorClass);
            if (!armorClass.TryEditCharacterProperty("10", character.Sheet, out errorMessage))
            {
                throw new InvalidOperationException($"Failed to set armor class to 10 : {errorMessage}");
            }

            // Morale begins at 7 (typical value)
            var morale = GetProperty<GameCounter>(character.Sheet, "Morale");
            if (!morale.TryEditCharacterProperty("7", character.Sheet, out errorMessage))
            {
                throw new InvalidOperationException($"Failed to set morale to 7 : {errorMessage}");
            }
        }
    }

    protected override GameCounter BuildCustomCounter(CustomCounter cc)
    {
        if (CharacterType == CharacterType.PlayerCharacter)
        {
            // Get the attack bonus counter by its well-known name
            var attackBonusCounter = (AttackBonusCounter)GetProperty(SwnSystem.AttackBonus)!;
            return new SwnCustomCounter(cc, attackBonusCounter);
        }
        else
        {
            // Monsters use a simpler custom counter without AttackBonusCounter
            return new SkillCounter(cc.Name, null, cc.DefaultValue, cc.Options);
        }
    }
}
