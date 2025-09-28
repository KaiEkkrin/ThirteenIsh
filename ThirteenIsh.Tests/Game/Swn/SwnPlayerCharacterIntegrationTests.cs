using System.Globalization;
using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;

namespace ThirteenIsh.Tests.Game.Swn;

/// <summary>
/// Integration tests for SWN player character creation and computed values.
/// Tests the full character system rather than individual methods.
/// </summary>
public class SwnPlayerCharacterIntegrationTests
{
    private readonly SwnSystem _gameSystem;
    private readonly SwnCharacterSystem _characterSystem;

    public SwnPlayerCharacterIntegrationTests()
    {
        _gameSystem = SwnTestHelpers.CreateSwnSystem();
        _characterSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter, null);
    }

    [Fact]
    public void PlayerCharacter_FullCharacterCreation_AllComputedValuesCorrect()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        SwnTestHelpers.SetupFullPlayerCharacter(character, _characterSystem);

        var sheet = character.Sheet;

        // Act & Assert - Verify base attributes
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Strength).GetValue(character).ShouldBe(14);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Dexterity).GetValue(character).ShouldBe(16);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).GetValue(character).ShouldBe(13);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Intelligence).GetValue(character).ShouldBe(12);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).GetValue(character).ShouldBe(15);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Charisma).GetValue(character).ShouldBe(10);

        // Verify attribute bonuses
        _characterSystem.GetProperty<GameCounter>(character, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Strength)).GetValue(character).ShouldBe(1); // 14 = +1
        _characterSystem.GetProperty<GameCounter>(character, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity)).GetValue(character).ShouldBe(1); // 16 = +1
        _characterSystem.GetProperty<GameCounter>(character, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Constitution)).GetValue(character).ShouldBe(0); // 13 = +0
        _characterSystem.GetProperty<GameCounter>(character, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Intelligence)).GetValue(character).ShouldBe(0); // 12 = +0
        _characterSystem.GetProperty<GameCounter>(character, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Wisdom)).GetValue(character).ShouldBe(1); // 15 = +1
        _characterSystem.GetProperty<GameCounter>(character, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Charisma)).GetValue(character).ShouldBe(0); // 10 = +0

        // Verify level and classes
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Level).GetValue(character).ShouldBe(3);
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").GetValue(character).ShouldBe(SwnSystem.Expert);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").GetValue(character).ShouldBe(SwnSystem.Warrior);

        // Verify Attack Bonus (Expert/Warrior at level 3 = 3/2 + warrior_bonus = 1 + 1 = 2)
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.AttackBonus).GetValue(character).ShouldBe(2);

        // Verify Hit Points (6 + (3-1)*3.5 + (con_bonus + warrior_bonus) * level = 6 + 7 + (0 + 2) * 3 = 19)
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.HitPoints).GetValue(character).ShouldBe(19);

        // Verify Armor Class (Armor Value 13 + Dex bonus 1 = 14)
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.ArmorClass).GetValue(character).ShouldBe(14);

        // Verify Saving Throws
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Evasion).GetValue(character).ShouldBe(15 - Math.Max(1, 1)); // 15 - max(dex_bonus, int_bonus) = 14
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Mental).GetValue(character).ShouldBe(15 - Math.Max(1, 0)); // 15 - max(wis_bonus, cha_bonus) = 14
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Physical).GetValue(character).ShouldBe(15 - Math.Max(1, 0)); // 15 - max(str_bonus, con_bonus) = 14
    }

    [Theory]
    [InlineData(SwnSystem.Expert, SwnSystem.Expert, 3, 1)] // Full Expert = level/2 = 1
    [InlineData(SwnSystem.Warrior, SwnSystem.Warrior, 3, 3)] // Full Warrior = level = 3
    [InlineData(SwnSystem.Expert, SwnSystem.Warrior, 3, 2)] // Expert/Warrior = level/2 + warrior_bonus = 1 + 1 = 2
    [InlineData(SwnSystem.Psychic, SwnSystem.Psychic, 5, 2)] // Full Psychic = level/2 = 2
    [InlineData(SwnSystem.Warrior, SwnSystem.Psychic, 5, 4)] // Warrior/Psychic = level/2 + warrior_bonus = 2 + 2 = 4
    public void PlayerCharacter_DifferentClassCombinations_AttackBonusCalculatedCorrectly(
        string class1, string class2, int level, int expectedAttackBonus)
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Act
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Level).EditCharacterProperty(level.ToString(CultureInfo.InvariantCulture), character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(class1, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(class2, character);

        // Assert
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.AttackBonus).GetValue(character).ShouldBe(expectedAttackBonus);
    }

    [Theory]
    [InlineData(1, 1)] // Level 1: 1/2 + 1 = 0 + 1 = 1
    [InlineData(2, 2)] // Level 2: 2/2 + 1 = 1 + 1 = 2
    [InlineData(3, 2)] // Level 3: 3/2 + 1 = 1 + 1 = 2
    [InlineData(4, 3)] // Level 4: 4/2 + 1 = 2 + 1 = 3
    [InlineData(5, 4)] // Level 5: 5/2 + 2 = 2 + 2 = 4
    [InlineData(6, 5)] // Level 6: 6/2 + 2 = 3 + 2 = 5
    [InlineData(7, 5)] // Level 7: 7/2 + 2 = 3 + 2 = 5
    [InlineData(8, 6)] // Level 8: 8/2 + 2 = 4 + 2 = 6
    [InlineData(9, 6)] // Level 9: 9/2 + 2 = 4 + 2 = 6
    [InlineData(10, 7)] // Level 10: 10/2 + 2 = 5 + 2 = 7
    public void PlayerCharacter_PartialWarrior_AttackBonusProgressionCorrect(int level, int expectedAttackBonus)
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Act - Set up Expert/Warrior (partial warrior)
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Level).EditCharacterProperty(level.ToString(CultureInfo.InvariantCulture), character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Expert, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Warrior, character);

        // Assert
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.AttackBonus).GetValue(character).ShouldBe(expectedAttackBonus);
    }

    [Theory]
    [InlineData(1, SwnSystem.Expert, SwnSystem.Expert, 14, 7)] // Level 1 Expert/Expert with CON 14: 6 + 0 + (1 + 0) * 1 = 7
    [InlineData(1, SwnSystem.Warrior, SwnSystem.Warrior, 14, 9)] // Level 1 Warrior/Warrior with CON 14: 6 + 0 + (1 + 2) * 1 = 9
    [InlineData(3, SwnSystem.Expert, SwnSystem.Warrior, 16, 22)] // Level 3 Expert/Warrior with CON 16: 6 + 7 + (1 + 2) * 3 = 22
    [InlineData(5, SwnSystem.Psychic, SwnSystem.Psychic, 10, 20)] // Level 5 Psychic/Psychic with CON 10: 6 + 14 + (0 + 0) * 5 = 20
    public void PlayerCharacter_DifferentLevelsAndClasses_HitPointsCalculatedCorrectly(
        int level, string class1, string class2, int constitution, int expectedHitPoints)
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Act
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Level).EditCharacterProperty(level.ToString(CultureInfo.InvariantCulture), character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(class1, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(class2, character);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty(constitution.ToString(CultureInfo.InvariantCulture), character);

        // Assert
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.HitPoints).GetValue(character).ShouldBe(expectedHitPoints);
    }

    [Theory]
    [InlineData(3, -2)] // STR 3 = -2 bonus
    [InlineData(7, -1)] // STR 7 = -1 bonus
    [InlineData(10, 0)]  // STR 10 = +0 bonus
    [InlineData(14, 1)]  // STR 14 = +1 bonus
    [InlineData(18, 2)]  // STR 18 = +2 bonus
    public void PlayerCharacter_AttributeBonuses_CalculatedCorrectlyForAllRanges(int attributeValue, int expectedBonus)
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Act
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Strength).EditCharacterProperty(attributeValue.ToString(CultureInfo.InvariantCulture), character);

        // Assert
        _characterSystem.GetProperty<GameCounter>(character, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Strength))
            .GetValue(character).ShouldBe(expectedBonus);
    }

    [Fact]
    public void PlayerCharacter_ArmorClass_CalculatedFromArmorValueAndDexterity()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set Dexterity to 16 (+1 bonus) and Armor Value to 15
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Dexterity).EditCharacterProperty("16", character);
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.ArmorValue).EditCharacterProperty("15", character);

        // Act & Assert
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.ArmorClass).GetValue(character).ShouldBe(16); // 15 + 1
    }

    [Fact]
    public void PlayerCharacter_SavingThrows_CalculatedFromAttributeBonuses()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set various attributes for different bonuses
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Strength).EditCharacterProperty("16", character); // +1
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Dexterity).EditCharacterProperty("14", character); // +1
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("12", character); // +0
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Intelligence).EditCharacterProperty("18", character); // +2
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("10", character); // +0
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Charisma).EditCharacterProperty("8", character); // -1

        // Act & Assert
        // Evasion = 15 - max(dex_bonus, int_bonus) = 15 - max(1, 2) = 13
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Evasion).GetValue(character).ShouldBe(13);

        // Mental = 15 - max(wis_bonus, cha_bonus) = 15 - max(0, -1) = 15
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Mental).GetValue(character).ShouldBe(15);

        // Physical = 15 - max(str_bonus, con_bonus) = 15 - max(1, 0) = 14
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Physical).GetValue(character).ShouldBe(14);
    }

    [Fact]
    public void PlayerCharacter_CharacterSummary_DisplaysCorrectClassAndLevel()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Level).EditCharacterProperty("5", character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Expert, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Warrior, character);

        // Act
        var summary = _gameSystem.GetCharacterSummary(character);

        // Assert
        summary.ShouldBe("Level 5 Expert/Warrior");
    }

    [Theory]
    [InlineData(SwnSystem.Expert, SwnSystem.Expert, "Level 3 Expert")]
    [InlineData(SwnSystem.Warrior, SwnSystem.Warrior, "Level 3 Warrior")]
    [InlineData(SwnSystem.Psychic, SwnSystem.Psychic, "Level 3 Psychic")]
    [InlineData(SwnSystem.Expert, "", "Level 3 Partial Expert")]
    [InlineData("", SwnSystem.Warrior, "Level 3 Partial Warrior")]
    [InlineData("", "", "Level 3 Adventurer")]
    public void PlayerCharacter_CharacterSummary_HandlesAllClassCombinations(string class1, string class2, string expectedSummary)
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Level).EditCharacterProperty("3", character);
        if (!string.IsNullOrEmpty(class1))
            _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(class1, character);
        if (!string.IsNullOrEmpty(class2))
            _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(class2, character);

        // Act
        var summary = _gameSystem.GetCharacterSummary(character);

        // Assert
        summary.ShouldBe(expectedSummary);
    }

    [Fact]
    public void PlayerCharacter_NonPsychicCharacter_HasNoEffortValue()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set up a non-psychic character (Expert/Warrior with no psychic skills)
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Expert, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Warrior, character);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("16", character); // +1 bonus
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("14", character); // +1 bonus

        // Don't set any psychic skills (they default to -1, which means no psychic training)

        // Act & Assert - Effort should be null for non-psychic characters
        var effortCounter = _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Effort);
        effortCounter.GetValue(character).ShouldBeNull();

        // Test with an adventurer too
        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;
        effortCounter.GetValue(adventurer).ShouldBeNull();
    }

    [Fact]
    public void PlayerCharacter_PartialPsychicCharacter_HasBasicEffortCalculation()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set up a non-psychic character with some psychic training (Expert/Warrior with psychic skills)
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Expert, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Warrior, character);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("16", character); // +1 bonus
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("14", character); // +1 bonus

        // Set psychic skills (non-psychic class gets no attribute bonus)
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Telepathy).EditCharacterProperty("1", character);
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Telekinesis).EditCharacterProperty("0", character);

        // Act & Assert - Effort = 1 (base) + 0 (no class bonus) + 1 (highest psychic skill) = 2
        var effortCounter = _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Effort);
        effortCounter.GetValue(character).ShouldBe(2);

        // Test with an adventurer too
        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;
        effortCounter.GetValue(adventurer).ShouldBe(2);
    }

    [Fact]
    public void PlayerCharacter_FullPsychicCharacter_HasAttributeBonusInEffortCalculation()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set up a full psychic character
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Psychic, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Psychic, character);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("16", character); // +1 bonus
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("18", character); // +2 bonus (higher)

        // Set psychic skills
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Telepathy).EditCharacterProperty("2", character);
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Metapsionics).EditCharacterProperty("1", character);

        // Act & Assert - Effort = 1 (base) + 2 (max of Con/Wis bonus for psychic class) + 2 (highest psychic skill) = 5
        var effortCounter = _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Effort);
        effortCounter.GetValue(character).ShouldBe(5);
    }

    [Fact]
    public void PlayerCharacter_PartialPsychicClass_HasAttributeBonusInEffortCalculation()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set up a partial psychic character (Expert/Psychic)
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Expert, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Psychic, character);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("14", character); // +1 bonus
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("16", character); // +1 bonus

        // Set psychic skills
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Biopsionics).EditCharacterProperty("1", character);

        // Act & Assert - Effort = 1 (base) + 1 (max of Con/Wis bonus for partial psychic) + 1 (highest psychic skill) = 3
        var effortCounter = _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Effort);
        effortCounter.GetValue(character).ShouldBe(3);
    }

    [Fact]
    public void PlayerCharacter_EffortVariable_StartingValueEqualsMaxValue()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set up a psychic character with known Effort value
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Psychic, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Expert, character);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("14", character); // +1 bonus
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("16", character); // +1 bonus
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Telepathy).EditCharacterProperty("2", character);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Act & Assert - Effort = 1 (base) + 1 (max attribute bonus) + 2 (highest psychic skill) = 4
        var effortCounter = _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Effort);
        var maxEffort = effortCounter.GetMaxVariableValue(adventurer);
        var startingEffort = effortCounter.GetStartingValue(adventurer);
        var currentEffort = effortCounter.GetVariableValue(adventurer);

        maxEffort.ShouldBe(4);
        startingEffort.ShouldBe(4);
        currentEffort.ShouldBe(4); // Should start at max
    }

    [Fact]
    public void PlayerCharacter_EffortVariable_CanSetAndModifyValue()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set up a psychic character with Effort = 3
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Expert, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Psychic, character);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("12", character); // +0 bonus
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("14", character); // +1 bonus
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Telekinesis).EditCharacterProperty("1", character);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        var effortCounter = _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Effort);

        // Verify initial state: Effort = 1 (base) + 1 (max attribute bonus) + 1 (psychic skill) = 3
        effortCounter.GetMaxVariableValue(adventurer).ShouldBe(3);
        effortCounter.GetVariableValue(adventurer).ShouldBe(3);

        // Act - Set current Effort to 1 (spending 2 effort)
        effortCounter.SetVariableClamped(1, adventurer);

        // Assert
        effortCounter.GetVariableValue(adventurer).ShouldBe(1);
        effortCounter.GetMaxVariableValue(adventurer).ShouldBe(3); // Max should remain unchanged

        // Act - Set current Effort to 0 (spending all effort)
        effortCounter.SetVariableClamped(0, adventurer);

        // Assert
        effortCounter.GetVariableValue(adventurer).ShouldBe(0);
        effortCounter.GetMaxVariableValue(adventurer).ShouldBe(3); // Max should remain unchanged
    }

    [Fact]
    public void PlayerCharacter_EffortVariable_ClampsToValidRange()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set up a psychic character with Effort = 2
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Expert, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Psychic, character);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("10", character); // +0 bonus
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("10", character); // +0 bonus
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Biopsionics).EditCharacterProperty("1", character);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        var effortCounter = _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Effort);

        // Verify Effort = 1 (base) + 0 (no attribute bonus) + 1 (psychic skill) = 2
        effortCounter.GetMaxVariableValue(adventurer).ShouldBe(2);

        // Act - Try to set below minimum (negative values should clamp to 0)
        effortCounter.SetVariableClamped(-5, adventurer);
        var clampedLow = effortCounter.GetVariableValue(adventurer);

        // Act - Try to set above maximum
        effortCounter.SetVariableClamped(10, adventurer);
        var clampedHigh = effortCounter.GetVariableValue(adventurer);

        // Assert
        clampedLow.ShouldBe(0); // Clamped to minimum (0)
        clampedHigh.ShouldBe(2); // Clamped to maximum (2)
    }

    [Fact]
    public void PlayerCharacter_EffortVariable_WithFixes_AffectsMaxAndStartingValue()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set up a psychic character with base Effort = 2
        _characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Expert, character);
        _characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Psychic, character);
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("10", character); // +0 bonus
        _characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("10", character); // +0 bonus
        _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Precognition).EditCharacterProperty("1", character);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Add a fix of +2 to Effort
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.Effort, 2));

        var effortCounter = _characterSystem.GetProperty<GameCounter>(character, SwnSystem.Effort);

        // Act & Assert - Check max and starting values with fix
        var maxValue = effortCounter.GetMaxVariableValue(adventurer);
        var startingValue = effortCounter.GetStartingValue(adventurer);
        var currentValue = effortCounter.GetVariableValue(adventurer);

        // Base Effort = 1 (base) + 0 (no attribute bonus) + 1 (psychic skill) = 2
        // With fix: 2 + 2 = 4
        maxValue.ShouldBe(4);
        startingValue.ShouldBe(4);
        currentValue.ShouldBe(4); // Should start at starting value
    }
}