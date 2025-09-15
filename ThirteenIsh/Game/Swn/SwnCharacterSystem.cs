using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Game.Swn;

internal class SwnCharacterSystem(ImmutableList<GamePropertyGroup> propertyGroups)
    : CharacterSystem(CharacterType.PlayerCharacter, SwnSystem.SystemName, propertyGroups)
{
    public override void SetNewCharacterStartingValues(Character character)
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

    protected override GameCounter BuildCustomCounter(CustomCounter cc)
    {
        // Get the attack bonus counter by its well-known name
        var attackBonusCounter = (AttackBonusCounter)GetProperty(SwnSystem.AttackBonus)!;
        return new SwnCustomCounter(cc, attackBonusCounter);
    }
}
