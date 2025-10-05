using System.Text;
using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;

namespace ThirteenIsh.Tests.Game.Swn;

/// <summary>
/// Integration tests for SWN starship character creation and combat operations.
/// Tests that starships work as monster-type characters in encounters with their unique properties.
/// </summary>
public class SwnStarshipIntegrationTests
{
    private readonly SwnSystem _gameSystem;
    private readonly SwnCharacterSystem _starshipSystem;

    public SwnStarshipIntegrationTests()
    {
        _gameSystem = SwnTestHelpers.CreateSwnSystem();
        _starshipSystem = (SwnCharacterSystem)_gameSystem.GetCharacterSystem(CharacterType.Monster, SwnSystem.Starship);
    }

    #region Starship Creation & Defaults

    [Theory]
    [InlineData(SwnSystem.Fighter, 8, 16, 5, 5, 2, 5, 2, 1, 4, 4)]
    [InlineData(SwnSystem.Frigate, 25, 14, 5, 4, 2, 15, 10, 20, 5, 4)]
    [InlineData(SwnSystem.Cruiser, 60, 14, 15, 1, 2, 50, 30, 200, 5, 5)]
    [InlineData(SwnSystem.Capital, 120, 17, 20, 0, 3, 75, 50, 1000, 6, 6)]
    public void Starship_DefaultValues_SetCorrectlyForHullClass(string hullClass, int expectedHP, int expectedAC,
        int expectedArmor, int expectedSpeed, int expectedSkill, int expectedPower, int expectedMass,
        int expectedCrew, int expectedCP, int expectedWeapons)
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();

        // Act
        _starshipSystem.GetProperty<GameProperty>(starship, SwnSystem.HullClass).EditCharacterProperty(hullClass, starship);
        _starshipSystem.SetNewCharacterStartingValues(starship);

        // Assert
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).GetValue(starship).ShouldBe(expectedHP);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.ArmorClass).GetValue(starship).ShouldBe(expectedAC);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Armor).GetValue(starship).ShouldBe(expectedArmor);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Speed).GetValue(starship).ShouldBe(expectedSpeed);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Skill).GetValue(starship).ShouldBe(expectedSkill);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Power).GetValue(starship).ShouldBe(expectedPower);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Mass).GetValue(starship).ShouldBe(expectedMass);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Crew).GetValue(starship).ShouldBe(expectedCrew);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).GetValue(starship).ShouldBe(expectedCP);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Weapons).GetValue(starship).ShouldBe(expectedWeapons);
    }

    [Fact]
    public void Starship_DefaultHullClass_StartsAsFighter()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();

        // Act
        _starshipSystem.SetNewCharacterStartingValues(starship);

        // Assert - Should get Fighter defaults when no hull class set
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).GetValue(starship).ShouldBe(8);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).GetValue(starship).ShouldBe(4);
    }

    #endregion

    #region Hull Class Behavior

    [Fact]
    public void Starship_HullClassChange_DoesNotAffectOtherProperties()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.GetProperty<GameProperty>(starship, SwnSystem.HullClass).EditCharacterProperty(SwnSystem.Fighter, starship);
        _starshipSystem.SetNewCharacterStartingValues(starship);

        // Verify we have Fighter defaults
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).GetValue(starship).ShouldBe(8);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).GetValue(starship).ShouldBe(4);

        // Act - Change hull class to Capital
        _starshipSystem.GetProperty<GameProperty>(starship, SwnSystem.HullClass).EditCharacterProperty(SwnSystem.Capital, starship);

        // Assert - HP and CP should NOT change to Capital defaults (120 HP, 6 CP)
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).GetValue(starship).ShouldBe(8);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).GetValue(starship).ShouldBe(4);
    }

    [Fact]
    public void Starship_HullClass_IsInformationalOnly()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);

        // Manually set custom values
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).EditCharacterProperty("100", starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).EditCharacterProperty("10", starship);

        // Act - Change hull class
        _starshipSystem.GetProperty<GameProperty>(starship, SwnSystem.HullClass).EditCharacterProperty(SwnSystem.Frigate, starship);

        // Assert - Custom values should remain unchanged
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).GetValue(starship).ShouldBe(100);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).GetValue(starship).ShouldBe(10);
    }

    #endregion

    #region Counter Editability

    [Fact]
    public void Starship_AllCounters_CanBeEdited()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);

        // Act & Assert - Test all counters can be edited
        var hpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints);
        hpCounter.EditCharacterProperty("50", starship);
        hpCounter.GetValue(starship).ShouldBe(50);

        var acCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.ArmorClass);
        acCounter.EditCharacterProperty("18", starship);
        acCounter.GetValue(starship).ShouldBe(18);

        var armorCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Armor);
        armorCounter.EditCharacterProperty("12", starship);
        armorCounter.GetValue(starship).ShouldBe(12);

        var speedCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Speed);
        speedCounter.EditCharacterProperty("3", starship);
        speedCounter.GetValue(starship).ShouldBe(3);

        var skillCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Skill);
        skillCounter.EditCharacterProperty("4", starship);
        skillCounter.GetValue(starship).ShouldBe(4);

        var powerCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Power);
        powerCounter.EditCharacterProperty("100", starship);
        powerCounter.GetValue(starship).ShouldBe(100);

        var massCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Mass);
        massCounter.EditCharacterProperty("40", starship);
        massCounter.GetValue(starship).ShouldBe(40);

        var crewCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Crew);
        crewCounter.EditCharacterProperty("500", starship);
        crewCounter.GetValue(starship).ShouldBe(500);

        var cpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints);
        cpCounter.EditCharacterProperty("8", starship);
        cpCounter.GetValue(starship).ShouldBe(8);

        var weaponsCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Weapons);
        weaponsCounter.EditCharacterProperty("7", starship);
        weaponsCounter.GetValue(starship).ShouldBe(7);
    }

    [Fact]
    public void Starship_CounterEdits_PersistRegardlessOfHullClass()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.GetProperty<GameProperty>(starship, SwnSystem.HullClass).EditCharacterProperty(SwnSystem.Fighter, starship);
        _starshipSystem.SetNewCharacterStartingValues(starship);

        // Edit counters to non-default values
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).EditCharacterProperty("99", starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).EditCharacterProperty("9", starship);

        // Act - Change hull class
        _starshipSystem.GetProperty<GameProperty>(starship, SwnSystem.HullClass).EditCharacterProperty(SwnSystem.Capital, starship);

        // Assert - Edits should persist
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).GetValue(starship).ShouldBe(99);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).GetValue(starship).ShouldBe(9);
    }

    #endregion

    #region Fixes System

    [Fact]
    public void Starship_WeaponsFix_AltersAttackRoll()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Weapons).EditCharacterProperty("3", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 12);

        // Act - Attack without fix
        var weaponsCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Weapons);
        int? targetAC = 15;
        var resultNoFix = weaponsCounter.Roll(combatant, null, mockRandom, 0, ref targetAC);

        // Apply fix
        combatant.GetFixes().Counters.SetValue(SwnSystem.Weapons, 2);
        mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 12);
        targetAC = 15;
        var resultWithFix = weaponsCounter.Roll(combatant, null, mockRandom, 0, ref targetAC);

        // Assert
        resultNoFix.Roll.ShouldBe(15); // 12 + 3
        resultWithFix.Roll.ShouldBe(17); // 12 + 3 + 2 (fix)
    }

    [Fact]
    public void Starship_HitPointsFix_AltersMaximumVariable()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).EditCharacterProperty("20", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        // Reset variables to initialize HP
        _starshipSystem.ResetVariables(combatant);

        var hpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints);

        // Act - Apply fix
        combatant.GetFixes().Counters.SetValue(SwnSystem.HitPoints, 5);

        // Assert
        hpCounter.GetMaxVariableValue(combatant).ShouldBe(25); // 20 + 5
        hpCounter.GetVariableValue(combatant).ShouldBe(20); // Variable doesn't auto-update, stays at old value
    }

    [Fact]
    public void Starship_CommandPointsFix_AltersMaximumVariable()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).EditCharacterProperty("5", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        _starshipSystem.ResetVariables(combatant);

        var cpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints);

        // Act - Apply fix
        combatant.GetFixes().Counters.SetValue(SwnSystem.CommandPoints, 3);

        // Assert
        cpCounter.GetMaxVariableValue(combatant).ShouldBe(8); // 5 + 3
        cpCounter.GetVariableValue(combatant).ShouldBe(5); // Variable doesn't auto-update
    }

    [Fact]
    public void Starship_VariableCounterFix_CompleteScenario()
    {
        // This test follows the scenario from the user's clarification:
        // - Starship with HP value 4
        // - Apply Fix 1 -- starship now has HP value 5
        // - Begin combat and add starship to combat -- Starship appears with HP 5/5
        // - Modify the HP variable value by -1 : starship now appears with HP 4/5
        // - Change the HP Fix to 2 -- starship now appears with HP 4/6

        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).EditCharacterProperty("4", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        var hpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints);

        // Apply Fix 1
        combatant.GetFixes().Counters.SetValue(SwnSystem.HitPoints, 1);
        hpCounter.GetMaxVariableValue(combatant).ShouldBe(5); // 4 + 1

        // Begin combat and add starship (reset variables)
        _starshipSystem.ResetVariables(combatant);
        hpCounter.GetVariableValue(combatant).ShouldBe(5); // Starting value = 4 + 1
        hpCounter.GetMaxVariableValue(combatant).ShouldBe(5);

        // Modify HP variable by -1
        hpCounter.SetVariableClamped(4, combatant);
        hpCounter.GetVariableValue(combatant).ShouldBe(4);
        hpCounter.GetMaxVariableValue(combatant).ShouldBe(5);

        // Change the HP Fix to 2
        combatant.GetFixes().Counters.SetValue(SwnSystem.HitPoints, 2);
        hpCounter.GetVariableValue(combatant).ShouldBe(4); // Variable stays the same
        hpCounter.GetMaxVariableValue(combatant).ShouldBe(6); // 4 + 2
    }

    #endregion

    #region Character Summary

    [Theory]
    [InlineData(SwnSystem.Fighter, 4, "Fighter class starship (4 CP)")]
    [InlineData(SwnSystem.Frigate, 5, "Frigate class starship (5 CP)")]
    [InlineData(SwnSystem.Cruiser, 5, "Cruiser class starship (5 CP)")]
    [InlineData(SwnSystem.Capital, 6, "Capital class starship (6 CP)")]
    public void Starship_CharacterSummary_ShowsHullClassAndCP(string hullClass, int expectedCP, string expectedSummary)
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.GetProperty<GameProperty>(starship, SwnSystem.HullClass).EditCharacterProperty(hullClass, starship);
        _starshipSystem.SetNewCharacterStartingValues(starship);

        // Act
        var summary = _gameSystem.GetCharacterSummary(starship);

        // Assert
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).GetValue(starship).ShouldBe(expectedCP);
        summary.ShouldBe(expectedSummary);
    }

    [Fact]
    public void Starship_CharacterSummary_ReflectsModifiedCP()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.GetProperty<GameProperty>(starship, SwnSystem.HullClass).EditCharacterProperty(SwnSystem.Fighter, starship);
        _starshipSystem.SetNewCharacterStartingValues(starship);

        // Modify CP
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).EditCharacterProperty("10", starship);

        // Act
        var summary = _gameSystem.GetCharacterSummary(starship);

        // Assert
        summary.ShouldBe("Fighter class starship (10 CP)");
    }

    #endregion

    #region Property Groups

    [Fact]
    public void Starship_PropertyGroups_ContainsStarshipStats()
    {
        // Arrange & Act
        var propertyGroups = _starshipSystem.GetPropertyGroups();

        // Assert
        propertyGroups.ShouldNotBeEmpty();
        var starshipStatsGroup = propertyGroups.FirstOrDefault(g => g.GroupName == SwnSystem.StarshipStats);
        starshipStatsGroup.ShouldNotBeNull();

        var propertyNames = starshipStatsGroup.Properties.Select(p => p.Name).ToList();
        propertyNames.ShouldContain(SwnSystem.HullClass);
        propertyNames.ShouldContain(SwnSystem.HitPoints);
        propertyNames.ShouldContain(SwnSystem.ArmorClass);
        propertyNames.ShouldContain(SwnSystem.Armor);
        propertyNames.ShouldContain(SwnSystem.Speed);
        propertyNames.ShouldContain(SwnSystem.Skill);
        propertyNames.ShouldContain(SwnSystem.Power);
        propertyNames.ShouldContain(SwnSystem.Mass);
        propertyNames.ShouldContain(SwnSystem.Crew);
        propertyNames.ShouldContain(SwnSystem.CommandPoints);
        propertyNames.ShouldContain(SwnSystem.Weapons);
    }

    [Fact]
    public void Starship_PropertyGroups_OnlyContainsStarshipStats()
    {
        // Arrange & Act
        var propertyGroups = _starshipSystem.GetPropertyGroups();

        // Assert - Should only have StarshipStats, no player or regular monster properties
        propertyGroups.Count().ShouldBe(1);
        propertyGroups.Single().GroupName.ShouldBe(SwnSystem.StarshipStats);

        var allPropertyNames = propertyGroups.SelectMany(g => g.Properties).Select(p => p.Name).ToList();

        // Should NOT have monster-specific properties
        allPropertyNames.ShouldNotContain(SwnSystem.HitDice);
        allPropertyNames.ShouldNotContain(SwnSystem.Morale);

        // Should NOT have player-specific properties
        allPropertyNames.ShouldNotContain(SwnSystem.Level);
        allPropertyNames.ShouldNotContain(SwnSystem.Strength);
    }

    #endregion

    #region Combat Operations

    [Fact]
    public void Starship_EncounterAdd_AddsToEncounterWithInitiative()
    {
        // Arrange
        var encounter = SwnTestHelpers.CreateEncounter();
        var nameAliases = new NameAliasCollection(encounter);

        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);

        var mockRandom = SwnTestHelpers.CreatePredictableRandom(8, 5); // Initiative roll

        // Act
        var result = _gameSystem.EncounterAdd(starship, encounter, nameAliases, mockRandom, 0, 1, 12345);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        result.Roll.ShouldBe(5); // Initiative = 5 (d8 for monsters)
        encounter.Combatants.Count().ShouldBe(1);
        encounter.Combatants.First().Initiative.ShouldBe(5);
        encounter.Combatants.First().CharacterSystemName.ShouldBe(SwnSystem.Starship);
    }

    [Fact]
    public void Starship_WeaponsAttack_WorksLikeMonsterAttack()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Weapons).EditCharacterProperty("4", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        var mockRandom = SwnTestHelpers.CreatePredictableRandom(20, 15);

        // Act
        var weaponsCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Weapons);
        int? targetAC = 18;
        var result = weaponsCounter.Roll(combatant, null, mockRandom, 0, ref targetAC);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        result.Roll.ShouldBe(19); // 15 (d20) + 4 (weapons)
        result.Success.ShouldBe(true); // 19 >= 18
    }

    [Fact]
    public void Starship_SkillCheck_WorksLikeMonsterSkill()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Skill).EditCharacterProperty("2", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        var mockRandom = SwnTestHelpers.CreatePredictableRandom(6, 4, 6, 3);

        // Act
        var skillCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.Skill);
        int? target = 9;
        var result = skillCounter.Roll(combatant, null, mockRandom, 0, ref target);

        // Assert
        result.Error.ShouldBe(GameCounterRollError.Success);
        result.Roll.ShouldBe(9); // 4 + 3 (2d6) + 2 (skill)
        result.Success.ShouldBe(true); // 9 >= 9
    }

    #endregion

    #region Encounter Table Display

    [Fact]
    public void Starship_EncounterTableCounters_IncludesHPAndCP()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);

        // Act
        var encounterCounters = _starshipSystem.GetEncounterTableCounters(starship.Sheet).ToList();

        // Assert
        encounterCounters.ShouldNotBeEmpty();

        var hpCounter = encounterCounters.FirstOrDefault(c => c.Name == SwnSystem.HitPoints);
        hpCounter.ShouldNotBeNull();
        hpCounter.Alias.ShouldBe(SwnSystem.HitPointsAlias); // "HP"

        var cpCounter = encounterCounters.FirstOrDefault(c => c.Name == SwnSystem.CommandPoints);
        cpCounter.ShouldNotBeNull();
        cpCounter.Alias.ShouldBe(SwnSystem.CommandPointsAlias); // "CP"
    }

    [Fact]
    public async Task Starship_InitiativeTable_DisplaysHPAndCP()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship("USS Enterprise");
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).EditCharacterProperty("50", starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).EditCharacterProperty("6", starship);

        var encounter = SwnTestHelpers.CreateEncounter();
        var nameAliases = new NameAliasCollection(encounter);
        var mockRandom = SwnTestHelpers.CreatePredictableRandom(8, 7);

        _gameSystem.EncounterAdd(starship, encounter, nameAliases, mockRandom, 0, 1, 12345);

        var combatant = encounter.Combatants.First() as MonsterCombatant;
        combatant.ShouldNotBeNull();
        _starshipSystem.ResetVariables(combatant);

        // Damage the starship
        var hpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints);
        hpCounter.SetVariableClamped(40, combatant);

        // Spend command points
        var cpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints);
        cpCounter.SetVariableClamped(4, combatant);

        var mockService = SwnTestHelpers.CreateMockCharacterDataService(combatant);

        // Act
        var tableBuilder = new StringBuilder();
        await _gameSystem.BuildEncounterInitiativeTableAsync(mockService, tableBuilder, encounter);
        var table = tableBuilder.ToString();

        // Assert - The table contains the HP and CP values in the correct format
        // The output looks like: "UsEnt1··7··HP 40/50··CP 4/6"
        table.ShouldContain("HP");
        table.ShouldContain("40/50"); // Current/max HP
        table.ShouldContain("CP");
        table.ShouldContain("4/6");   // Current/max CP

        // Verify both counters appear in the table (order doesn't matter)
        table.ShouldMatch(@"HP.*40/50"); // HP followed by the value
        table.ShouldMatch(@"CP.*4/6");   // CP followed by the value
    }

    #endregion

    #region Variable Tracking

    [Fact]
    public void Starship_HitPointsVariable_TrackedIndependently()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).EditCharacterProperty("30", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        _starshipSystem.ResetVariables(combatant);

        var hpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints);

        // Act - Take damage
        hpCounter.SetVariableClamped(20, combatant);

        // Assert
        hpCounter.GetValue(combatant).ShouldBe(30); // Max HP unchanged
        hpCounter.GetVariableValue(combatant).ShouldBe(20); // Current HP reduced
    }

    [Fact]
    public void Starship_CommandPointsVariable_TrackedIndependently()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).EditCharacterProperty("6", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        _starshipSystem.ResetVariables(combatant);

        var cpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints);

        // Act - Spend CP
        cpCounter.SetVariableClamped(3, combatant);

        // Assert
        cpCounter.GetValue(combatant).ShouldBe(6); // Max CP unchanged
        cpCounter.GetVariableValue(combatant).ShouldBe(3); // Current CP reduced
    }

    [Fact]
    public void Starship_Variables_ClampCorrectlyToMax()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).EditCharacterProperty("20", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        _starshipSystem.ResetVariables(combatant);

        var hpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints);

        // Act - Try to set HP above max
        hpCounter.SetVariableClamped(100, combatant);

        // Assert - Should clamp to max
        hpCounter.GetVariableValue(combatant).ShouldBe(20);
    }

    [Fact]
    public void Starship_ResetVariables_SetsToStartingValue()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).EditCharacterProperty("25", starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).EditCharacterProperty("5", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        var hpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints);
        var cpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints);

        // Act
        _starshipSystem.ResetVariables(combatant);

        // Assert
        hpCounter.GetVariableValue(combatant).ShouldBe(25);
        cpCounter.GetVariableValue(combatant).ShouldBe(5);
    }

    [Fact]
    public void Starship_ResetVariables_WithFixes_SetsToStartingPlusFix()
    {
        // Arrange
        var starship = SwnTestHelpers.CreateStarship();
        _starshipSystem.SetNewCharacterStartingValues(starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints).EditCharacterProperty("20", starship);
        _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints).EditCharacterProperty("4", starship);

        var combatant = SwnTestHelpers.CreateMonsterCombatant("TestStarship", starship.Sheet);
        combatant.CharacterSystemName = SwnSystem.Starship;

        // Apply fixes before reset
        combatant.GetFixes().Counters.SetValue(SwnSystem.HitPoints, 10);
        combatant.GetFixes().Counters.SetValue(SwnSystem.CommandPoints, 2);

        var hpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.HitPoints);
        var cpCounter = _starshipSystem.GetProperty<GameCounter>(starship, SwnSystem.CommandPoints);

        // Act
        _starshipSystem.ResetVariables(combatant);

        // Assert - Starting value should include fixes
        hpCounter.GetVariableValue(combatant).ShouldBe(30); // 20 + 10
        cpCounter.GetVariableValue(combatant).ShouldBe(6);  // 4 + 2
    }

    #endregion
}
