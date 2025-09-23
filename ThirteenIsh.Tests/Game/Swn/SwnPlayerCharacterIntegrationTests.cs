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
        _characterSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
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
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Strength).GetValue(sheet).ShouldBe(14);
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).GetValue(sheet).ShouldBe(16);
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Constitution).GetValue(sheet).ShouldBe(13);
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Intelligence).GetValue(sheet).ShouldBe(12);
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Wisdom).GetValue(sheet).ShouldBe(15);
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Charisma).GetValue(sheet).ShouldBe(10);

        // Verify attribute bonuses
        _characterSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Strength)).GetValue(sheet).ShouldBe(1); // 14 = +1
        _characterSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity)).GetValue(sheet).ShouldBe(1); // 16 = +1
        _characterSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Constitution)).GetValue(sheet).ShouldBe(0); // 13 = +0
        _characterSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Intelligence)).GetValue(sheet).ShouldBe(0); // 12 = +0
        _characterSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Wisdom)).GetValue(sheet).ShouldBe(1); // 15 = +1
        _characterSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Charisma)).GetValue(sheet).ShouldBe(0); // 10 = +0

        // Verify level and classes
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).GetValue(sheet).ShouldBe(3);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 1").GetValue(sheet).ShouldBe(SwnSystem.Expert);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 2").GetValue(sheet).ShouldBe(SwnSystem.Warrior);

        // Verify Attack Bonus (Expert/Warrior at level 3 = 3/2 + warrior_bonus = 1 + 1 = 2)
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.AttackBonus).GetValue(sheet).ShouldBe(2);

        // Verify Hit Points (6 + (3-1)*3.5 + (con_bonus + warrior_bonus) * level = 6 + 7 + (0 + 2) * 3 = 19)
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitPoints).GetValue(sheet).ShouldBe(19);

        // Verify Armor Class (Armor Value 13 + Dex bonus 1 = 14)
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.ArmorClass).GetValue(sheet).ShouldBe(14);

        // Verify Saving Throws
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Evasion).GetValue(sheet).ShouldBe(15 - Math.Max(1, 1)); // 15 - max(dex_bonus, int_bonus) = 14
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Mental).GetValue(sheet).ShouldBe(15 - Math.Max(1, 0)); // 15 - max(wis_bonus, cha_bonus) = 14
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Physical).GetValue(sheet).ShouldBe(15 - Math.Max(1, 0)); // 15 - max(str_bonus, con_bonus) = 14
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
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty(level.ToString(CultureInfo.InvariantCulture), sheet);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(class1, sheet);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 2").EditCharacterProperty(class2, sheet);

        // Assert
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.AttackBonus).GetValue(sheet).ShouldBe(expectedAttackBonus);
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
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty(level.ToString(CultureInfo.InvariantCulture), sheet);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(SwnSystem.Expert, sheet);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 2").EditCharacterProperty(SwnSystem.Warrior, sheet);

        // Assert
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.AttackBonus).GetValue(sheet).ShouldBe(expectedAttackBonus);
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
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty(level.ToString(CultureInfo.InvariantCulture), sheet);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(class1, sheet);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 2").EditCharacterProperty(class2, sheet);
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Constitution).EditCharacterProperty(constitution.ToString(CultureInfo.InvariantCulture), sheet);

        // Assert
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitPoints).GetValue(sheet).ShouldBe(expectedHitPoints);
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
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Strength).EditCharacterProperty(attributeValue.ToString(CultureInfo.InvariantCulture), sheet);

        // Assert
        _characterSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Strength))
            .GetValue(sheet).ShouldBe(expectedBonus);
    }

    [Fact]
    public void PlayerCharacter_ArmorClass_CalculatedFromArmorValueAndDexterity()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set Dexterity to 16 (+1 bonus) and Armor Value to 15
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("16", sheet);
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.ArmorValue).EditCharacterProperty("15", sheet);

        // Act & Assert
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.ArmorClass).GetValue(sheet).ShouldBe(16); // 15 + 1
    }

    [Fact]
    public void PlayerCharacter_SavingThrows_CalculatedFromAttributeBonuses()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        // Set various attributes for different bonuses
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Strength).EditCharacterProperty("16", sheet); // +1
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("14", sheet); // +1
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Constitution).EditCharacterProperty("12", sheet); // +0
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Intelligence).EditCharacterProperty("18", sheet); // +2
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Wisdom).EditCharacterProperty("10", sheet); // +0
        _characterSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Charisma).EditCharacterProperty("8", sheet); // -1

        // Act & Assert
        // Evasion = 15 - max(dex_bonus, int_bonus) = 15 - max(1, 2) = 13
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Evasion).GetValue(sheet).ShouldBe(13);

        // Mental = 15 - max(wis_bonus, cha_bonus) = 15 - max(0, -1) = 15
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Mental).GetValue(sheet).ShouldBe(15);

        // Physical = 15 - max(str_bonus, con_bonus) = 15 - max(1, 0) = 14
        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Physical).GetValue(sheet).ShouldBe(14);
    }

    [Fact]
    public void PlayerCharacter_CharacterSummary_DisplaysCorrectClassAndLevel()
    {
        // Arrange
        var character = SwnTestHelpers.CreatePlayerCharacter();
        _characterSystem.SetNewCharacterStartingValues(character);
        var sheet = character.Sheet;

        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty("5", sheet);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(SwnSystem.Expert, sheet);
        _characterSystem.GetProperty<GameProperty>(sheet, "Class 2").EditCharacterProperty(SwnSystem.Warrior, sheet);

        // Act
        var summary = _gameSystem.GetCharacterSummary(sheet, CharacterType.PlayerCharacter);

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

        _characterSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty("3", sheet);
        if (!string.IsNullOrEmpty(class1))
            _characterSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(class1, sheet);
        if (!string.IsNullOrEmpty(class2))
            _characterSystem.GetProperty<GameProperty>(sheet, "Class 2").EditCharacterProperty(class2, sheet);

        // Act
        var summary = _gameSystem.GetCharacterSummary(sheet, CharacterType.PlayerCharacter);

        // Assert
        summary.ShouldBe(expectedSummary);
    }
}