using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Tests.Game.Swn;

/// <summary>
/// Unit tests for SWN morale check mechanics for monsters during combat.
/// Tests the 2d6 morale roll system where success is rolling equal to or less than the morale value.
/// </summary>
public class SwnMoraleTests
{
    private readonly SwnSystem _gameSystem;
    private readonly SwnCharacterSystem _monsterSystem;

    public SwnMoraleTests()
    {
        _gameSystem = SwnTestHelpers.CreateSwnSystem();
        _monsterSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.Monster, null);
    }

    [Fact]
    public void MonsterMorale_BasicRollSuccess_RollsCorrectly()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to 8
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("8", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll 2d6 = 7 (success because 7 <= 8)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 3); // First die 4, second die 3 = 7 total
        int? targetValue = null; // Should auto-use morale value
        var rollResult = moraleCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.CounterName.ShouldBe("Morale");
        rollResult.Roll.ShouldBe(7); // 4 + 3
        rollResult.Success.ShouldBe(true); // 7 <= 8 (success)
        rollResult.Working.ShouldContain("4");
        rollResult.Working.ShouldContain("3");
        targetValue.ShouldBe(8); // Should have been set to monster's morale value
    }

    [Fact]
    public void MonsterMorale_BasicRollFailure_RollsCorrectly()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to 6 (relatively low)
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("6", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll 2d6 = 9 (failure because 9 > 6)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 5, 6, 4); // First die 5, second die 4 = 9 total
        int? targetValue = 6;
        var rollResult = moraleCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.CounterName.ShouldBe("Morale");
        rollResult.Roll.ShouldBe(9); // 5 + 4
        rollResult.Success.ShouldBe(false); // 9 > 6 (failure)
        rollResult.Working.ShouldContain("5");
        rollResult.Working.ShouldContain("4");
    }

    [Fact]
    public void MonsterMorale_ExactlyEqualsTarget_Succeeds()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to 10
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("10", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll 2d6 = 10 (success because 10 <= 10)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 6, 6, 4); // First die 6, second die 4 = 10 total
        int? targetValue = 10;
        var rollResult = moraleCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.Roll.ShouldBe(10);
        rollResult.Success.ShouldBe(true); // 10 <= 10 (success - exactly meets target)
    }

    [Fact]
    public void MonsterMorale_WithPositiveBonus_AppliesBonusToRoll()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to 9
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("9", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll 2d6 = 6 + 2 bonus = 8 (success because 8 <= 9)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 3); // First die 3, second die 3 = 6 total
        var bonus = new IntegerParseTree(0, 2); // +2 bonus
        int? targetValue = 9;
        var rollResult = moraleCounter.Roll(monsterCombatant, bonus, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.Roll.ShouldBe(8); // 6 (2d6) + 2 (bonus) = 8
        rollResult.Success.ShouldBe(true); // 8 <= 9 (success)
        rollResult.Working.ShouldContain("3");
        rollResult.Working.ShouldContain("2"); // Should show the bonus
    }

    [Fact]
    public void MonsterMorale_WithNegativeBonus_AppliesPenaltyToRoll()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to 8
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("8", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll 2d6 = 10 - 3 penalty = 7 (success because 7 <= 8)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 6, 6, 4); // First die 6, second die 4 = 10 total
        var bonus = new IntegerParseTree(0, -3); // -3 penalty
        int? targetValue = 8;
        var rollResult = moraleCounter.Roll(monsterCombatant, bonus, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.Roll.ShouldBe(7); // 10 (2d6) - 3 (penalty) = 7
        rollResult.Success.ShouldBe(true); // 7 <= 8 (success)
        rollResult.Working.ShouldContain("6");
        rollResult.Working.ShouldContain("4");
    }

    [Fact]
    public void MonsterMorale_Natural2_Succeeds()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to very low value (would normally be hard to succeed)
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("2", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll natural 2 (minimum possible on 2d6)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 1, 6, 1); // First die 1, second die 1 = 2 total
        int? targetValue = 2;
        var rollResult = moraleCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.Roll.ShouldBe(2);
        rollResult.Success.ShouldBe(true); // 2 <= 2 (best possible morale roll)
        rollResult.Working.ShouldContain("1");
    }

    [Fact]
    public void MonsterMorale_Natural12_WorstPossibleRoll()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to maximum possible value
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("12", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll natural 12 (maximum possible on 2d6)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 6, 6, 6); // First die 6, second die 6 = 12 total
        int? targetValue = 12;
        var rollResult = moraleCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.Roll.ShouldBe(12);
        rollResult.Success.ShouldBe(true); // 12 <= 12 (exactly meets maximum)
        rollResult.Working.ShouldContain("6");
    }

    [Fact]
    public void MonsterMorale_Natural12AgainstLowMorale_Fails()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to low value
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("5", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll natural 12 against low morale (should fail)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 6, 6, 6); // First die 6, second die 6 = 12 total
        int? targetValue = 5;
        var rollResult = moraleCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.Roll.ShouldBe(12);
        rollResult.Success.ShouldBe(false); // 12 > 5 (failure - worst possible roll vs low morale)
    }

    [Fact]
    public void MonsterMorale_AutoTargeting_UsesMonsterMoraleValue()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to 7 (default value)
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("7", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll without specifying target (should auto-use morale value)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 2, 6, 4); // First die 2, second die 4 = 6 total
        int? targetValue = null; // No target specified - should auto-use 7
        var rollResult = moraleCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.CounterName.ShouldBe("Morale");
        rollResult.Roll.ShouldBe(6);
        rollResult.Success.ShouldBe(true); // 6 <= 7 (success)
        rollResult.Working.ShouldContain("2");
        rollResult.Working.ShouldContain("4");
        targetValue.ShouldBe(7); // Should have been set to monster's morale value
    }

    [Fact]
    public void MonsterMorale_BonusCausingFailure_RollsCorrectly()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);
        var sheet = monster.Sheet;

        // Set monster morale to 8
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("8", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");

        // Act - Roll 2d6 = 7, but +3 bonus makes it 10 > 8 (failure)
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 3); // First die 4, second die 3 = 7 total
        var bonus = new IntegerParseTree(0, 3); // +3 bonus (makes morale check worse!)
        int? targetValue = 8;
        var rollResult = moraleCounter.Roll(monsterCombatant, bonus, mockRandom, 0, ref targetValue);

        // Assert
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        rollResult.Roll.ShouldBe(10); // 7 (2d6) + 3 (bonus) = 10
        rollResult.Success.ShouldBe(false); // 10 > 8 (failure - bonus made it worse)
        rollResult.Working.ShouldContain("4");
        rollResult.Working.ShouldContain("3");
    }
}