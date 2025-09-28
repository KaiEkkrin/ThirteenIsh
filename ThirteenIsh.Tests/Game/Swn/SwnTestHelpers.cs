using NSubstitute;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Game.Swn;
using ThirteenIsh.Tests.Mocks;

namespace ThirteenIsh.Tests.Game.Swn;

/// <summary>
/// Helper methods for creating SWN characters and related objects for testing.
/// </summary>
internal static class SwnTestHelpers
{
    /// <summary>
    /// Creates a fully configured SWN game system.
    /// </summary>
    public static SwnSystem CreateSwnSystem() => SwnSystem.Build();

    /// <summary>
    /// Creates a basic player character with the SWN system.
    /// </summary>
    public static Character CreatePlayerCharacter(string name = "TestPlayer", ulong userId = 12345)
    {
        return new Character
        {
            Id = 1,
            Name = name,
            UserId = userId,
            CharacterType = CharacterType.PlayerCharacter,
            GameSystem = SwnSystem.SystemName,
            Sheet = new CharacterSheet(),
            LastEdited = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a basic monster character with the SWN system.
    /// </summary>
    public static Character CreateMonster(string name = "TestMonster", ulong userId = 12345)
    {
        return new Character
        {
            Id = 2,
            Name = name,
            UserId = userId,
            CharacterType = CharacterType.Monster,
            GameSystem = SwnSystem.SystemName,
            Sheet = new CharacterSheet(),
            LastEdited = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a test adventurer for encounter testing.
    /// </summary>
    public static Adventurer CreateAdventurer(string name = "TestAdventurer", ulong userId = 12345)
    {
        return new Adventurer
        {
            Id = 1,
            Name = name,
            UserId = userId,
            Sheet = new CharacterSheet(),
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a test monster combatant for encounter testing.
    /// </summary>
    public static MonsterCombatant CreateMonsterCombatant(string name = "TestMonster", CharacterSheet? sheet = null, ulong userId = 12345)
    {
        return new MonsterCombatant
        {
            Name = name,
            Alias = name,
            UserId = userId,
            Sheet = sheet ?? new CharacterSheet(),
            LastUpdated = DateTimeOffset.UtcNow,
            SwarmCount = 1
        };
    }

    /// <summary>
    /// Creates a test encounter.
    /// </summary>
    public static Encounter CreateEncounter(string adventureName = "TestAdventure")
    {
        return new Encounter
        {
            Id = 1,
            AdventureName = adventureName,
            ChannelId = 123456789,
            Round = 1
        };
    }

    /// <summary>
    /// Creates a mock character data service that returns the specified characters.
    /// </summary>
    public static ICharacterDataService CreateMockCharacterDataService(params ITrackedCharacter[] characters)
    {
        var service = Substitute.For<ICharacterDataService>();

        // Set up default to return null for unknown characters
        service.GetCharacterAsync(
            Arg.Any<CombatantBase>(),
            Arg.Any<Encounter>(),
            Arg.Any<CancellationToken>())
            .Returns((ITrackedCharacter?)null);

        // Set up specific characters to return themselves
        foreach (var character in characters)
        {
            service.GetCharacterAsync(
                Arg.Is<CombatantBase>(c => c.Name == character.Name),
                Arg.Any<Encounter>(),
                Arg.Any<CancellationToken>())
                .Returns(character);
        }

        return service;
    }

    /// <summary>
    /// Sets up a player character with all basic values for comprehensive testing.
    /// </summary>
    public static void SetupFullPlayerCharacter(Character character, SwnCharacterSystem characterSystem)
    {
        var sheet = character.Sheet;

        // Set attributes
        characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Strength).EditCharacterProperty("14", character);
        characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Dexterity).EditCharacterProperty("16", character);
        characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Constitution).EditCharacterProperty("13", character);
        characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Intelligence).EditCharacterProperty("12", character);
        characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Wisdom).EditCharacterProperty("15", character);
        characterSystem.GetProperty<GameAbilityCounter>(character, SwnSystem.Charisma).EditCharacterProperty("10", character);

        // Set level and classes
        characterSystem.GetProperty<GameCounter>(character, SwnSystem.Level).EditCharacterProperty("3", character);
        characterSystem.GetProperty<GameProperty>(character, "Class 1").EditCharacterProperty(SwnSystem.Expert, character);
        characterSystem.GetProperty<GameProperty>(character, "Class 2").EditCharacterProperty(SwnSystem.Warrior, character);

        // Set some skills
        characterSystem.GetProperty<GameCounter>(character, SwnSystem.Shoot).EditCharacterProperty("2", character);
        characterSystem.GetProperty<GameCounter>(character, SwnSystem.Fix).EditCharacterProperty("1", character);
        characterSystem.GetProperty<GameCounter>(character, SwnSystem.Notice).EditCharacterProperty("0", character);

        // Set armor value
        characterSystem.GetProperty<GameCounter>(character, SwnSystem.ArmorValue).EditCharacterProperty("13", character);
    }

    /// <summary>
    /// Sets up a monster with all basic values for comprehensive testing.
    /// </summary>
    public static void SetupFullMonster(Character monster, SwnCharacterSystem characterSystem)
    {
        var sheet = monster.Sheet;

        // Set monster stats
        characterSystem.GetProperty<GameCounter>(monster, SwnSystem.HitDice).EditCharacterProperty("4", monster);
        characterSystem.GetProperty<GameCounter>(monster, SwnSystem.ArmorClass).EditCharacterProperty("15", monster);
        characterSystem.GetProperty<GameCounter>(monster, "Attack").EditCharacterProperty("+6", monster);
        characterSystem.GetProperty<GameCounter>(monster, "Morale").EditCharacterProperty("8", monster);
        characterSystem.GetProperty<GameCounter>(monster, "Skill").EditCharacterProperty("+2", monster);
        characterSystem.GetProperty<GameCounter>(monster, "Save").EditCharacterProperty("12", monster);
    }

    /// <summary>
    /// Creates a MockRandomWrapper with predetermined dice results for testing.
    /// Format: [die_size, result, die_size, result, ...]
    /// </summary>
    public static MockRandomWrapper CreatePredictableRandom(params int[] diceResults)
    {
        return new MockRandomWrapper(diceResults);
    }
}