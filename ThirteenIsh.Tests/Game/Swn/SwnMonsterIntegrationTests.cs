using System.Globalization;
using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;

namespace ThirteenIsh.Tests.Game.Swn;

/// <summary>
/// Integration tests for SWN monster character creation and computed values.
/// Tests the full monster system rather than individual methods.
/// </summary>
public class SwnMonsterIntegrationTests
{
    private readonly SwnSystem _gameSystem;
    private readonly SwnCharacterSystem _monsterSystem;

    public SwnMonsterIntegrationTests()
    {
        _gameSystem = SwnTestHelpers.CreateSwnSystem();
        _monsterSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.Monster);
    }

    [Fact]
    public void Monster_FullMonsterCreation_AllValuesSetCorrectly()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);

        var sheet = monster.Sheet;

        // Act & Assert - Verify all monster stats are set correctly
        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitDice).GetValue(sheet).ShouldBe(4);
        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.ArmorClass).GetValue(sheet).ShouldBe(15);
        _monsterSystem.GetProperty<GameCounter>(sheet, "Morale").GetValue(sheet).ShouldBe(8);

        // Verify monster hit points (Hit Dice 4 * 4.5 = 18)
        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitPoints).GetValue(sheet).ShouldBe(18);
    }

    [Theory]
    [InlineData(1, 4)] // 1 HD = 4 HP (1 * 4.5 rounded down)
    [InlineData(2, 9)] // 2 HD = 9 HP (2 * 4.5 rounded down)
    [InlineData(3, 13)] // 3 HD = 13 HP (3 * 4.5 rounded down)
    [InlineData(4, 18)] // 4 HD = 18 HP (4 * 4.5 rounded down)
    [InlineData(5, 22)] // 5 HD = 22 HP (5 * 4.5 rounded down)
    [InlineData(10, 45)] // 10 HD = 45 HP (10 * 4.5 rounded down)
    public void Monster_HitPoints_CalculatedCorrectlyFromHitDice(int hitDice, int expectedHitPoints)
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Act
        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitDice).EditCharacterProperty(hitDice.ToString(CultureInfo.InvariantCulture), sheet);

        // Assert
        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitPoints).GetValue(sheet).ShouldBe(expectedHitPoints);
    }

    [Fact]
    public void Monster_NewMonsterDefaults_SetCorrectly()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();

        // Act
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Assert - Verify default starting values
        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitDice).GetValue(sheet).ShouldBe(1);
        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.ArmorClass).GetValue(sheet).ShouldBe(10);
        _monsterSystem.GetProperty<GameCounter>(sheet, "Morale").GetValue(sheet).ShouldBe(7);
        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitPoints).GetValue(sheet).ShouldBe(4); // 1 HD = 4 HP
    }

    [Fact]
    public void Monster_CharacterSummary_DisplaysCorrectHitDice()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitDice).EditCharacterProperty("6", sheet);

        // Act
        var summary = _gameSystem.GetCharacterSummary(sheet, CharacterType.Monster);

        // Assert
        summary.ShouldBe("6 HD Monster");
    }

    [Theory]
    [InlineData(1, "1 HD Monster")]
    [InlineData(5, "5 HD Monster")]
    [InlineData(12, "12 HD Monster")]
    public void Monster_CharacterSummary_HandlesAllHitDiceValues(int hitDice, string expectedSummary)
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitDice).EditCharacterProperty(hitDice.ToString(CultureInfo.InvariantCulture), sheet);

        // Act
        var summary = _gameSystem.GetCharacterSummary(sheet, CharacterType.Monster);

        // Assert
        summary.ShouldBe(expectedSummary);
    }

    [Fact]
    public void Monster_AllCountersAccessible_CanSetAndRetrieveValues()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Act & Assert - Test all monster counters can be set and retrieved
        var hitDiceCounter = _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitDice);
        hitDiceCounter.EditCharacterProperty("8", sheet);
        hitDiceCounter.GetValue(sheet).ShouldBe(8);

        var acCounter = _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.ArmorClass);
        acCounter.EditCharacterProperty("16", sheet);
        acCounter.GetValue(sheet).ShouldBe(16);

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(sheet, "Morale");
        moraleCounter.EditCharacterProperty("9", sheet);
        moraleCounter.GetValue(sheet).ShouldBe(9);

        // Attack counter should be available
        var attackCounter = _monsterSystem.GetProperty<GameCounter>(sheet, "Attack");
        attackCounter.ShouldNotBeNull();

        // Skill counter should be available
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(sheet, "Skill");
        skillCounter.ShouldNotBeNull();

        // Save counter should be available
        var saveCounter = _monsterSystem.GetProperty<GameCounter>(sheet, "Save");
        saveCounter.ShouldNotBeNull();

        // Hit Points should be computed correctly
        var hitPointsCounter = _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitPoints);
        hitPointsCounter.GetValue(sheet).ShouldBe(36); // 8 HD * 4.5 = 36
    }

    [Fact]
    public void Monster_MonsterSystemPropertyGroups_ContainsExpectedGroups()
    {
        // Arrange & Act
        var propertyGroups = _monsterSystem.GetPropertyGroups();

        // Assert
        propertyGroups.ShouldNotBeEmpty();

        // Should have Monster Stats group
        var monsterStatsGroup = propertyGroups.FirstOrDefault(g => g.GroupName == SwnSystem.MonsterStats);
        monsterStatsGroup.ShouldNotBeNull();

        // Should contain all expected monster properties
        var propertyNames = monsterStatsGroup.Properties.Select(p => p.Name).ToList();
        propertyNames.ShouldContain(SwnSystem.HitDice);
        propertyNames.ShouldContain(SwnSystem.ArmorClass);
        propertyNames.ShouldContain("Attack");
        propertyNames.ShouldContain("Morale");
        propertyNames.ShouldContain("Skill");
        propertyNames.ShouldContain("Save");
        propertyNames.ShouldContain(SwnSystem.HitPoints);
    }

    [Fact]
    public void Monster_MinimumHitDice_RespectsMinimumValue()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Act & Assert - Try to set hit dice below minimum
        var hitDiceCounter = _monsterSystem.GetProperty<GameCounter>(sheet, SwnSystem.HitDice);

        // Should not allow values below 1
        hitDiceCounter.TryEditCharacterProperty("0", sheet, out var errorMessage).ShouldBeFalse();
        errorMessage.ShouldNotBeNullOrEmpty();

        // Should allow minimum value of 1
        hitDiceCounter.TryEditCharacterProperty("1", sheet, out errorMessage).ShouldBeTrue();
        errorMessage.ShouldBeNullOrEmpty();
        hitDiceCounter.GetValue(sheet).ShouldBe(1);
    }
}