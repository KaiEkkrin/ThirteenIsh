using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;
using ThirteenIsh.Tests.Mocks;

namespace ThirteenIsh.Tests.Game.Swn;

/// <summary>
/// Integration tests for SWN combat mechanics, including attacks, damage, and encounter interactions.
/// Tests the full combat system rather than individual methods.
/// </summary>
public class SwnCombatIntegrationTests
{
    private readonly SwnSystem _gameSystem;
    private readonly SwnCharacterSystem _playerSystem;
    private readonly SwnCharacterSystem _monsterSystem;

    public SwnCombatIntegrationTests()
    {
        _gameSystem = SwnTestHelpers.CreateSwnSystem();
        _playerSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
        _monsterSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.Monster);
    }

    [Fact]
    public void Combat_PlayerCharacterAndMonsterAttacks_RollsCalculatedCorrectly()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        SwnTestHelpers.SetupFullPlayerCharacter(player, _playerSystem);

        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = player.Sheet;

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = monster.Sheet;

        // Set up predictable dice rolls: d20 roll = 15, d6 rolls = 4, 4
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 15, 6, 4, 6, 4);

        // Act & Assert - Test player Shoot skill attack (1d20 + skill + dex bonus + attack bonus)
        // Shoot skill = 2, Dex bonus = +1, Attack bonus = 3, d20 = 15
        var shootCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, SwnSystem.Shoot);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? targetAC = 15; // Monster's AC
        var attackResult = shootCounter.Roll(adventurer, null, mockRandom, 0, ref targetAC, dexBonusCounter, GameCounterRollOptions.IsAttack);

        // Expected: 15 (d20) + 2 (skill) + 1 (dex) + 2 (attack bonus) = 20
        attackResult.Error.ShouldBe(GameCounterRollError.Success);
        attackResult.Roll.ShouldBe(20);
        attackResult.Success.ShouldBe(true); // 20 >= 15 (target AC)
        attackResult.Working.ShouldContain("15"); // Should show the d20 roll
        attackResult.CounterName.ShouldContain("Shoot");
        attackResult.CounterName.ShouldContain("attack");

        // Test player skill check (2d6 + skill + attribute bonus)
        // Using a new mock for 2d6 rolls
        var skillCheckRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 4);
        int? skillTarget = 8;
        var skillResult = shootCounter.Roll(adventurer, null, skillCheckRandom, 0, ref skillTarget, dexBonusCounter, GameCounterRollOptions.None);

        // Expected: 4 + 4 (2d6) + 2 (skill) + 1 (dex) = 11
        skillResult.Error.ShouldBe(GameCounterRollError.Success);
        skillResult.Roll.ShouldBe(11);
        skillResult.Success.ShouldBe(true); // 11 >= 8
        skillResult.CounterName.ShouldContain("Shoot");
        skillResult.CounterName.ShouldNotContain("attack");
    }

    [Fact]
    public void Combat_MonsterAttack_RollsCalculatedCorrectly()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = monster.Sheet;

        // Set monster attack value - need to parse the "+6" format
        var attackCounter = _monsterSystem.GetProperty<GameCounter>(monsterCombatant.Sheet, "Attack");

        // Set up predictable dice rolls: d20 roll = 12
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 12);

        // Act - Monster attack uses AttackCounter which rolls 1d20 + attack bonus
        int? targetAC = 14; // Player's AC
        var attackResult = attackCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetAC);

        // Assert
        attackResult.Error.ShouldBe(GameCounterRollError.Success);
        // The attack counter should extract the numeric value from "+6" and add it to the d20 roll
        // Expected: 12 (d20) + 6 (attack bonus) = 18
        attackResult.Roll.ShouldBe(18);
        attackResult.Success.ShouldBe(true); // 18 >= 14 (target AC)
        attackResult.Working.ShouldContain("12"); // Should show the d20 roll
    }

    [Fact]
    public void Combat_HitPointDamage_TrackedCorrectly()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        SwnTestHelpers.SetupFullPlayerCharacter(player, _playerSystem);

        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = player.Sheet;

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = monster.Sheet;

        // Get initial hit points
        var playerHPCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, SwnSystem.HitPoints);
        var monsterHPCounter = _monsterSystem.GetProperty<GameCounter>(monsterCombatant.Sheet, SwnSystem.HitPoints);

        var initialPlayerHP = playerHPCounter.GetValue(adventurer);
        var initialMonsterHP = monsterHPCounter.GetValue(monsterCombatant);

        initialPlayerHP.ShouldBe(19); // Expected from setup
        initialMonsterHP.ShouldBe(18); // Expected from setup

        // Act - Apply damage to both characters
        // Player takes 5 damage
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.HitPoints, -5));

        // Monster takes 8 damage
        monsterCombatant.GetFixes().Counters.Add(new PropertyValue<int>(SwnSystem.HitPoints, -8));

        // Assert - Check remaining hit points
        var remainingPlayerHP = playerHPCounter.GetValue(adventurer);
        var remainingMonsterHP = monsterHPCounter.GetValue(monsterCombatant);

        remainingPlayerHP.ShouldBe(14); // 19 - 5 = 14
        remainingMonsterHP.ShouldBe(10); // 18 - 8 = 10
    }

    [Fact]
    public void Combat_EncounterInitiative_PlayerAndMonsterJoinCorrectly()
    {
        // Arrange
        var encounter = SwnTestHelpers.CreateEncounter();
        var nameAliases = new NameAliasCollection(encounter);

        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        SwnTestHelpers.SetupFullPlayerCharacter(player, _playerSystem);

        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = player.Sheet;

        // Set up predictable dice rolls for initiative
        // Player initiative: 1d8 + Dex bonus = d8 roll + 1
        // Monster initiative: 1d8 = d8 roll
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(8, 6, 8, 4); // Player gets 6+1=7, Monster gets 4

        // Act - Player joins encounter
        var playerResult = _gameSystem.EncounterJoin(adventurer, encounter, nameAliases, mockRandom, 0, 12345);

        // Monster joins encounter
        var monsterResult = _gameSystem.EncounterAdd(monster, encounter, nameAliases, mockRandom, 0, 1, 12345);

        // Assert
        playerResult.Error.ShouldBe(GameCounterRollError.Success);
        playerResult.Roll.ShouldBe(7); // 6 (d8) + 1 (Dex bonus)
        playerResult.Working.ShouldContain("6"); // Should show the d8 roll

        monsterResult.Error.ShouldBe(GameCounterRollError.Success);
        monsterResult.Roll.ShouldBe(4); // 4 (d8) for monster

        // Check encounter turn order (higher initiative goes first)
        encounter.CombatantsInTurnOrder.Count().ShouldBe(2);
        encounter.CombatantsInTurnOrder.First().Initiative.ShouldBe(7); // Player goes first
        encounter.CombatantsInTurnOrder.Last().Initiative.ShouldBe(4); // Monster goes second
    }

    [Fact]
    public void Combat_AttackMissScenario_TargetNotMetShouldShowMiss()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        SwnTestHelpers.SetupFullPlayerCharacter(player, _playerSystem);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = player.Sheet;

        // Set up a low dice roll that will miss high AC
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 5); // Low d20 roll

        // Act - Attack against high AC (will miss)
        var shootCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, SwnSystem.Shoot);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? targetAC = 20; // Very high AC
        var attackResult = shootCounter.Roll(adventurer, null, mockRandom, 0, ref targetAC, dexBonusCounter, GameCounterRollOptions.IsAttack);

        // Assert
        attackResult.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 5 (d20) + 2 (skill) + 1 (dex) + 2 (attack bonus) = 10
        attackResult.Roll.ShouldBe(10);
        attackResult.Success.ShouldBe(false); // 10 < 20 (target AC) = miss
        attackResult.Working.ShouldContain("5"); // Should show the d20 roll
    }

    [Fact]
    public void Combat_SkilllessCharacterAttack_ShowsUnskilledPenalty()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        // Set basic attributes but don't set up skills, so character will be unskilled (-1 default)
        var sheet = player.Sheet;
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("10", sheet);
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty("1", sheet);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = player.Sheet;

        // Set up dice roll
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 15);

        // Act - Unskilled Shoot attack
        var shootCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, SwnSystem.Shoot);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? targetAC = 15;
        var attackResult = shootCounter.Roll(adventurer, null, mockRandom, 0, ref targetAC, dexBonusCounter, GameCounterRollOptions.IsAttack);

        // Assert
        attackResult.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 15 (d20) + (-1) (unskilled) + (-2) (unfamiliar) + 0 (dex) + 0 (attack bonus) = 12
        attackResult.Roll.ShouldBe(12);
        attackResult.CounterName.ShouldContain("unskilled"); // Should indicate unskilled penalty
        attackResult.CounterName.ShouldContain("unfamiliar"); // Should indicate weapon unfamiliarity penalty
    }

    [Fact]
    public void Combat_WeaponUnfamiliarityPenalty_AppliedCorrectlyToAttackRolls()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);

        // Set up character with basic attributes but no weapon skills (will be unskilled -1 with additional -2 unfamiliarity)
        var sheet = player.Sheet;
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("14", sheet); // +1 bonus
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty("1", sheet);
        _playerSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(SwnSystem.Expert, sheet);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = player.Sheet;

        // Set up predictable dice roll
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 12);

        // Act - Attack with Shoot skill (no training = -1 skill + -2 unfamiliarity penalty)
        var shootCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, SwnSystem.Shoot);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? targetAC = 15;
        var attackResult = shootCounter.Roll(adventurer, null, mockRandom, 0, ref targetAC, dexBonusCounter, GameCounterRollOptions.IsAttack);

        // Assert
        attackResult.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 12 (d20) + (-1) (unskilled) + (-2) (unfamiliar) + 1 (dex) + 0 (base attack bonus) = 10
        attackResult.Roll.ShouldBe(10);
        attackResult.Success.ShouldBe(false); // 10 < 15 (target AC)
        attackResult.CounterName.ShouldContain("Shoot");
        attackResult.CounterName.ShouldContain("attack");
        attackResult.CounterName.ShouldContain("unskilled");
        attackResult.CounterName.ShouldContain("unfamiliar");
        attackResult.Working.ShouldContain("12"); // Should show the d20 roll
    }

    [Fact]
    public void Combat_SkilledCharacterAttack_NoUnfamiliarityPenalty()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);

        // Set up character with Shoot skill level 0 (trained but basic)
        var sheet = player.Sheet;
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("14", sheet); // +1 bonus
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty("1", sheet);
        _playerSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(SwnSystem.Expert, sheet);
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Shoot).EditCharacterProperty("0", sheet); // Level-0 skill

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = player.Sheet;

        // Set up predictable dice roll
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 12);

        // Act - Attack with Shoot skill (level 0 = no unfamiliarity penalty)
        var shootCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, SwnSystem.Shoot);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? targetAC = 15;
        var attackResult = shootCounter.Roll(adventurer, null, mockRandom, 0, ref targetAC, dexBonusCounter, GameCounterRollOptions.IsAttack);

        // Assert
        attackResult.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 12 (d20) + 0 (skill level 0) + 1 (dex) + 0 (base attack bonus) = 13
        attackResult.Roll.ShouldBe(13);
        attackResult.Success.ShouldBe(false); // 13 < 15 (target AC)
        attackResult.CounterName.ShouldContain("Shoot");
        attackResult.CounterName.ShouldContain("attack");
        attackResult.CounterName.ShouldNotContain("unskilled"); // Should NOT show unskilled
        attackResult.CounterName.ShouldNotContain("unfamiliar"); // Should NOT show unfamiliar
        attackResult.Working.ShouldContain("12"); // Should show the d20 roll
    }

    [Fact]
    public void Combat_UnskilledSkillCheck_OnlyGetsUnskilledPenalty()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);

        // Set up character with basic attributes but no weapon skills (unskilled but no unfamiliarity for skill checks)
        var sheet = player.Sheet;
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("14", sheet); // +1 bonus
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty("1", sheet);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = player.Sheet;

        // Set up predictable dice rolls for 2d6 skill check
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 5);

        // Act - Skill check with Shoot skill (should only get -1 unskilled, no -2 unfamiliarity)
        var shootCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, SwnSystem.Shoot);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(adventurer.Sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? skillTarget = 8;
        var skillResult = shootCounter.Roll(adventurer, null, mockRandom, 0, ref skillTarget, dexBonusCounter, GameCounterRollOptions.None);

        // Assert
        skillResult.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 3 + 5 (2d6) + (-1) (unskilled) + 1 (dex) = 8 (no unfamiliarity penalty for skill checks)
        skillResult.Roll.ShouldBe(8);
        skillResult.Success.ShouldBe(true); // 8 >= 8 (target)
        skillResult.CounterName.ShouldContain("Shoot");
        skillResult.CounterName.ShouldNotContain("attack"); // This is a skill check, not an attack
        skillResult.CounterName.ShouldContain("unskilled"); // Should show unskilled
        skillResult.CounterName.ShouldNotContain("unfamiliar"); // Should NOT show unfamiliar for skill checks
    }

    [Fact]
    public async Task Combat_CharacterDataService_MockWorksCorrectly()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = player.Sheet;

        var monster = SwnTestHelpers.CreateMonster();
        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant("TestMonster", monster.Sheet);

        var encounter = SwnTestHelpers.CreateEncounter();

        // Create mock service that returns our characters
        var mockService = SwnTestHelpers.CreateMockCharacterDataService(adventurer, monsterCombatant);

        // Act & Assert - Test that the mock service returns correct characters
        var retrievedAdventurer = await mockService.GetCharacterAsync(
            new AdventurerCombatant { Name = adventurer.Name, Alias = adventurer.Name, UserId = 12345 }, encounter);
        var retrievedMonster = await mockService.GetCharacterAsync(
            new MonsterCombatant {
                Name = monsterCombatant.Name,
                Alias = monsterCombatant.Name,
                UserId = 12345,
                LastUpdated = DateTimeOffset.UtcNow,
                Sheet = new CharacterSheet()
            }, encounter);

        retrievedAdventurer.ShouldBe(adventurer);
        retrievedMonster.ShouldBe(monsterCombatant);

        // Test null case for unknown character
        var unknownCharacter = await mockService.GetCharacterAsync(
            new MonsterCombatant {
                Name = "Unknown",
                Alias = "Unknown",
                UserId = 12345,
                LastUpdated = DateTimeOffset.UtcNow,
                Sheet = new CharacterSheet()
            }, encounter);
        unknownCharacter.ShouldBeNull();
    }
}