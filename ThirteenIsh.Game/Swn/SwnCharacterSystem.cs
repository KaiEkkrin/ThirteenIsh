namespace ThirteenIsh.Game.Swn;

internal abstract class SwnCharacterSystem(string name, CharacterTypeCompatibility compatibility,
    CharacterType? defaultForType, ImmutableList<GamePropertyGroup> propertyGroups)
    : CharacterSystem(name, SwnSystem.SystemName, compatibility, defaultForType, propertyGroups)
{
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