using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;
using ThirteenIsh.Parsing;

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

        initialPlayerHP.ShouldBe(19); // Expected from setup (max HP)
        initialMonsterHP.ShouldBe(18); // Expected from setup (max HP)

        // Verify initial variable values equal max HP
        var initialPlayerCurrentHP = playerHPCounter.GetVariableValue(adventurer);
        var initialMonsterCurrentHP = monsterHPCounter.GetVariableValue(monsterCombatant);

        initialPlayerCurrentHP.ShouldBe(19); // Current HP should start at max
        initialMonsterCurrentHP.ShouldBe(18); // Current HP should start at max

        // Act - Apply damage to both characters using SetVariableClamped (like real damage handler)
        // Player takes 5 damage: 19 - 5 = 14
        playerHPCounter.SetVariableClamped(initialPlayerCurrentHP!.Value - 5, adventurer);

        // Monster takes 8 damage: 18 - 8 = 10
        monsterHPCounter.SetVariableClamped(initialMonsterCurrentHP!.Value - 8, monsterCombatant);

        // Assert - Check that max HP values remain unchanged (representing maximum hit points)
        var maxPlayerHP = playerHPCounter.GetValue(adventurer);
        var maxMonsterHP = monsterHPCounter.GetValue(monsterCombatant);

        maxPlayerHP.ShouldBe(19); // Max HP should not change
        maxMonsterHP.ShouldBe(18); // Max HP should not change

        // Assert - Check that current HP (variable values) have been reduced by damage
        var currentPlayerHP = playerHPCounter.GetVariableValue(adventurer);
        var currentMonsterHP = monsterHPCounter.GetVariableValue(monsterCombatant);

        currentPlayerHP.ShouldBe(14); // 19 - 5 = 14
        currentMonsterHP.ShouldBe(10); // 18 - 8 = 10
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
    public void Combat_MonsterSwarm_Has3xHitPoints()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster("SwarmMonster");
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);

        // Create a swarm with 3 monsters
        var swarmCombatant = SwnTestHelpers.CreateMonsterCombatant("SwarmMonster", monster.Sheet);
        swarmCombatant.SwarmCount = 3;

        // Get the hit points counter
        var hitPointsCounter = _monsterSystem.GetProperty<GameCounter>(swarmCombatant.Sheet, SwnSystem.HitPoints);

        // Act - Get the hit point values
        var singleMonsterHP = hitPointsCounter.GetValue(monster.Sheet); // Base monster HP
        var swarmStartingHP = hitPointsCounter.GetStartingValue(swarmCombatant); // Swarm starting HP
        var swarmMaxHP = hitPointsCounter.GetMaxVariableValue(swarmCombatant); // Swarm max HP
        var swarmCurrentHP = hitPointsCounter.GetValue(swarmCombatant); // Current HP (should equal starting)

        // Assert - Swarm should have 3x the hit points of a single monster
        singleMonsterHP.ShouldBe(18); // Expected from monster setup (4 HD * 4.5 = 18)
        swarmStartingHP.ShouldBe(54); // 18 * 3 = 54
        swarmMaxHP.ShouldBe(54); // Same as starting for monsters
        swarmCurrentHP.ShouldBe(54); // Current should equal starting initially

        // Test that the swarm behaves like a single monster otherwise
        swarmCombatant.CharacterType.ShouldBe(CharacterType.Monster);
        swarmCombatant.Sheet.ShouldBe(monster.Sheet); // Same base stats
    }

    [Fact]
    public void Combat_MonsterSwarm_DamageTaken_ReducesFrom3xHitPoints()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster("SwarmMonster");
        _monsterSystem.SetNewCharacterStartingValues(monster);
        SwnTestHelpers.SetupFullMonster(monster, _monsterSystem);

        // Create a swarm with 3 monsters
        var swarmCombatant = SwnTestHelpers.CreateMonsterCombatant("SwarmMonster", monster.Sheet);
        swarmCombatant.SwarmCount = 3;

        var hitPointsCounter = _monsterSystem.GetProperty<GameCounter>(swarmCombatant.Sheet, SwnSystem.HitPoints);

        // Get initial hit points (should be 3x normal)
        var initialHP = hitPointsCounter.GetVariableValue(swarmCombatant);
        initialHP.ShouldBe(54); // 18 * 3

        // Act - Apply damage to the swarm (20 points of damage)
        var currentHP = hitPointsCounter.GetValue(swarmCombatant)!.Value;
        hitPointsCounter.SetVariableClamped(currentHP - 20, swarmCombatant);

        // Assert - Check remaining hit points
        var remainingHP = hitPointsCounter.GetVariableValue(swarmCombatant);
        remainingHP.ShouldBe(34); // 54 - 20 = 34

        // Verify the max HP is still 3x normal
        var maxHP = hitPointsCounter.GetMaxVariableValue(swarmCombatant);
        maxHP.ShouldBe(54);
    }

    [Fact]
    public void Combat_PlayerAttack_WithAdHocBonus()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Set up character with good attack stats
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("16", sheet); // +1 bonus
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty("3", sheet);
        _playerSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(SwnSystem.Expert, sheet);
        _playerSystem.GetProperty<GameProperty>(sheet, "Class 2").EditCharacterProperty(SwnSystem.Warrior, sheet);
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Shoot).EditCharacterProperty("2", sheet); // Skilled marksman

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Set up predictable d20 roll
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 12);

        // Create an ad hoc bonus of +3 (e.g., tactical advantage, magic weapon, etc.)
        var bonus = new IntegerParseTree(0, 3);

        // Act - Attack roll with ad hoc bonus: 1d20 + skill + attribute bonus + attack bonus + ad hoc bonus
        var shootCounter = _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Shoot);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? targetAC = 18; // High AC target
        var result = shootCounter.Roll(adventurer, bonus, mockRandom, 0, ref targetAC, dexBonusCounter, GameCounterRollOptions.IsAttack);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 12 (d20) + 2 (Shoot skill) + 1 (Dex bonus) + 2 (attack bonus from Expert/Warrior L3) + 3 (ad hoc bonus) = 20
        result.Roll.ShouldBe(20);
        result.Success.ShouldBe(true); // 20 >= 18 (target AC)
        result.CounterName.ShouldBe("Shoot attack (DEX)");
        result.Working.ShouldContain("12"); // Should show the d20 roll
        result.Working.ShouldContain("3"); // Should show the ad hoc bonus
    }

    [Fact]
    public void Combat_PlayerAttack_WithNegativeAdHocBonus()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Set up character with good attack stats
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("14", sheet); // +1 bonus
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty("2", sheet);
        _playerSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(SwnSystem.Warrior, sheet);
        _playerSystem.GetProperty<GameProperty>(sheet, "Class 2").EditCharacterProperty(SwnSystem.Warrior, sheet);
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Shoot).EditCharacterProperty("1", sheet); // Decent marksman

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Set up predictable d20 roll
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 16);

        // Create an ad hoc penalty of -3 (e.g., difficult conditions, cover, etc.)
        var penalty = new IntegerParseTree(0, -3);

        // Act - Attack roll with ad hoc penalty: 1d20 + skill + attribute bonus + attack bonus + ad hoc penalty
        var shootCounter = _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Shoot);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? targetAC = 15;
        var result = shootCounter.Roll(adventurer, penalty, mockRandom, 0, ref targetAC, dexBonusCounter, GameCounterRollOptions.IsAttack);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 16 (d20) + 1 (Shoot skill) + 1 (Dex bonus) + 2 (attack bonus from Warrior L2) + (-3) (ad hoc penalty) = 17
        result.Roll.ShouldBe(17);
        result.Success.ShouldBe(true); // 17 >= 15 (target AC)
        result.CounterName.ShouldBe("Shoot attack (DEX)");
        result.Working.ShouldContain("16"); // Should show the d20 roll
        result.Working.ShouldContain("-3"); // Should show the ad hoc penalty
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