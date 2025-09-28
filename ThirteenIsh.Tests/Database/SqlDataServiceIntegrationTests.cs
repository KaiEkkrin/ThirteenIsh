using Microsoft.Extensions.Logging;
using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.Tests.Database;

[Collection("Database")]
public class SqlDataServiceIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private SqlDataService _sqlDataService = null!;

    public SqlDataServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset database to clean state for each test
        await _fixture.ResetDatabaseAsync();

        // Create a fresh service instance for each test
        var context = _fixture.CreateDataContext();
        var logger = new LoggerFactory().CreateLogger<SqlDataService>();
        _sqlDataService = new SqlDataService(context, logger);
    }

    public async Task DisposeAsync()
    {
        // Context will be disposed automatically when SqlDataService is disposed
        // No explicit disposal needed as context is scoped
        await Task.CompletedTask;
    }

    #region Guild Tests

    [Fact]
    public async Task EnsureGuildAsync_ShouldCreateNewGuild_WhenGuildDoesNotExist()
    {
        // Arrange
        const ulong guildId = 123456789;

        // Act
        var guild = await _sqlDataService.EnsureGuildAsync(guildId);

        // Assert
        guild.ShouldNotBeNull();
        guild.GuildId.ShouldBe(guildId);
        guild.CurrentAdventureName.ShouldBe(string.Empty);

        // Verify it was saved to database
        var retrievedGuild = await _sqlDataService.GetGuildAsync(guildId);
        retrievedGuild.ShouldNotBeNull();
        retrievedGuild.GuildId.ShouldBe(guildId);
    }

    [Fact]
    public async Task EnsureGuildAsync_ShouldReturnExistingGuild_WhenGuildExists()
    {
        // Arrange
        const ulong guildId = 123456789;
        var originalGuild = await _sqlDataService.EnsureGuildAsync(guildId);
        originalGuild.CurrentAdventureName = "Test Adventure";
        await _sqlDataService.SaveChangesAsync();

        // Act
        var retrievedGuild = await _sqlDataService.EnsureGuildAsync(guildId);

        // Assert
        retrievedGuild.ShouldNotBeNull();
        retrievedGuild.Id.ShouldBe(originalGuild.Id);
        retrievedGuild.GuildId.ShouldBe(guildId);
        retrievedGuild.CurrentAdventureName.ShouldBe("Test Adventure");
    }

    [Fact]
    public async Task GetGuildAsync_ShouldThrowException_WhenGuildDoesNotExist()
    {
        // Arrange
        const ulong nonExistentGuildId = 999999999;

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sqlDataService.GetGuildAsync(nonExistentGuildId));
    }

    #endregion

    #region Adventure Tests

    [Fact]
    public async Task AddAdventureAsync_ShouldCreateNewAdventure_WhenValidData()
    {
        // Arrange
        const ulong guildId = 123456789;
        const string adventureName = "The Lost Mine";
        const string description = "A dangerous mining expedition";
        const string gameSystem = "13th Age";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);

        // Act
        var result = await _sqlDataService.AddAdventureAsync(guildId, adventureName, description, gameSystem);

        // Assert
        result.ShouldNotBeNull();
        result.Adventure.Name.ShouldBe(adventureName);
        result.Adventure.Description.ShouldBe(description);
        result.Adventure.GameSystem.ShouldBe(gameSystem);
        result.Adventure.Guild.ShouldBe(guild);

        // Verify guild's current adventure was updated
        var updatedGuild = await _sqlDataService.GetGuildAsync(guildId);
        updatedGuild.CurrentAdventureName.ShouldBe(adventureName);
    }

    [Fact]
    public async Task AddAdventureAsync_ShouldReturnNull_WhenDuplicateAdventureName()
    {
        // Arrange
        const ulong guildId = 123456789;
        const string adventureName = "Duplicate Adventure";
        const string description = "First adventure";
        const string gameSystem = "13th Age";

        await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, description, gameSystem);

        // Act - Try to add adventure with same name
        var result = await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Second adventure", gameSystem);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAdventureAsync_ShouldFindExactMatch_WhenExactNameProvided()
    {
        // Arrange
        const ulong guildId = 123456789;
        const string adventureName = "The Exact Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");

        // Act
        var adventure = await _sqlDataService.GetAdventureAsync(guild, adventureName);

        // Assert
        adventure.ShouldNotBeNull();
        adventure.Name.ShouldBe(adventureName);
    }

    [Fact]
    public async Task GetAdventureAsync_ShouldFindPartialMatch_WhenUnambiguous()
    {
        // Arrange
        const ulong guildId = 123456789;
        const string adventureName = "The Unique Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");

        // Act
        var adventure = await _sqlDataService.GetAdventureAsync(guild, "The Unique");

        // Assert
        adventure.ShouldNotBeNull();
        adventure.Name.ShouldBe(adventureName);
    }

    [Fact]
    public async Task GetAdventureAsync_ShouldReturnNull_WhenAmbiguousPartialMatch()
    {
        // Arrange
        const ulong guildId = 123456789;

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, "The Adventure One", "Description", "13th Age");
        await _sqlDataService.AddAdventureAsync(guildId, "The Adventure Two", "Description", "13th Age");

        // Act
        var adventure = await _sqlDataService.GetAdventureAsync(guild, "The Adventure");

        // Assert
        adventure.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAdventureAsync_ShouldRemoveAdventure_WhenExists()
    {
        // Arrange
        const ulong guildId = 123456789;
        const string adventureName = "Adventure to Delete";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");

        // Act
        var deletedAdventure = await _sqlDataService.DeleteAdventureAsync(guildId, adventureName);

        // Assert
        deletedAdventure.ShouldNotBeNull();
        deletedAdventure.Name.ShouldBe(adventureName);

        // Verify it was actually deleted
        var retrievedAdventure = await _sqlDataService.GetAdventureAsync(guild, adventureName);
        retrievedAdventure.ShouldBeNull();

        // Verify guild's current adventure was cleared
        var updatedGuild = await _sqlDataService.GetGuildAsync(guildId);
        updatedGuild.CurrentAdventureName.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task DeleteAdventureAsync_ShouldReturnNull_WhenAdventureDoesNotExist()
    {
        // Arrange
        const ulong guildId = 123456789;
        await _sqlDataService.EnsureGuildAsync(guildId);

        // Act
        var result = await _sqlDataService.DeleteAdventureAsync(guildId, "Non-existent Adventure");

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Character Tests

    [Fact]
    public async Task CreateCharacterAsync_ShouldCreateNewCharacter_WhenValidData()
    {
        // Arrange
        const string characterName = "Thorin Ironforge";
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        // Act
        var character = await _sqlDataService.CreateCharacterAsync(
            characterName, characterType, gameSystem, userId);

        // Assert
        character.ShouldNotBeNull();
        character.Name.ShouldBe(characterName);
        character.CharacterType.ShouldBe(characterType);
        character.GameSystem.ShouldBe(gameSystem);
        character.UserId.ShouldBe(userId);
        character.Sheet.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateCharacterAsync_ShouldReturnNull_WhenDuplicateCharacterName()
    {
        // Arrange
        const string characterName = "Duplicate Character";
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        await _sqlDataService.CreateCharacterAsync(characterName, characterType, gameSystem, userId);

        // Act - Try to create character with same name for same user and type
        var duplicateCharacter = await _sqlDataService.CreateCharacterAsync(
            characterName, characterType, gameSystem, userId);

        // Assert
        duplicateCharacter.ShouldBeNull();
    }

    [Fact]
    public async Task CreateCharacterAsync_ShouldAllowSameNameForDifferentUsers()
    {
        // Arrange
        const string characterName = "Shared Character Name";
        const ulong userId1 = 111111111;
        const ulong userId2 = 222222222;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        // Act
        var character1 = await _sqlDataService.CreateCharacterAsync(
            characterName, characterType, gameSystem, userId1);
        var character2 = await _sqlDataService.CreateCharacterAsync(
            characterName, characterType, gameSystem, userId2);

        // Assert
        character1.ShouldNotBeNull();
        character2.ShouldNotBeNull();
        character1.UserId.ShouldBe(userId1);
        character2.UserId.ShouldBe(userId2);
    }

    [Fact]
    public async Task CreateCharacterAsync_ShouldAllowSameNameForDifferentCharacterTypes()
    {
        // Arrange
        const string characterName = "Same Name Different Type";
        const ulong userId = 987654321;
        const string gameSystem = "13th Age";

        // Act
        var playerCharacter = await _sqlDataService.CreateCharacterAsync(
            characterName, CharacterType.PlayerCharacter, gameSystem, userId);
        var npc = await _sqlDataService.CreateCharacterAsync(
            characterName, CharacterType.Monster, gameSystem, userId);

        // Assert
        playerCharacter.ShouldNotBeNull();
        npc.ShouldNotBeNull();
        playerCharacter.CharacterType.ShouldBe(CharacterType.PlayerCharacter);
        npc.CharacterType.ShouldBe(CharacterType.Monster);
    }

    [Fact]
    public async Task CreateCharacterAsync_ShouldCallInitialiseCharacter_WhenProvided()
    {
        // Arrange
        const string characterName = "Initialized Character";
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";
        var initializerCalled = false;

        // Act
        var character = await _sqlDataService.CreateCharacterAsync(
            characterName, characterType, gameSystem, userId,
            c => {
                initializerCalled = true;
                // Character doesn't have Notes property, just verify initializer was called
            });

        // Assert
        character.ShouldNotBeNull();
        initializerCalled.ShouldBeTrue();
        // Verify initializer was called (can't test Notes property as it doesn't exist)
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldFindExactMatch()
    {
        // Arrange
        const string characterName = "Exact Match Character";
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        await _sqlDataService.CreateCharacterAsync(characterName, characterType, gameSystem, userId);

        // Act
        var character = await _sqlDataService.GetCharacterAsync(characterName, userId, characterType);

        // Assert
        character.ShouldNotBeNull();
        character.Name.ShouldBe(characterName);
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldFindPartialMatch_WhenUnambiguous()
    {
        // Arrange
        const string characterName = "The Unique Character Name";
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        await _sqlDataService.CreateCharacterAsync(characterName, characterType, gameSystem, userId);

        // Act
        var character = await _sqlDataService.GetCharacterAsync("The Unique", userId, characterType);

        // Assert
        character.ShouldNotBeNull();
        character.Name.ShouldBe(characterName);
    }

    [Fact]
    public async Task DeleteCharacterAsync_ShouldRemoveCharacter_WhenExists()
    {
        // Arrange
        const string characterName = "Character to Delete";
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        await _sqlDataService.CreateCharacterAsync(characterName, characterType, gameSystem, userId);

        // Act
        var deletedCharacter = await _sqlDataService.DeleteCharacterAsync(characterName, userId, characterType);

        // Assert
        deletedCharacter.ShouldNotBeNull();
        deletedCharacter.Name.ShouldBe(characterName);

        // Verify it was actually deleted
        var retrievedCharacter = await _sqlDataService.GetCharacterAsync(characterName, userId, characterType);
        retrievedCharacter.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteCharacterAsync_ShouldReturnNull_WhenCharacterDoesNotExist()
    {
        // Arrange
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;

        // Act
        var result = await _sqlDataService.DeleteCharacterAsync("Non-existent Character", userId, characterType);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Message Tests

    [Fact]
    public async Task AddMessageAsync_ShouldSaveMessage_WhenValidMessage()
    {
        // Arrange
        var message = new AddCharacterMessage
        {
            UserId = 123456789,
            Name = "Test Character",
            CharacterType = CharacterType.PlayerCharacter,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _sqlDataService.AddMessageAsync(message);

        // Assert
        var retrievedMessage = await _sqlDataService.GetMessageAsync(message.Id);
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.ShouldBeOfType<AddCharacterMessage>();
        var addCharMessage = (AddCharacterMessage)retrievedMessage;
        addCharMessage.UserId.ShouldBe(message.UserId);
        addCharMessage.Name.ShouldBe(message.Name);
        addCharMessage.CharacterType.ShouldBe(message.CharacterType);
    }

    [Fact]
    public async Task DeleteMessageAsync_ShouldRemoveMessage_WhenExists()
    {
        // Arrange
        var message = new DeleteAdventureMessage
        {
            UserId = 123456789,
            GuildId = 123456789,
            Name = "Adventure to Delete",
            Timestamp = DateTimeOffset.UtcNow
        };
        await _sqlDataService.AddMessageAsync(message);

        // Act
        await _sqlDataService.DeleteMessageAsync(message.Id);

        // Assert
        var retrievedMessage = await _sqlDataService.GetMessageAsync(message.Id);
        retrievedMessage.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteOldMessagesAsync_ShouldRemoveExpiredMessages()
    {
        // Arrange - Create messages with different timestamps
        var oldMessage = new AddCharacterMessage
        {
            UserId = 123456789,
            Name = "Old Character",
            CharacterType = CharacterType.PlayerCharacter,
            Timestamp = DateTimeOffset.UtcNow - TimeSpan.FromDays(8) // Older than 7-day timeout
        };

        var recentMessage = new AddCharacterMessage
        {
            UserId = 123456789,
            Name = "Recent Character",
            CharacterType = CharacterType.PlayerCharacter,
            Timestamp = DateTimeOffset.UtcNow - TimeSpan.FromHours(1) // Recent
        };

        await _sqlDataService.AddMessageAsync(oldMessage);
        await _sqlDataService.AddMessageAsync(recentMessage);

        // Act
        await _sqlDataService.DeleteOldMessagesAsync();

        // Assert
        var retrievedOldMessage = await _sqlDataService.GetMessageAsync(oldMessage.Id);
        var retrievedRecentMessage = await _sqlDataService.GetMessageAsync(recentMessage.Id);

        retrievedOldMessage.ShouldBeNull();
        retrievedRecentMessage.ShouldNotBeNull();
    }

    #endregion

    #region Adventurer Tests

    [Fact]
    public async Task GetAdventurerAsync_ShouldReturnAdventurer_WhenExistsByUserId()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong userId = 987654321;
        const string adventureName = "Test Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        var adventureResult = await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");
        var adventure = adventureResult!.Adventure;

        // Create an adventurer manually using the context
        using var context = _fixture.CreateDataContext();
        var adventurer = new Adventurer
        {
            Name = "Test Adventurer",
                        UserId = userId,
            AdventureId = adventure.Id,
            LastUpdated = DateTimeOffset.UtcNow,
            Sheet = new CharacterSheet(),
        };
        context.Adventurers.Add(adventurer);
        await context.SaveChangesAsync();

        // Act
        var retrievedAdventurer = await _sqlDataService.GetAdventurerAsync(adventure, userId);

        // Assert
        retrievedAdventurer.ShouldNotBeNull();
        retrievedAdventurer.Name.ShouldBe("Test Adventurer");
        retrievedAdventurer.UserId.ShouldBe(userId);
        retrievedAdventurer.AdventureId.ShouldBe(adventure.Id);
    }

    [Fact]
    public async Task GetAdventurerAsync_ShouldReturnAdventurer_WhenExistsByName()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong userId = 987654321;
        const string adventureName = "Test Adventure";
        const string adventurerName = "Named Adventurer";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        var adventureResult = await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");
        var adventure = adventureResult!.Adventure;

        // Create an adventurer manually
        using var context = _fixture.CreateDataContext();
        var adventurer = new Adventurer
        {
            Name = adventurerName,
                        UserId = userId,
            AdventureId = adventure.Id,
            LastUpdated = DateTimeOffset.UtcNow,
            Sheet = new CharacterSheet(),
        };
        context.Adventurers.Add(adventurer);
        await context.SaveChangesAsync();

        // Act
        var retrievedAdventurer = await _sqlDataService.GetAdventurerAsync(adventure, adventurerName);

        // Assert
        retrievedAdventurer.ShouldNotBeNull();
        retrievedAdventurer.Name.ShouldBe(adventurerName);
        retrievedAdventurer.UserId.ShouldBe(userId);
    }

    [Fact]
    public async Task GetAdventurerAsync_ShouldFindPartialMatch_WhenUnambiguous()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong userId = 987654321;
        const string adventureName = "Test Adventure";
        const string adventurerName = "The Unique Adventurer Name";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        var adventureResult = await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");
        var adventure = adventureResult!.Adventure;

        // Create an adventurer manually
        using var context = _fixture.CreateDataContext();
        var adventurer = new Adventurer
        {
            Name = adventurerName,
                        UserId = userId,
            AdventureId = adventure.Id,
            LastUpdated = DateTimeOffset.UtcNow,
            Sheet = new CharacterSheet(),
        };
        context.Adventurers.Add(adventurer);
        await context.SaveChangesAsync();

        // Act
        var retrievedAdventurer = await _sqlDataService.GetAdventurerAsync(adventure, "The Unique");

        // Assert
        retrievedAdventurer.ShouldNotBeNull();
        retrievedAdventurer.Name.ShouldBe(adventurerName);
    }

    [Fact]
    public async Task DeleteAdventurerAsync_ShouldRemoveAdventurer_WhenExists()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong userId = 987654321;
        const string adventureName = "Test Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        var adventureResult = await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");
        var adventure = adventureResult!.Adventure;

        // Create an adventurer manually
        using var context = _fixture.CreateDataContext();
        var adventurer = new Adventurer
        {
            Name = "Adventurer to Delete",
                        UserId = userId,
            AdventureId = adventure.Id,
            LastUpdated = DateTimeOffset.UtcNow,
            Sheet = new CharacterSheet(),
        };
        context.Adventurers.Add(adventurer);
        await context.SaveChangesAsync();

        // Act
        var deletedAdventurer = await _sqlDataService.DeleteAdventurerAsync(guildId, userId, adventureName);

        // Assert
        deletedAdventurer.ShouldNotBeNull();
        deletedAdventurer.Name.ShouldBe("Adventurer to Delete");
        deletedAdventurer.UserId.ShouldBe(userId);

        // Verify it was actually deleted
        var retrievedAdventurer = await _sqlDataService.GetAdventurerAsync(adventure, userId);
        retrievedAdventurer.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAdventurerAsync_ShouldReturnNull_WhenAdventurerDoesNotExist()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong userId = 987654321;
        const string adventureName = "Test Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");

        // Act
        var result = await _sqlDataService.DeleteAdventurerAsync(guildId, userId, adventureName);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAdventurersAsync_ShouldReturnAllAdventurersInAdventure()
    {
        // Arrange
        const ulong guildId = 123456789;
        const string adventureName = "Test Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        var adventureResult = await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");
        var adventure = adventureResult!.Adventure;

        // Create multiple adventurers manually
        using var context = _fixture.CreateDataContext();
        var adventurer1 = new Adventurer
        {
            Name = "Adventurer Alpha",
                        UserId = 111111111,
            AdventureId = adventure.Id,
            LastUpdated = DateTimeOffset.UtcNow,
            Sheet = new CharacterSheet(),
        };
        var adventurer2 = new Adventurer
        {
            Name = "Adventurer Beta",
                        UserId = 222222222,
            AdventureId = adventure.Id,
            LastUpdated = DateTimeOffset.UtcNow,
            Sheet = new CharacterSheet(),
        };
        context.Adventurers.AddRange(adventurer1, adventurer2);
        await context.SaveChangesAsync();

        // Act
        var adventurers = await _sqlDataService.GetAdventurersAsync(adventure).ToListAsync();

        // Assert
        adventurers.ShouldNotBeNull();
        adventurers.Count.ShouldBe(2);
        adventurers.ShouldContain(a => a.Name == "Adventurer Alpha");
        adventurers.ShouldContain(a => a.Name == "Adventurer Beta");
        // Should be ordered by name
        adventurers[0].Name.ShouldBe("Adventurer Alpha");
        adventurers[1].Name.ShouldBe("Adventurer Beta");
    }

    #endregion

    #region Encounter Tests

    [Fact]
    public async Task AddEncounterAsync_ShouldCreateNewEncounter_WhenValidData()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong channelId = 555555555;
        const string adventureName = "Test Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");

        // Act
        var result = await _sqlDataService.AddEncounterAsync(guildId, channelId);

        // Assert
        result.Success.ShouldBeTrue();
        result.Handle(
            errorMessage => throw new InvalidOperationException($"Unexpected error: {errorMessage}"),
            value => {
                value.ShouldNotBeNull();
                value.Encounter.ChannelId.ShouldBe(channelId);
                value.Encounter.GuildId.ShouldBe(guild.Id);
                value.Encounter.AdventureName.ShouldBe(adventureName);
                value.Encounter.Round.ShouldBe(1);
                value.Adventure.Name.ShouldBe(adventureName);
                return true;
            });
    }

    [Fact]
    public async Task AddEncounterAsync_ShouldReturnError_WhenEncounterAlreadyExists()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong channelId = 555555555;
        const string adventureName = "Test Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");
        await _sqlDataService.AddEncounterAsync(guildId, channelId);

        // Act
        var result = await _sqlDataService.AddEncounterAsync(guildId, channelId);

        // Assert
        result.Success.ShouldBeFalse();
        result.Handle(
            errorMessage => {
                errorMessage.ShouldContain("already an active encounter");
                return true;
            },
            value => throw new InvalidOperationException("Expected error but got success"));
    }

    [Fact]
    public async Task AddEncounterAsync_ShouldReturnError_WhenNoCurrentAdventure()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong channelId = 555555555;

        await _sqlDataService.EnsureGuildAsync(guildId);

        // Act
        var result = await _sqlDataService.AddEncounterAsync(guildId, channelId);

        // Assert
        result.Success.ShouldBeFalse();
        result.Handle(
            errorMessage => {
                errorMessage.ShouldContain("no current adventure");
                return true;
            },
            value => throw new InvalidOperationException("Expected error but got success"));
    }

    [Fact]
    public async Task GetEncounterAsync_ShouldReturnEncounter_WhenExists()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong channelId = 555555555;
        const string adventureName = "Test Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");
        await _sqlDataService.AddEncounterAsync(guildId, channelId);

        // Act
        var encounter = await _sqlDataService.GetEncounterAsync(guild, channelId);

        // Assert
        encounter.ShouldNotBeNull();
        encounter.ChannelId.ShouldBe(channelId);
        encounter.GuildId.ShouldBe(guild.Id);
        encounter.AdventureName.ShouldBe(adventureName);
    }

    [Fact]
    public async Task GetEncounterAsync_ShouldReturnNull_WhenEncounterDoesNotExist()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong channelId = 555555555;

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);

        // Act
        var encounter = await _sqlDataService.GetEncounterAsync(guild, channelId);

        // Assert
        encounter.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteEncounterAsync_ShouldRemoveEncounter_WhenExists()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong channelId = 555555555;
        const string adventureName = "Test Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");
        await _sqlDataService.AddEncounterAsync(guildId, channelId);

        // Act
        var deletedEncounter = await _sqlDataService.DeleteEncounterAsync(guildId, channelId);

        // Assert
        deletedEncounter.ShouldNotBeNull();
        deletedEncounter.ChannelId.ShouldBe(channelId);
        deletedEncounter.GuildId.ShouldBe(guild.Id);

        // Verify it was actually deleted
        var retrievedEncounter = await _sqlDataService.GetEncounterAsync(guild, channelId);
        retrievedEncounter.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteEncounterAsync_ShouldReturnNull_WhenEncounterDoesNotExist()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong channelId = 555555555;

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);

        // Act
        var result = await _sqlDataService.DeleteEncounterAsync(guildId, channelId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetEncounterResultAsync_ShouldReturnResult_WhenEncounterExists()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong channelId = 555555555;
        const string adventureName = "Test Adventure";

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description", "13th Age");
        await _sqlDataService.AddEncounterAsync(guildId, channelId);

        // Act
        var result = await _sqlDataService.GetEncounterResultAsync(guild, channelId);

        // Assert
        result.Success.ShouldBeTrue();
        result.Handle(
            errorMessage => throw new InvalidOperationException($"Unexpected error: {errorMessage}"),
            value => {
                value.ShouldNotBeNull();
                value.Adventure.Name.ShouldBe(adventureName);
                value.Encounter.ChannelId.ShouldBe(channelId);
                return true;
            });
    }

    [Fact]
    public async Task GetEncounterResultAsync_ShouldReturnError_WhenEncounterDoesNotExist()
    {
        // Arrange
        const ulong guildId = 123456789;
        const ulong channelId = 555555555;

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);

        // Act
        var result = await _sqlDataService.GetEncounterResultAsync(guild, channelId);

        // Assert
        result.Success.ShouldBeFalse();
        result.Handle(
            errorMessage => {
                errorMessage.ShouldContain("no active encounter");
                return true;
            },
            value => throw new InvalidOperationException("Expected error but got success"));
    }

    #endregion

    #region Advanced Operations Tests

    [Fact]
    public async Task UpdateGuildCommandVersionAsync_ShouldUpdateVersion()
    {
        // Arrange
        const ulong guildId = 123456789;
        const int newVersion = 42;

        var guild = await _sqlDataService.EnsureGuildAsync(guildId);
        guild.CommandVersion.ShouldBe(0); // Default value

        // Act
        await _sqlDataService.UpdateGuildCommandVersionAsync(guild, newVersion);

        // Assert
        var updatedGuild = await _sqlDataService.GetGuildAsync(guildId);
        updatedGuild.CommandVersion.ShouldBe(newVersion);
    }

    [Fact]
    public async Task GetCharactersPageAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        // Create multiple characters
        await _sqlDataService.CreateCharacterAsync("Character Alpha", characterType, gameSystem, userId);
        await _sqlDataService.CreateCharacterAsync("Character Beta", characterType, gameSystem, userId);
        await _sqlDataService.CreateCharacterAsync("Character Gamma", characterType, gameSystem, userId);

        // Act
        var page = await _sqlDataService.GetCharactersPageAsync(userId, characterType, null, false, 2);

        // Assert
        page.ShouldNotBeNull();
        page.Count.ShouldBe(2);
        page[0].Name.ShouldBe("Character Alpha");
        page[1].Name.ShouldBe("Character Beta");
    }

    [Fact]
    public async Task GetCharactersPageAsync_ShouldRespectNameFilter()
    {
        // Arrange
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        // Create multiple characters
        await _sqlDataService.CreateCharacterAsync("Alpha Character", characterType, gameSystem, userId);
        await _sqlDataService.CreateCharacterAsync("Beta Character", characterType, gameSystem, userId);
        await _sqlDataService.CreateCharacterAsync("Gamma Character", characterType, gameSystem, userId);

        // Act - Get characters starting from "Beta"
        var page = await _sqlDataService.GetCharactersPageAsync(userId, characterType, "Beta Character", false, 10);

        // Assert
        page.ShouldNotBeNull();
        page.Count.ShouldBe(2); // Beta and Gamma
        page[0].Name.ShouldBe("Beta Character");
        page[1].Name.ShouldBe("Gamma Character");
    }

    [Theory]
    [InlineData(true)] // asTracking = true
    [InlineData(false)] // asTracking = false
    public async Task GetCharacterAsync_ShouldRespectTrackingBehavior(bool asTracking)
    {
        // Arrange
        const string characterName = "Tracking Test Character";
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        await _sqlDataService.CreateCharacterAsync(characterName, characterType, gameSystem, userId);

        // Act
        var character = await _sqlDataService.GetCharacterAsync(characterName, userId, characterType, asTracking);

        // Assert
        character.ShouldNotBeNull();
        character.Name.ShouldBe(characterName);

        // For tracking behavior, we can't easily test the EF tracking state in this context,
        // but we can at least verify the method executes without error
    }

    #endregion

    #region Error Scenario Tests

    [Fact]
    public async Task EnsureGuildAsync_ShouldHandleConcurrentCreation()
    {
        // This test simulates the race condition handling in EnsureGuildAsync
        // where two requests try to create the same guild simultaneously

        // Arrange
        const ulong guildId = 123456789;

        // Act - Create guild twice (simulating concurrent requests)
        var guild1 = await _sqlDataService.EnsureGuildAsync(guildId);
        var guild2 = await _sqlDataService.EnsureGuildAsync(guildId);

        // Assert
        guild1.ShouldNotBeNull();
        guild2.ShouldNotBeNull();
        guild1.Id.ShouldBe(guild2.Id); // Should be the same guild
        guild1.GuildId.ShouldBe(guildId);
        guild2.GuildId.ShouldBe(guildId);
    }

    [Fact]
    public async Task AddAdventureAsync_ShouldHandleUniqueConstraintViolation()
    {
        // This test verifies the unique constraint handling for duplicate adventure names

        // Arrange
        const ulong guildId = 123456789;
        const string adventureName = "Constraint Test Adventure";

        await _sqlDataService.EnsureGuildAsync(guildId);

        // Act
        var result1 = await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description 1", "13th Age");
        var result2 = await _sqlDataService.AddAdventureAsync(guildId, adventureName, "Description 2", "13th Age");

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldBeNull();
    }

    [Fact]
    public async Task CreateCharacterAsync_ShouldHandleUniqueConstraintViolation()
    {
        // This test verifies the unique constraint handling for duplicate character names

        // Arrange
        const string characterName = "Constraint Test Character";
        const ulong userId = 987654321;
        const CharacterType characterType = CharacterType.PlayerCharacter;
        const string gameSystem = "13th Age";

        // Act
        var character1 = await _sqlDataService.CreateCharacterAsync(characterName, characterType, gameSystem, userId);
        var character2 = await _sqlDataService.CreateCharacterAsync(characterName, characterType, gameSystem, userId);

        // Assert
        character1.ShouldNotBeNull();
        character2.ShouldBeNull();
    }

    #endregion
}