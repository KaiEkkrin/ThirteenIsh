using System.Globalization;
using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;

namespace ThirteenIsh.Tests.Game.Swn;

/// <summary>
/// Integration tests for the SWN fix system, verifying that fixes correctly modify
/// both base attributes and all derived values.
/// </summary>
public class SwnFixSystemIntegrationTests
{
    private readonly SwnSystem _gameSystem;
    private readonly SwnCharacterSystem _playerSystem;
    private readonly SwnCharacterSystem _monsterSystem;

    public SwnFixSystemIntegrationTests()
    {
        _gameSystem = SwnTestHelpers.CreateSwnSystem();
        _playerSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter, null);
        _monsterSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.Monster, null);
    }

    [Fact]
    public void PlayerCharacter_StrengthFix_AffectsPhysicalSavingThrow()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);
        SwnTestHelpers.SetupFullPlayerCharacter(character, _playerSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        var sheet = character.Sheet;

        // Get initial values - STR 14 (+1), CON 13 (+0), Level 3, so Physical Save = 15 - max(1, 0) - (3-1) = 12
        var initialPhysicalSave = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Physical).GetValue(adventurer);
        initialPhysicalSave.ShouldBe(12);

        // Act - Apply a +4 fix to Strength (14 + 4 = 18, which is +2 bonus instead of +1)
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.Strength, 4)); // +4 fix to the base attribute

        // Assert - Physical save should improve because STR bonus increased from +1 to +2
        var newPhysicalSave = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Physical).GetValue(adventurer);
        newPhysicalSave.ShouldBe(11); // 15 - max(2, 0) - (3-1) = 11
    }

    [Fact]
    public void PlayerCharacter_DexterityFix_AffectsArmorClassAndEvasionSave()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);
        SwnTestHelpers.SetupFullPlayerCharacter(character, _playerSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        var sheet = character.Sheet;

        // Get initial values - DEX 16 (+1), Armor Value 13, so AC = 13 + 1 = 14
        var initialAC = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.ArmorClass).GetValue(adventurer);
        initialAC.ShouldBe(14);

        // Evasion with DEX +1, INT +0, Level 3 = 15 - max(1, 0) - (3-1) = 12
        var initialEvasion = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Evasion).GetValue(adventurer);
        initialEvasion.ShouldBe(12);

        // Act - Apply a +2 fix to Dexterity (from 16 to 18, bonus from +1 to +2)
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.Dexterity, 2)); // +2 fix to the base attribute

        // Assert - Both AC and Evasion should improve
        var newAC = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.ArmorClass).GetValue(adventurer);
        newAC.ShouldBe(15); // 13 + 2 = 15

        var newEvasion = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Evasion).GetValue(adventurer);
        newEvasion.ShouldBe(11); // 15 - max(2, 0) - (3-1) = 11
    }

    [Fact]
    public void PlayerCharacter_ConstitutionFix_AffectsHitPointsAndPhysicalSave()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);
        SwnTestHelpers.SetupFullPlayerCharacter(character, _playerSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        var sheet = character.Sheet;

        // Get initial values - CON 13 (+0), Level 3, Expert/Warrior
        // HP = 6 + 7 + (0 + 2) * 3 = 19
        var initialHP = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.HitPoints).GetValue(adventurer);
        initialHP.ShouldBe(19);

        // Physical save with STR +1, CON +0, Level 3 = 15 - max(1, 0) - (3-1) = 12
        var initialPhysical = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Physical).GetValue(adventurer);
        initialPhysical.ShouldBe(12);

        // Act - Apply a +4 fix to Constitution (from 13 to 17, bonus from +0 to +1)
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.Constitution, 4)); // +4 fix to the base attribute

        // Assert - Both HP and Physical save should improve
        var newHP = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.HitPoints).GetValue(adventurer);
        newHP.ShouldBe(22); // 6 + 7 + (1 + 2) * 3 = 22

        var newPhysical = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Physical).GetValue(adventurer);
        newPhysical.ShouldBe(12); // 15 - max(1, 1) - (3-1) = 12 (no change since max is still 1)
    }

    [Fact]
    public void PlayerCharacter_IntelligenceFix_AffectsEvasionSave()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);

        var sheet = character.Sheet;

        // Set up character with low DEX and high INT for testing
        _playerSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Dexterity).EditCharacterProperty("10", character); // +0
        _playerSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Intelligence).EditCharacterProperty("16", character); // +1

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        // Initial Evasion = 15 - max(0, 1) = 14
        var initialEvasion = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Evasion).GetValue(adventurer);
        initialEvasion.ShouldBe(14);

        // Act - Apply a +2 fix to Intelligence (from 16 to 18, bonus from +1 to +2)
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.Intelligence, 2)); // +2 fix to the base attribute

        // Assert - Evasion should improve
        var newEvasion = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Evasion).GetValue(adventurer);
        newEvasion.ShouldBe(13); // 15 - max(0, 2) = 13
    }

    [Fact]
    public void PlayerCharacter_WisdomFix_AffectsMentalSave()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);

        var sheet = character.Sheet;

        // Set up character with average WIS and low CHA
        _playerSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("12", character); // +0
        _playerSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Charisma).EditCharacterProperty("8", character); // -1

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        // Initial Mental save = 15 - max(0, -1) = 15
        var initialMental = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Mental).GetValue(adventurer);
        initialMental.ShouldBe(15);

        // Act - Apply a +2 fix to Wisdom (from 12 to 14, bonus from +0 to +1)
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.Wisdom, 2)); // +2 fix to the base attribute

        // Assert - Mental save should improve
        var newMental = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Mental).GetValue(adventurer);
        newMental.ShouldBe(14); // 15 - max(1, -1) = 14
    }

    [Fact]
    public void PlayerCharacter_AttackBonusFix_AffectsAttackBonusDirectly()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);
        SwnTestHelpers.SetupFullPlayerCharacter(character, _playerSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        var sheet = character.Sheet;

        // Initial Attack Bonus for Expert/Warrior level 3 = 2
        var initialAttackBonus = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.AttackBonus).GetValue(adventurer);
        initialAttackBonus.ShouldBe(2);

        // Act - Apply a +2 fix to Attack Bonus
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.AttackBonus, 2));

        // Assert - Attack Bonus should increase
        var newAttackBonus = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.AttackBonus).GetValue(adventurer);
        newAttackBonus.ShouldBe(4); // 2 + 2 = 4
    }

    [Fact]
    public void Monster_HitPointsFix_AffectsMonsterHitPoints()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = monster.Sheet;

        var sheet = monster.Sheet;

        // Initial HP for 4 HD monster = 18
        var initialHP = _monsterSystem.GetProperty<GameCounter>(monsterCombatant, SwnSystem.HitPoints).GetValue(monsterCombatant);
        initialHP.ShouldBe(18);

        // Act - Apply a +5 fix to Hit Points
        monsterCombatant.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.HitPoints, 5));

        // Assert - Hit Points should increase
        var newHP = _monsterSystem.GetProperty<GameCounter>(monsterCombatant, SwnSystem.HitPoints).GetValue(monsterCombatant);
        newHP.ShouldBe(23); // 18 + 5 = 23
    }

    [Fact]
    public void Monster_ArmorClassFix_AffectsMonsterAC()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = monster.Sheet;

        var sheet = monster.Sheet;

        // Initial AC = 15
        var initialAC = _monsterSystem.GetProperty<GameCounter>(monsterCombatant, SwnSystem.ArmorClass).GetValue(monsterCombatant);
        initialAC.ShouldBe(15);

        // Act - Apply a -2 fix to Armor Class (making the monster easier to hit)
        monsterCombatant.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.ArmorClass, -2));

        // Assert - AC should decrease
        var newAC = _monsterSystem.GetProperty<GameCounter>(monsterCombatant, SwnSystem.ArmorClass).GetValue(monsterCombatant);
        newAC.ShouldBe(13); // 15 - 2 = 13
    }

    [Fact]
    public void PlayerCharacter_MultipleFixes_AllAppliedCorrectly()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);
        SwnTestHelpers.SetupFullPlayerCharacter(character, _playerSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        var sheet = character.Sheet;

        // Get initial values
        var initialAC = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.ArmorClass).GetValue(adventurer);
        var initialHP = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.HitPoints).GetValue(adventurer);
        var initialAttackBonus = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.AttackBonus).GetValue(adventurer);

        // Act - Apply multiple fixes
        var fixes = adventurer.GetFixes();
        fixes.Counters.Add(new PropertyValue<int>(SwnSystem.Dexterity, 2)); // +2 DEX (16->18, +1->+2 bonus)
        fixes.Counters.Add(new PropertyValue<int>(SwnSystem.Constitution, 4)); // +4 CON (13->17, +0->+1 bonus)
        fixes.Counters.Add(new PropertyValue<int>(SwnSystem.AttackBonus, 2)); // +2 Attack Bonus

        // Assert - All derived values should be affected correctly
        var newAC = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.ArmorClass).GetValue(adventurer);
        newAC.ShouldBe(initialAC + 1); // AC increases by 1 due to DEX bonus

        var newHP = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.HitPoints).GetValue(adventurer);
        newHP.ShouldBe(initialHP + 3); // HP increases by 3 (CON bonus +1 * level 3)

        var newAttackBonus = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.AttackBonus).GetValue(adventurer);
        newAttackBonus.ShouldBe(initialAttackBonus + 2); // Attack Bonus increases by 2
    }

    [Fact]
    public void PlayerCharacter_NegativeFixes_CanReduceValues()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);
        SwnTestHelpers.SetupFullPlayerCharacter(character, _playerSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        var sheet = character.Sheet;

        var initialAC = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.ArmorClass).GetValue(adventurer);
        var initialAttackBonus = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.AttackBonus).GetValue(adventurer);

        // Act - Apply negative fixes
        var fixes = adventurer.GetFixes();
        fixes.Counters.Add(new PropertyValue<int>(SwnSystem.Dexterity, -9)); // -9 DEX (16->7, +1->-1 bonus, AC drops by 2)
        fixes.Counters.Add(new PropertyValue<int>(SwnSystem.AttackBonus, -1)); // -1 Attack Bonus

        // Assert - Values should be reduced
        var newAC = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.ArmorClass).GetValue(adventurer);
        newAC.ShouldBe(initialAC - 2); // AC decreases by 2

        var newAttackBonus = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.AttackBonus).GetValue(adventurer);
        newAttackBonus.ShouldBe(initialAttackBonus - 1); // Attack Bonus decreases by 1
    }

    [Fact]
    public void AttributeBonus_IsVisibleInCharacterSheet()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);
        SwnTestHelpers.SetupFullPlayerCharacter(character, _playerSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        // Act - Get the Strength Bonus counter
        var strengthBonusCounter = _playerSystem.GetProperty<GameCounter>(adventurer, "Strength Bonus");

        // Assert - Verify it exists and is not hidden
        strengthBonusCounter.ShouldNotBeNull();
        strengthBonusCounter.Options.HasFlag(GameCounterOptions.IsHidden).ShouldBeFalse();
    }

    [Fact]
    public void AttributeBonus_CanBeFixed_DirectlyOnBonus()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);

        // Set Strength to 14 (bonus +1)
        _playerSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Strength).EditCharacterProperty("14", character);
        _playerSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("14", character);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        // Verify initial bonus
        var initialStrengthBonus = _playerSystem.GetProperty<GameCounter>(adventurer, "Strength Bonus").GetValue(adventurer);
        initialStrengthBonus.ShouldBe(1); // STR 14 = +1 bonus

        // Verify initial Physical save
        var initialPhysicalSave = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Physical).GetValue(adventurer);
        initialPhysicalSave.ShouldBe(14); // 15 - max(1, 1) = 14

        // Act - Apply fix directly to Strength Bonus
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("Strength Bonus", 1));

        // Assert - Strength Bonus should now be 2 (base 1 + fix 1)
        var newStrengthBonus = _playerSystem.GetProperty<GameCounter>(adventurer, "Strength Bonus").GetValue(adventurer);
        newStrengthBonus.ShouldBe(2);

        // Physical save should improve (uses the higher bonus)
        var newPhysicalSave = _playerSystem.GetProperty<GameCounter>(adventurer, SwnSystem.Physical).GetValue(adventurer);
        newPhysicalSave.ShouldBe(13); // 15 - max(2, 1) = 13
    }

    [Fact]
    public void AttributeBonus_FixesAndAttributeFixes_StackCorrectly()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);

        // Set Strength to 14 (bonus +1)
        _playerSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Strength).EditCharacterProperty("14", character);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = character.Sheet;

        // Verify initial bonus
        var initialStrengthBonus = _playerSystem.GetProperty<GameCounter>(adventurer, "Strength Bonus").GetValue(adventurer);
        initialStrengthBonus.ShouldBe(1); // STR 14 = +1 bonus

        // Act - Apply fix to Strength attribute (changes base bonus)
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.Strength, 4)); // STR 14 -> 18 (bonus +1 -> +2)

        // Verify the attribute fix changed the bonus
        var bonusAfterAttributeFix = _playerSystem.GetProperty<GameCounter>(adventurer, "Strength Bonus").GetValue(adventurer);
        bonusAfterAttributeFix.ShouldBe(2);

        // Apply additional fix directly to Strength Bonus
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("Strength Bonus", 1));

        // Assert - Final bonus should be 3 (base 2 from STR 18, + 1 from bonus fix)
        var finalStrengthBonus = _playerSystem.GetProperty<GameCounter>(adventurer, "Strength Bonus").GetValue(adventurer);
        finalStrengthBonus.ShouldBe(3);
    }

    [Fact]
    public void AttributeBonus_RemainsCalculated_EvenIfEdited()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(character);

        // Set Strength to 14 (bonus +1)
        _playerSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Strength).EditCharacterProperty("14", character);

        var strengthBonusCounter = _playerSystem.GetProperty<GameCounter>(character, "Strength Bonus");

        // Assert - Verify that CanStore is false (value is calculated, not stored)
        strengthBonusCounter.CanStore.ShouldBeFalse();

        // Verify initial bonus is calculated correctly
        var initialBonus = strengthBonusCounter.GetValue(character);
        initialBonus.ShouldBe(1); // STR 14 = +1 bonus

        // Even if we "edit" the bonus to a different value, it should still be calculated
        // (TryEditCharacterProperty may succeed but the edit is ignored for calculated properties)
        strengthBonusCounter.TryEditCharacterProperty("5", character, out var errorMessage);

        // The bonus should still be calculated from the attribute, not the edited value
        var bonusAfterEdit = strengthBonusCounter.GetValue(character);
        bonusAfterEdit.ShouldBe(1); // Still +1, not 5

        // But fixes should still work (tested in other tests)
    }
}