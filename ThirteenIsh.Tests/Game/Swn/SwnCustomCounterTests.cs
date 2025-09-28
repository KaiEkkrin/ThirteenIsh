using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Tests.Game.Swn;

/// <summary>
/// Unit tests for SwnCustomCounter, testing both rollable counter (skill-like) and variable counter (hit points-like) functionality.
/// Custom counters in SWN should function like skills for rolling (with minimum value 0 instead of -1) and like hit points for variable tracking.
/// </summary>
public class SwnCustomCounterTests
{
    private readonly SwnSystem _gameSystem;
    private readonly SwnCharacterSystem _playerSystem;
    private readonly SwnCharacterSystem _monsterSystem;

    public SwnCustomCounterTests()
    {
        _gameSystem = SwnTestHelpers.CreateSwnSystem();
        _playerSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter, null);
        _monsterSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.Monster, null);
    }

    [Fact]
    public void CustomCounter_RollableSkillCheck_CalculatesCorrectly()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with CanRoll option, set to level 2
        var customCounter = new CustomCounter("TestSkill", 2, GameCounterOptions.CanRoll);
        sheet.CustomCounters = [customCounter];
        var skillCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestSkill");
        skillCounter.EditCharacterProperty("2", sheet);

        // Set Dexterity for attribute bonus
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("16", sheet);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Set up predictable dice rolls for 2d6 skill check
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 5);

        // Act - Skill check: 2d6 + skill level + attribute bonus
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));
        int? skillTarget = 10;
        var result = skillCounter.Roll(adventurer, null, mockRandom, 0, ref skillTarget, dexBonusCounter, GameCounterRollOptions.None);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 3 + 5 (2d6) + 2 (skill level) + 1 (dex bonus) = 11
        result.Roll.ShouldBe(11);
        result.Success.ShouldBe(true); // 11 >= 10
        result.CounterName.ShouldBe("TestSkill (DEX)");
        result.Working.ShouldContain("3");
        result.Working.ShouldContain("5");
    }

    [Fact]
    public void CustomCounter_AttackRoll_CalculatesCorrectly()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        SwnTestHelpers.SetupFullPlayerCharacter(player, _playerSystem);
        var sheet = player.Sheet;

        // Add custom counter with CanRoll option, set to level 1
        var customCounter = new CustomCounter("TestWeapon", 1, GameCounterOptions.CanRoll);
        sheet.CustomCounters = [customCounter];
        var weaponCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestWeapon");
        weaponCounter.EditCharacterProperty("1", sheet);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Set up predictable dice roll for 1d20 attack
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 15);

        // Act - Attack roll: 1d20 + skill level + attribute bonus + attack bonus
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));
        int? targetAC = 16;
        var result = weaponCounter.Roll(adventurer, null, mockRandom, 0, ref targetAC, dexBonusCounter, GameCounterRollOptions.IsAttack);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 15 (d20) + 1 (skill level) + 1 (dex bonus) + 2 (attack bonus) = 19
        result.Roll.ShouldBe(19);
        result.Success.ShouldBe(true); // 19 >= 16
        result.CounterName.ShouldBe("TestWeapon attack (DEX)");
        result.Working.ShouldContain("15");
    }

    [Fact]
    public void CustomCounter_UnskilledSkillCheck_HasCorrectPenalty()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with default value that allows negative to test unskilled behavior
        var customCounter = new CustomCounter("TestSkill", -1, GameCounterOptions.CanRoll);
        sheet.CustomCounters = [customCounter];
        var skillCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestSkill");
        // Don't set the value explicitly, it will use the default value of -1

        // Set Dexterity for attribute bonus
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("14", sheet);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Set up predictable dice rolls for 2d6 skill check
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 3);

        // Act - Unskilled skill check (should only get -1 penalty, no unfamiliarity for skill checks)
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));
        int? skillTarget = 8;
        var result = skillCounter.Roll(adventurer, null, mockRandom, 0, ref skillTarget, dexBonusCounter, GameCounterRollOptions.None);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 4 + 3 (2d6) + (-1) (unskilled) + 1 (dex bonus) = 7
        result.Roll.ShouldBe(7);
        result.Success.ShouldBe(false); // 7 < 8
        result.CounterName.ShouldContain("unskilled");
        result.CounterName.ShouldNotContain("unfamiliar"); // No unfamiliarity penalty for skill checks
    }

    [Fact]
    public void CustomCounter_UnskilledAttack_HasUnfamiliarityPenalty()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Set up basic character with attack bonus
        _playerSystem.GetProperty<GameCounter>(sheet, SwnSystem.Level).EditCharacterProperty("1", sheet);
        _playerSystem.GetProperty<GameProperty>(sheet, "Class 1").EditCharacterProperty(SwnSystem.Expert, sheet);
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("14", sheet);

        // Add custom counter with unskilled default value (-1)
        var customCounter = new CustomCounter("TestWeapon", -1, GameCounterOptions.CanRoll);
        sheet.CustomCounters = [customCounter];
        var weaponCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestWeapon");
        // Don't set the value explicitly, it will use the default value of -1

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Set up predictable dice roll for 1d20 attack
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 12);

        // Act - Unskilled attack roll (should get both -1 unskilled and -2 unfamiliarity penalties)
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));
        int? targetAC = 15;
        var result = weaponCounter.Roll(adventurer, null, mockRandom, 0, ref targetAC, dexBonusCounter, GameCounterRollOptions.IsAttack);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 12 (d20) + (-1) (unskilled) + (-2) (unfamiliar) + 1 (dex bonus) + 0 (attack bonus) = 10
        result.Roll.ShouldBe(10);
        result.Success.ShouldBe(false); // 10 < 15
        result.CounterName.ShouldContain("unskilled");
        result.CounterName.ShouldContain("unfamiliar");
    }

    [Fact]
    public void CustomCounter_MinimumValue_IsZeroNotNegativeOne()
    {
        // Arrange
        var customCounter = new CustomCounter("TestSkill", 0, GameCounterOptions.CanRoll);

        // Act - Create the SwnCustomCounter
        var swnCustomCounter = new SwnCustomCounter(customCounter, null);

        // Assert - Minimum value should be 0, not -1 like regular skills
        swnCustomCounter.MinValue.ShouldBe(0);
        swnCustomCounter.DefaultValue.ShouldBe(0);
    }

    [Fact]
    public void CustomCounter_WithNegativeDefaultValue_AdjustsMinimumCorrectly()
    {
        // Arrange
        var customCounter = new CustomCounter("TestSkill", -2, GameCounterOptions.CanRoll);

        // Act - Create the SwnCustomCounter
        var swnCustomCounter = new SwnCustomCounter(customCounter, null);

        // Assert - Minimum value should be the smaller of 0 and the default value
        swnCustomCounter.MinValue.ShouldBe(-2);
        swnCustomCounter.DefaultValue.ShouldBe(-2);
    }

    [Fact]
    public void CustomCounter_VariableCounter_TracksCurrentAndMaxValues()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with HasVariable option
        var customCounter = new CustomCounter("TestResource", 10, GameCounterOptions.HasVariable);
        sheet.CustomCounters = [customCounter];
        var resourceCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestResource");

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Act & Assert - Test initial values
        var startingValue = resourceCounter.GetStartingValue(adventurer);
        var maxValue = resourceCounter.GetMaxVariableValue(adventurer);
        var currentValue = resourceCounter.GetVariableValue(adventurer);

        startingValue.ShouldBe(10);
        maxValue.ShouldBe(10);
        currentValue.ShouldBe(10); // Should start at starting value

        // Test setting variable value
        resourceCounter.SetVariableClamped(7, adventurer);
        var newCurrentValue = resourceCounter.GetVariableValue(adventurer);
        newCurrentValue.ShouldBe(7);

        // Max should remain unchanged
        resourceCounter.GetMaxVariableValue(adventurer).ShouldBe(10);
    }

    [Fact]
    public void CustomCounter_VariableCounter_ClampsToValidRange()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with HasVariable option, range 0-5
        var customCounter = new CustomCounter("TestResource", 3, GameCounterOptions.HasVariable);
        sheet.CustomCounters = [customCounter];
        var resourceCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestResource");

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Act - Try to set values outside the valid range
        resourceCounter.SetVariableClamped(-5, adventurer); // Below minimum
        var clampedLow = resourceCounter.GetVariableValue(adventurer);

        resourceCounter.SetVariableClamped(10, adventurer); // Above maximum
        var clampedHigh = resourceCounter.GetVariableValue(adventurer);

        // Assert - Values should be clamped to valid range
        clampedLow.ShouldBe(0); // Clamped to minimum value (0)
        clampedHigh.ShouldBe(3); // Clamped to maximum value (should be the default value 3 for custom counters)
    }

    [Fact]
    public void CustomCounter_BothOptions_FunctionsAsRollableAndVariable()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        SwnTestHelpers.SetupFullPlayerCharacter(player, _playerSystem);
        var sheet = player.Sheet;

        // Add custom counter with both CanRoll and HasVariable options
        var customCounter = new CustomCounter("TestAmmo", 6, GameCounterOptions.CanRoll | GameCounterOptions.HasVariable);
        sheet.CustomCounters = [customCounter];
        var ammoCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestAmmo");
        ammoCounter.EditCharacterProperty("2", sheet); // Set skill level to 2

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Test variable functionality
        var initialAmmo = ammoCounter.GetVariableValue(adventurer);
        initialAmmo.ShouldBe(2); // Should start at the skill level set above (2)

        // Test setting variable value (clamped to skill level max)
        ammoCounter.SetVariableClamped(1, adventurer);
        var remainingAmmo = ammoCounter.GetVariableValue(adventurer);
        remainingAmmo.ShouldBe(1); // Should be set to 1 (within skill level max of 2)

        // Test rollable functionality
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 2);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? skillTarget = 9;
        var rollResult = ammoCounter.Roll(adventurer, null, mockRandom, 0, ref skillTarget, dexBonusCounter, GameCounterRollOptions.None);

        // Assert roll functionality
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 4 + 2 (2d6) + 2 (skill level) + 1 (dex bonus) = 9
        rollResult.Roll.ShouldBe(9);
        rollResult.Success.ShouldBe(true); // 9 >= 9
        rollResult.CounterName.ShouldBe("TestAmmo (DEX)");
    }

    [Fact]
    public void CustomCounter_MonsterCustomCounter_CannotAttackWithoutAttackBonus()
    {
        // Arrange
        var monster = SwnTestHelpers.CreateMonster();
        _monsterSystem.SetNewCharacterStartingValues(monster);
        var sheet = monster.Sheet;

        // Add custom counter for monster
        var customCounter = new CustomCounter("TestSkill", 1, GameCounterOptions.CanRoll);
        sheet.CustomCounters = [customCounter];
        var skillCounter = _monsterSystem.GetProperty<GameCounter>(sheet, "TestSkill");

        var monsterCombatant = SwnTestHelpers.CreateMonsterCombatant();
        monsterCombatant.Sheet = sheet;

        // Set up predictable dice roll for 2d6 skill check
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 4);

        // Act - Monster skill check (not attack) should work
        int? skillTarget = 8;
        var skillResult = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref skillTarget, null, GameCounterRollOptions.None);

        // Assert skill check works
        skillResult.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 3 + 4 (2d6) + 1 (skill level) = 8
        skillResult.Roll.ShouldBe(8);
        skillResult.Success.ShouldBe(true); // 8 >= 8
        skillResult.CounterName.ShouldBe("TestSkill");

        // Act - Monster attack roll should fail (no attack bonus counter)
        int? targetAC = 15;
        var attackResult = skillCounter.Roll(monsterCombatant, null, mockRandom, 0, ref targetAC, null, GameCounterRollOptions.IsAttack);

        // Assert attack roll fails
        attackResult.Error.ShouldBe(GameCounterRollError.NotRollable);
        attackResult.Working.ShouldContain("Cannot make attack rolls without AttackBonusCounter");
    }

    [Fact]
    public void CustomCounter_GetValue_ReturnsDefaultWhenNull()
    {
        // Arrange
        var customCounter = new CustomCounter("TestSkill", 2, GameCounterOptions.CanRoll);
        var swnCustomCounter = new SwnCustomCounter(customCounter, null);

        var sheet = new CharacterSheet();
        var character = SwnTestHelpers.CreatePlayerCharacter();
        character.Sheet = sheet;

        // Act - Get value when no value is set (should return default)
        var sheetValue = swnCustomCounter.GetValue(sheet);
        var characterValue = swnCustomCounter.GetValue(character.Sheet);

        // Assert
        sheetValue.ShouldBe(2); // Should return default value
        characterValue.ShouldBe(2); // Should return default value
    }

    [Theory]
    [InlineData(GameCounterOptions.None)]
    [InlineData(GameCounterOptions.CanRoll)]
    [InlineData(GameCounterOptions.HasVariable)]
    [InlineData(GameCounterOptions.CanRoll | GameCounterOptions.HasVariable)]
    [InlineData(GameCounterOptions.IsHidden)]
    [InlineData(GameCounterOptions.CanRoll | GameCounterOptions.HasVariable | GameCounterOptions.IsHidden)]
    public void CustomCounter_DifferentOptions_CreatedCorrectly(GameCounterOptions options)
    {
        // Arrange
        var customCounter = new CustomCounter("TestCounter", 5, options);

        // Act
        var swnCustomCounter = new SwnCustomCounter(customCounter, null);

        // Assert
        swnCustomCounter.Name.ShouldBe("TestCounter");
        swnCustomCounter.DefaultValue.ShouldBe(5);
        swnCustomCounter.Options.ShouldBe(options);
        swnCustomCounter.MinValue.ShouldBe(0); // Custom counters should have min of 0
        swnCustomCounter.MaxValue.ShouldBe(Math.Max(0, 5)); // Should be Math.Max(0, defaultValue)
    }

    [Fact]
    public void CustomCounter_RollableWithFix_AddsFixToCounterValue()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with rollable option, set to level 1
        var customCounter = new CustomCounter("TestSkill", 1, GameCounterOptions.CanRoll);
        sheet.CustomCounters = [customCounter];
        var skillCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestSkill");
        skillCounter.EditCharacterProperty("1", sheet);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Add a fix of +2 to the custom counter
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("TestSkill", 2));

        // Act - Get value (should include fix)
        var valueWithFix = skillCounter.GetValue(adventurer);

        // Assert - Should be base value (1) + fix (2) = 3
        valueWithFix.ShouldBe(3);
    }

    [Fact]
    public void CustomCounter_RollableWithFixUsesDefaultValue_AddsFixToDefaultValue()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with rollable option, default value 2, but don't set explicit value
        var customCounter = new CustomCounter("TestSkill", 2, GameCounterOptions.CanRoll);
        sheet.CustomCounters = [customCounter];
        var skillCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestSkill");

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Add a fix of +1 to the custom counter
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("TestSkill", 1));

        // Act - Get value (should use default value + fix)
        var valueWithFix = skillCounter.GetValue(adventurer);

        // Assert - Should be default value (2) + fix (1) = 3
        valueWithFix.ShouldBe(3);
    }

    [Fact]
    public void CustomCounter_RollableWithFix_UsesFixInSkillChecks()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with rollable option, set to level 1
        var customCounter = new CustomCounter("TestSkill", 1, GameCounterOptions.CanRoll);
        sheet.CustomCounters = [customCounter];
        var skillCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestSkill");
        skillCounter.EditCharacterProperty("1", sheet);

        // Set Dexterity for attribute bonus
        _playerSystem.GetProperty<GameAbilityCounter>(sheet, SwnSystem.Dexterity).EditCharacterProperty("14", sheet);

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Add a fix of +1 to the custom counter
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("TestSkill", 1));

        // Set up predictable dice rolls for 2d6 skill check
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 3, 6, 4);

        // Act - Skill check with fix applied
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));
        int? skillTarget = 10;
        var result = skillCounter.Roll(adventurer, null, mockRandom, 0, ref skillTarget, dexBonusCounter, GameCounterRollOptions.None);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 3 + 4 (2d6) + (1 base + 1 fix) + 1 (dex bonus) = 10
        result.Roll.ShouldBe(10);
        result.Success.ShouldBe(true); // 10 >= 10
        result.CounterName.ShouldBe("TestSkill (DEX)");
    }

    [Fact]
    public void CustomCounter_VariableWithFix_FixAffectsMaxAndStartingValue()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with variable option, default value 5
        var customCounter = new CustomCounter("TestResource", 5, GameCounterOptions.HasVariable);
        sheet.CustomCounters = [customCounter];
        var resourceCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestResource");

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Add a fix of +2 to the custom counter
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("TestResource", 2));

        // Act & Assert - Check max and starting values with fix
        var maxValue = resourceCounter.GetMaxVariableValue(adventurer);
        var startingValue = resourceCounter.GetStartingValue(adventurer);
        var currentValue = resourceCounter.GetVariableValue(adventurer);

        maxValue.ShouldBe(7); // 5 (default) + 2 (fix) = 7
        startingValue.ShouldBe(7); // 5 (default) + 2 (fix) = 7
        currentValue.ShouldBe(7); // Should start at starting value
    }

    [Fact]
    public void CustomCounter_VariableWithFix_FixDoesNotAffectSetVariableValue()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with variable option, default value 5
        var customCounter = new CustomCounter("TestResource", 5, GameCounterOptions.HasVariable);
        sheet.CustomCounters = [customCounter];
        var resourceCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestResource");

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // First set variable value to 3
        resourceCounter.SetVariableClamped(3, adventurer);
        var valueBeforeFix = resourceCounter.GetVariableValue(adventurer);
        valueBeforeFix.ShouldBe(3);

        // Then add a fix of +2 to the custom counter
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("TestResource", 2));

        // Act & Assert - Variable value should be unchanged, but max should be affected
        var currentValue = resourceCounter.GetVariableValue(adventurer);
        var maxValue = resourceCounter.GetMaxVariableValue(adventurer);

        currentValue.ShouldBe(3); // Should remain unchanged
        maxValue.ShouldBe(7); // 5 (default) + 2 (fix) = 7
    }

    [Fact]
    public void CustomCounter_VariableWithFix_ResetUsesFixedStartingValue()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with variable option, default value 4
        var customCounter = new CustomCounter("TestResource", 4, GameCounterOptions.HasVariable);
        sheet.CustomCounters = [customCounter];
        var resourceCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestResource");

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Add a fix of +3 to the custom counter
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("TestResource", 3));

        // Set variable to a low value
        resourceCounter.SetVariableClamped(2, adventurer);
        var lowValue = resourceCounter.GetVariableValue(adventurer);
        lowValue.ShouldBe(2);

        // Act - Reset variable (by setting to starting value)
        var startingValue = resourceCounter.GetStartingValue(adventurer);
        resourceCounter.SetVariableClamped(startingValue!.Value, adventurer);

        // Assert - Should reset to fixed starting value
        var resetValue = resourceCounter.GetVariableValue(adventurer);
        resetValue.ShouldBe(7); // 4 (default) + 3 (fix) = 7
    }

    [Fact]
    public void CustomCounter_BothOptionsWithFix_WorksForBothRollableAndVariable()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        SwnTestHelpers.SetupFullPlayerCharacter(player, _playerSystem);
        var sheet = player.Sheet;

        // Add custom counter with both options, default value 3
        var customCounter = new CustomCounter("TestAmmo", 3, GameCounterOptions.CanRoll | GameCounterOptions.HasVariable);
        sheet.CustomCounters = [customCounter];
        var ammoCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestAmmo");
        ammoCounter.EditCharacterProperty("2", sheet); // Set skill level to 2

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Add a fix of +1 to the custom counter
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("TestAmmo", 1));

        // Test variable functionality with fix
        var maxValue = ammoCounter.GetMaxVariableValue(adventurer);
        var startingValue = ammoCounter.GetStartingValue(adventurer);
        var currentValue = ammoCounter.GetVariableValue(adventurer);

        // For rollable+variable counters, max is the skill level + fix
        maxValue.ShouldBe(3); // skill level (2) + fix (1) = 3
        startingValue.ShouldBe(3); // skill level (2) + fix (1) = 3
        currentValue.ShouldBe(3); // Should start at starting value

        // Test rollable functionality with fix
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 2, 6, 3);
        var dexBonusCounter = _playerSystem.GetProperty<GameCounter>(sheet, AttributeBonusCounter.GetBonusCounterName(SwnSystem.Dexterity));

        int? skillTarget = 8;
        var rollResult = ammoCounter.Roll(adventurer, null, mockRandom, 0, ref skillTarget, dexBonusCounter, GameCounterRollOptions.None);

        // Assert roll functionality includes fix
        rollResult.Error.ShouldBe(GameCounterRollError.Success);
        // Expected: 2 + 3 (2d6) + (2 skill + 1 fix) + 1 (dex bonus) = 9
        rollResult.Roll.ShouldBe(9);
        rollResult.Success.ShouldBe(true); // 9 >= 8
        rollResult.CounterName.ShouldBe("TestAmmo (DEX)");
    }

    [Fact]
    public void CustomCounter_NegativeFix_ReducesCounterValues()
    {
        // Arrange
        var player = SwnTestHelpers.CreatePlayerCharacter();
        _playerSystem.SetNewCharacterStartingValues(player);
        var sheet = player.Sheet;

        // Add custom counter with both options, default value 4
        var customCounter = new CustomCounter("TestSkill", 4, GameCounterOptions.CanRoll | GameCounterOptions.HasVariable);
        sheet.CustomCounters = [customCounter];
        var skillCounter = _playerSystem.GetProperty<GameCounter>(sheet, "TestSkill");
        skillCounter.EditCharacterProperty("3", sheet); // Set skill level to 3

        var adventurer = SwnTestHelpers.CreateAdventurer();
        adventurer.Sheet = sheet;

        // Add a negative fix of -1 to the custom counter
        adventurer.GetFixes().Counters.Add(new PropertyValue<int>("TestSkill", -1));

        // Act & Assert - Test rollable value with negative fix
        var skillValue = skillCounter.GetValue(adventurer);
        skillValue.ShouldBe(2); // 3 (skill level) + (-1 fix) = 2

        // Test variable values with negative fix
        var maxValue = skillCounter.GetMaxVariableValue(adventurer);
        var startingValue = skillCounter.GetStartingValue(adventurer);

        maxValue.ShouldBe(2); // 3 (skill level) + (-1 fix) = 2
        startingValue.ShouldBe(2); // 3 (skill level) + (-1 fix) = 2
    }
}