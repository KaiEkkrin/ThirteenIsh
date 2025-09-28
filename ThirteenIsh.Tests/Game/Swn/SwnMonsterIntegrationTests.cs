using System.Globalization;
using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;
using ThirteenIsh.Parsing;

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
        _monsterSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.Monster, null);
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
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice).GetValue(monster).ShouldBe(4);
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.ArmorClass).GetValue(monster).ShouldBe(15);
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").GetValue(monster).ShouldBe(8);

        // Verify monster hit points (Hit Dice 4 * 4.5 = 18)
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitPoints).GetValue(monster).ShouldBe(18);
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
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice).EditCharacterProperty(hitDice.ToString(CultureInfo.InvariantCulture), monster);

        // Assert
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitPoints).GetValue(monster).ShouldBe(expectedHitPoints);
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
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice).GetValue(monster).ShouldBe(1);
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.ArmorClass).GetValue(monster).ShouldBe(10);
        _monsterSystem.GetProperty<GameCounter>(monster, "Morale").GetValue(monster).ShouldBe(7);
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitPoints).GetValue(monster).ShouldBe(4); // 1 HD = 4 HP
    }

    [Fact]
    public void Monster_CharacterSummary_DisplaysCorrectHitDice()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice).EditCharacterProperty("6", monster);

        // Act
        var summary = _gameSystem.GetCharacterSummary(monster);

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

        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice).EditCharacterProperty(hitDice.ToString(CultureInfo.InvariantCulture), monster);

        // Act
        var summary = _gameSystem.GetCharacterSummary(monster);

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
        var hitDiceCounter = _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice);
        hitDiceCounter.EditCharacterProperty("8", monster);
        hitDiceCounter.GetValue(monster).ShouldBe(8);

        var acCounter = _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.ArmorClass);
        acCounter.EditCharacterProperty("16", monster);
        acCounter.GetValue(monster).ShouldBe(16);

        var moraleCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Morale");
        moraleCounter.EditCharacterProperty("9", monster);
        moraleCounter.GetValue(monster).ShouldBe(9);

        // Attack counter should be available
        var attackCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Attack");
        attackCounter.ShouldNotBeNull();

        // Skill counter should be available
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        skillCounter.ShouldNotBeNull();

        // Save counter should be available
        var saveCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Save");
        saveCounter.ShouldNotBeNull();

        // Hit Points should be computed correctly
        var hitPointsCounter = _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitPoints);
        hitPointsCounter.GetValue(monster).ShouldBe(36); // 8 HD * 4.5 = 36
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
        var hitDiceCounter = _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice);

        // Should not allow values below 1
        hitDiceCounter.TryEditCharacterProperty("0", monster, out var errorMessage).ShouldBeFalse();
        errorMessage.ShouldNotBeNullOrEmpty();

        // Should allow minimum value of 1
        hitDiceCounter.TryEditCharacterProperty("1", monster, out errorMessage).ShouldBeTrue();
        errorMessage.ShouldBeNullOrEmpty();
        hitDiceCounter.GetValue(monster).ShouldBe(1);
    }

    [Fact]
    public void Monster_SkillCheck_BasicRollCalculation()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +2
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("2", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up predictable dice rolls for 2d6: 4 + 3 = 7
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 3);

        // Act - Monster skill check: 2d6 + skill bonus (no attribute bonus)
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 10;
        var result = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref skillTarget);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 4 + 3 (2d6) + 2 (skill bonus) = 9
        result.Roll.ShouldBe(9);
        result.Success.ShouldBe(false); // 9 < 10 (target)
        result.CounterName.ShouldBe("Skill");
        result.Working.ShouldContain("4");
        result.Working.ShouldContain("3");
    }

    [Fact]
    public void Monster_SkillCheck_WithPositiveSkillBonus()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +3 (skilled)
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("3", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up predictable dice rolls for 2d6: 5 + 4 = 9
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 5, 6, 4);

        // Act - Monster skill check with positive skill bonus
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 12;
        var result = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref skillTarget);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 5 + 4 (2d6) + 3 (skill bonus) = 12
        result.Roll.ShouldBe(12);
        result.Success.ShouldBe(true); // 12 >= 12 (target)
        result.CounterName.ShouldBe("Skill");
        result.CounterName.ShouldNotContain("unskilled"); // Should not show unskilled for positive bonus
    }

    [Fact]
    public void Monster_SkillCheck_WithNegativeSkillBonus()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Monster starts with default skill of -1 (unskilled)
        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up predictable dice rolls for 2d6: 6 + 5 = 11
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 6, 6, 5);

        // Act - Monster skill check with negative skill bonus (unskilled)
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 8;
        var result = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref skillTarget);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 6 + 5 (2d6) + (-1) (unskilled penalty) = 10
        result.Roll.ShouldBe(10);
        result.Success.ShouldBe(true); // 10 >= 8 (target)
        result.CounterName.ShouldContain("unskilled"); // Should show unskilled for negative bonus
    }

    [Fact]
    public void Monster_UnskilledSkillCheck_ShowsUnskilledPenalty()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Leave monster at default skill level (-1) - unskilled
        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up dice rolls
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 4);

        // Act - Unskilled monster skill check
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 6;
        var result = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref skillTarget);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 3 + 4 (2d6) + (-1) (unskilled) = 6
        result.Roll.ShouldBe(6);
        result.Success.ShouldBe(true); // 6 >= 6 (target)
        result.CounterName.ShouldBe("Skill unskilled");
        result.CounterName.ShouldContain("unskilled");
    }

    [Fact]
    public void Monster_SkillCheck_SuccessAndFailureScenarios()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +1
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("1", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");

        // Test successful skill check
        var highRollRandom = SwnTestHelpers.CreatePredictableRandom(6, 6, 6, 5); // 6 + 5 = 11
        int? easyTarget = 8;
        var successResult = skillCounter.Roll(monsterCombatant, null, highRollRandom, 0, ref easyTarget);

        // Assert success
        successResult.Error.ShouldBe(GameCounterRollError.Success);
        successResult.Roll.ShouldBe(12); // 11 (2d6) + 1 (skill) = 12
        successResult.Success.ShouldBe(true); // 12 >= 8

        // Test failed skill check
        var lowRollRandom = SwnTestHelpers.CreatePredictableRandom(6, 1, 6, 1); // 1 + 1 = 2
        int? hardTarget = 15;
        var failResult = skillCounter.Roll(monsterCombatant, null, lowRollRandom, 0, ref hardTarget);

        // Assert failure
        failResult.Error.ShouldBe(GameCounterRollError.Success);
        failResult.Roll.ShouldBe(3); // 2 (2d6) + 1 (skill) = 3
        failResult.Success.ShouldBe(false); // 3 < 15
    }

    [Fact]
    public void Monster_SkillCheck_NoAttributeBonus()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +2
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("2", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up predictable dice rolls
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 5);

        // Act - Monster skill check with NO second counter (no attribute bonus)
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 10;
        var result = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref skillTarget, null);

        // Assert - Only skill bonus should be applied, no attribute bonus
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 3 + 5 (2d6) + 2 (skill bonus only) = 10
        result.Roll.ShouldBe(10);
        result.Success.ShouldBe(true); // 10 >= 10
        result.CounterName.ShouldBe("Skill"); // No attribute abbreviation should appear
        result.CounterName.ShouldNotContain("("); // No parentheses for attribute bonus
    }

    [Fact]
    public void Monster_SkillCounter_CannotMakeAttackRolls()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +2
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("2", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 15);

        // Act - Try to make an attack roll with monster skill counter
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? targetAC = 15;
        var result = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetAC, null, GameCounterRollOptions.IsAttack);

        // Assert - Should fail because monsters can't attack with skill counters (no AttackBonusCounter)
        result.Error.ShouldBe(GameCounterRollError.NotRollable);
        result.Working.ShouldContain("Cannot make attack rolls with monster skill counter");
        result.CounterName.ShouldBe("Skill");
    }

    [Fact]
    public void Monster_SkillCheck_VsAttackRoll_Different()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +1 and attack value for comparison
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("1", monster);
        _monsterSystem.GetProperty<GameCounter>(monster, "Attack").EditCharacterProperty("+4", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Test skill check (should work)
        var skillRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 3);
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 8;
        var skillResult = skillCounter.Roll(monsterCombatant, null, skillRandom, 0, ref skillTarget);

        // Assert skill check works
        skillResult.Error.ShouldBe(GameCounterRollError.Success);
        skillResult.Roll.ShouldBe(8); // 4 + 3 (2d6) + 1 (skill) = 8
        skillResult.Success.ShouldBe(true);

        // Test attack with actual attack counter (should work differently)
        var attackRandom = SwnTestHelpers.CreatePredictableRandom(20, 12);
        var attackCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Attack");
        int? targetAC = 15;
        var attackResult = attackCounter.Roll(monsterCombatant, null, attackRandom, 0, ref targetAC);

        // Assert attack uses different mechanics (1d20 + attack bonus, not 2d6 + skill)
        attackResult.Error.ShouldBe(GameCounterRollError.Success);
        // Attack counter has different calculation than skill check
        attackResult.Roll.ShouldNotBe(skillResult.Roll); // Different roll mechanics
    }

    [Fact]
    public void Monster_SkillCheck_NoUnfamiliarityPenalty()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Leave monster at default skill (-1, unskilled)
        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up dice rolls
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 5, 6, 4);

        // Act - Unskilled monster skill check
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 8;
        var result = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref skillTarget);

        // Assert - Monsters don't get unfamiliarity penalty for skill checks (unlike players with attack rolls)
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 5 + 4 (2d6) + (-1) (unskilled) = 8 (no additional -2 unfamiliarity penalty)
        result.Roll.ShouldBe(8);
        result.Success.ShouldBe(true); // 8 >= 8
        result.CounterName.ShouldContain("unskilled");
        result.CounterName.ShouldNotContain("unfamiliar"); // Monsters don't get unfamiliarity penalty for skill checks
    }

    [Fact]
    public void Monster_SkillCheck_Uses2d6NotBonusAttribute()
    {
        // Arrange - Create both monster and player for comparison
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var monsterSheet = monster.Sheet;

        var player = SwnTestHelpers.CreatePlayerCharacter();
        var playerSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter, null);
        playerSystem.SetNewCharacterStartingValues(player);
        var playerSheet = player.Sheet;

        // Set same skill level for both (but player gets attribute bonus, monster doesn't)
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("1", monster);
        playerSystem.GetProperty<GameCounter>(player, SwnSystem.Shoot).EditCharacterProperty("1", player);
        playerSystem.GetProperty<GameAbilityCounter>(player, SwnSystem.Dexterity).EditCharacterProperty("14", player); // +1 bonus

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = monsterSheet;

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = playerSheet;

        // Use same dice rolls for comparison
        var mockRandom1 = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 4); // 7 total
        var mockRandom2 = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 4); // 7 total

        // Act - Test monster skill check (2d6 + skill only)
        var monsterSkillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? monsterTarget = 8;
        var monsterResult = monsterSkillCounter.Roll(monsterCombatant, null, mockRandom1, 0, ref monsterTarget);

        // Test player skill check (2d6 + skill + attribute bonus)
        var playerSkillCounter = playerSystem.GetProperty<GameCounter>(player, SwnSystem.Shoot);
        var dexBonusCounter = playerSystem.GetProperty<GameCounter>(player, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));
        int? playerTarget = 8;
        var playerResult = playerSkillCounter.Roll(adventurer, null, mockRandom2, 0, ref playerTarget, dexBonusCounter, GameCounterRollOptions.None);

        // Assert - Monster should have lower total (no attribute bonus)
        monsterResult.Error.ShouldBe(GameCounterRollError.Success);
        playerResult.Error.ShouldBe(GameCounterRollError.Success);

        // Monster: 3 + 4 (2d6) + 1 (skill) = 8
        monsterResult.Roll.ShouldBe(8);
        // Player: 3 + 4 (2d6) + 1 (skill) + 1 (dex bonus) = 9
        playerResult.Roll.ShouldBe(9);

        // Verify the difference comes from attribute bonus
        playerResult.Roll.ShouldBe(monsterResult.Roll + 1); // Player gets +1 from attribute
        monsterResult.CounterName.ShouldBe("Skill");
        playerResult.CounterName.ShouldContain("(DEX)"); // Player shows attribute in name
    }

    [Fact]
    public void Monster_SkillCheck_WithDifferentHitDice()
    {
        // Arrange - Test that hit dice don't affect skill checks (they should be independent)
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +2
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("2", monster);

        // Test with different hit dice values
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice).EditCharacterProperty("8", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up predictable dice rolls
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 5);

        // Act - Monster skill check should be unaffected by hit dice
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 11;
        var result = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref skillTarget);

        // Assert - Skill check calculation should be independent of hit dice
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 4 + 5 (2d6) + 2 (skill bonus) = 11 (hit dice should not factor in)
        result.Roll.ShouldBe(11);
        result.Success.ShouldBe(true); // 11 >= 11
        result.CounterName.ShouldBe("Skill");

        // Verify hit dice is different but doesn't affect skill check
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice).GetValue(monster).ShouldBe(8);
        _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitPoints).GetValue(monster).ShouldBe(36); // 8 * 4.5 = 36
    }

    [Fact]
    public void Monster_SkillCheck_WithSwarmCount()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +1
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("1", monster);

        // Create a swarm with 3 monsters
        var swarmCombatant = SwnTestHelpers.CreateMonsterCombatant("SwarmMonster", sheet);
        swarmCombatant.SwarmCount = 3;

        // Set up predictable dice rolls
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 6);

        // Act - Swarm monster skill check should use same mechanics as single monster
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 10;
        var result = skillCounter.Roll(swarmCombatant, null, mockRandom, 0, ref skillTarget);

        // Assert - Swarm count should not affect skill check mechanics
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 3 + 6 (2d6) + 1 (skill bonus) = 10 (swarm count irrelevant for skill checks)
        result.Roll.ShouldBe(10);
        result.Success.ShouldBe(true); // 10 >= 10
        result.CounterName.ShouldBe("Skill");

        // Verify swarm has different hit points but same skill mechanics
        var hitPointsCounter = _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitPoints);
        var swarmHP = hitPointsCounter.GetValue(swarmCombatant);
        var singleMonsterHP = hitPointsCounter.GetValue(monster);

        swarmHP.ShouldBe(singleMonsterHP * 3); // Swarm has 3x HP
        // But skill check mechanics remain the same
    }

    [Fact]
    public void Monster_SkillCheck_WithAdHocBonus()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +1
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("1", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up predictable dice rolls for 2d6: 4 + 2 = 6
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 2);

        // Create an ad hoc bonus of +3
        var bonus = new IntegerParseTree(0, 3);

        // Act - Monster skill check with ad hoc bonus: 2d6 + skill + bonus
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 10;
        var result = skillCounter.Roll(monsterCombatant, bonus, mockRandom, 0, ref skillTarget);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 4 + 2 (2d6) + 1 (skill bonus) + 3 (ad hoc bonus) = 10
        result.Roll.ShouldBe(10);
        result.Success.ShouldBe(true); // 10 >= 10 (target)
        result.CounterName.ShouldBe("Skill");
        result.Working.ShouldContain("4"); // Should show first d6 roll
        result.Working.ShouldContain("2"); // Should show second d6 roll
        result.Working.ShouldContain("3"); // Should show the ad hoc bonus
    }

    [Fact]
    public void Monster_SkillCheck_WithNegativeAdHocBonus()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Set monster skill to +2
        _monsterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("2", monster);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up predictable dice rolls for 2d6: 5 + 3 = 8
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 5, 6, 3);

        // Create an ad hoc penalty of -2
        var penalty = new IntegerParseTree(0, -2);

        // Act - Monster skill check with ad hoc penalty: 2d6 + skill + penalty
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(monster, "Skill");
        int? skillTarget = 8;
        var result = skillCounter.Roll(monsterCombatant, penalty, mockRandom, 0, ref skillTarget);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 5 + 3 (2d6) + 2 (skill bonus) + (-2) (ad hoc penalty) = 8
        result.Roll.ShouldBe(8);
        result.Success.ShouldBe(true); // 8 >= 8 (target)
        result.CounterName.ShouldBe("Skill");
        result.Working.ShouldContain("5"); // Should show first d6 roll
        result.Working.ShouldContain("3"); // Should show second d6 roll
        result.Working.ShouldContain("-2"); // Should show the ad hoc penalty
    }

    [Fact]
    public void Monster_EffortCounter_DoesNotExistForMonsters()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Act & Assert - Monsters should not have an Effort counter at all
        // The monster system property groups should not include Effort
        var propertyGroups = _monsterSystem.GetPropertyGroups();
        var allProperties = propertyGroups.SelectMany(g => g.Properties).Select(p => p.Name).ToList();

        allProperties.ShouldNotContain(SwnSystem.Effort);

        // Attempting to get an Effort property should throw since it doesn't exist
        Should.Throw<ArgumentOutOfRangeException>(() =>
            _monsterSystem.GetProperty<GameCounter>(monster, SwnSystem.Effort));
    }

    [Fact]
    public void Monster_NoEffortProperty_ConfirmsMonsterSystemDesign()
    {
        // Arrange & Act
        var propertyGroups = _monsterSystem.GetPropertyGroups();

        // Assert - Verify monsters only have the Monster Stats group and no psychic-related properties
        propertyGroups.Count().ShouldBe(1);

        var monsterStatsGroup = propertyGroups.Single();
        monsterStatsGroup.GroupName.ShouldBe(SwnSystem.MonsterStats);

        var propertyNames = monsterStatsGroup.Properties.Select(p => p.Name).ToList();

        // Should have standard monster properties
        propertyNames.ShouldContain(SwnSystem.HitDice);
        propertyNames.ShouldContain(SwnSystem.ArmorClass);
        propertyNames.ShouldContain("Attack");
        propertyNames.ShouldContain("Morale");
        propertyNames.ShouldContain("Skill");
        propertyNames.ShouldContain("Save");
        propertyNames.ShouldContain(SwnSystem.HitPoints);

        // Should NOT have psychic-related properties
        propertyNames.ShouldNotContain(SwnSystem.Effort);
        propertyNames.ShouldNotContain(SwnSystem.Telepathy);
        propertyNames.ShouldNotContain(SwnSystem.Telekinesis);
        propertyNames.ShouldNotContain(SwnSystem.Biopsionics);
        propertyNames.ShouldNotContain(SwnSystem.Metapsionics);
        propertyNames.ShouldNotContain(SwnSystem.Precognition);
        propertyNames.ShouldNotContain(SwnSystem.Teleportation);
    }
}