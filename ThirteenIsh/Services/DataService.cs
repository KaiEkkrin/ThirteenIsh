using MongoDB.Driver;
using System.Runtime.CompilerServices;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Services;

/// <summary>
/// Database adapter -- a singleton
/// </summary>
public sealed class DataService : IDisposable
{
    private const string DatabaseName = "ThirteenIsh";

    // I suspect these errors aren't fatal, because the index would already exist
    private static readonly Action<ILogger, string, string, Exception> ErrorCreatingIndex =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1, nameof(DataService)),
            "Error creating index on {Keys}: {Message}. Maybe it already exists?");

    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly ILogger<DataService> _logger;

    // These will be set after an index has been added
    private IMongoCollection<Character>? _characters;
    private IMongoCollection<Guild>? _guilds;

    private readonly SemaphoreSlim _indexCreationSemaphore = new(1, 1);

    public DataService(
        IConfiguration configuration,
        ILogger<DataService> logger)
    {
        _client = new MongoClient(configuration[ConfigKeys.MongoConnectionString]);
        _database = _client.GetDatabase(DatabaseName);
        _logger = logger;
    }

    public async Task<Character?> CreateCharacterAsync(string name, CharacterSheet sheet, ulong userId,
        CancellationToken cancellationToken = default)
    {
        Character character = new()
        {
            Name = name,
            Sheet = sheet,
            UserId = Character.ToDatabaseUserId(userId)
        };

        var collection = await GetCharactersCollectionAsync(cancellationToken);
        try
        {
            await collection.InsertOneAsync(character, new InsertOneOptions { }, cancellationToken);
            return character;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
        {
            // This means that character already exists
            return null;
        }
    }

    public async Task<bool> DeleteCharacterAsync(string name, ulong userId, CancellationToken cancellationToken = default)
    {
        var collection = await GetCharactersCollectionAsync(cancellationToken);
        var filter = GetCharacterFilter(name, userId);
        var result = await collection.DeleteOneAsync(filter, cancellationToken);
        return result.DeletedCount > 0;
    }

    public async Task<Guild> EnsureGuildAsync(ulong guildId, CancellationToken cancellationToken = default)
    {
        var guild = await GetGuildAsync(guildId, cancellationToken);
        if (guild is null)
        {
            try
            {
                var collection = await GetGuildsCollectionAsync(cancellationToken);
                await collection.InsertOneAsync(
                    new Guild { GuildId = Guild.ToDatabaseGuildId(guildId) },
                    cancellationToken: cancellationToken);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
            {
                // Something must have created it concurrently with us -- can ignore
            }

            guild = await GetGuildAsync(guildId, cancellationToken);
        }

        return guild ?? throw new InvalidOperationException($"Failed to create guild record for {guildId}");
    }

    public async Task<Character?> GetCharacterAsync(string name, ulong? userId = null,
        CancellationToken cancellationToken = default)
    {
        return await ListCharactersAsync(name, userId, cancellationToken).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guild?> GetGuildAsync(ulong guildId, CancellationToken cancellationToken = default)
    {
        var collection = await GetGuildsCollectionAsync(cancellationToken);
        using var cursor = await collection.FindAsync(
            Builders<Guild>.Filter.Eq(o => o.GuildId, Guild.ToDatabaseGuildId(guildId)),
            cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            if (cursor.Current.FirstOrDefault() is { } guild) return guild;
        }

        return null;
    }

    public async IAsyncEnumerable<Character> ListCharactersAsync(
        string? name = null,
        ulong? userId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var collection = await GetCharactersCollectionAsync(cancellationToken);
        var filter = GetCharacterFilter(name, userId);
        using var cursor = await collection.FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var character in cursor.Current) yield return character;
        }
    }

    public async Task<Character?> UpdateCharacterAsync(
        string name, Action<CharacterSheet> updateSheet, ulong userId,
        CancellationToken cancellationToken = default)
    {
        // TODO do this in a transaction.
        // Requiring a transaction here makes the update much easier and more flexible than
        // having to write code to do the splice, and I don't care that it's less performant
        var character = await ListCharactersAsync(name, userId, cancellationToken).FirstOrDefaultAsync(cancellationToken);
        if (character is null) return null;

        updateSheet(character.Sheet);

        var collection = await GetCharactersCollectionAsync(cancellationToken);
        var filter = GetCharacterFilter(name, userId);
        return await collection.FindOneAndReplaceAsync(
            filter,
            character,
            new FindOneAndReplaceOptions<Character>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
    }

    public async Task UpdateGuildCommandVersionAsync(ulong guildId, int commandVersion,
        CancellationToken cancellationToken = default)
    {
        var collection = await GetGuildsCollectionAsync(cancellationToken);
        await collection.UpdateOneAsync(
            Builders<Guild>.Filter.And(
                Builders<Guild>.Filter.Eq(o => o.GuildId, Guild.ToDatabaseGuildId(guildId)),
                Builders<Guild>.Filter.Lt(o => o.CommandVersion, commandVersion)),
            Builders<Guild>.Update.Set(o => o.CommandVersion, commandVersion),
            cancellationToken: cancellationToken);
    }

    public void Dispose() => _indexCreationSemaphore.Dispose();

    private static FilterDefinition<Character> GetCharacterFilter(string? name, ulong? userId) =>
        (name, userId) switch
        {
            ({ } n, { } uid) => Builders<Character>.Filter.And(
                Builders<Character>.Filter.Eq(o => o.Name, n),
                Builders<Character>.Filter.Eq(o => o.UserId, Character.ToDatabaseUserId(uid))),
            ({ } n, null) => Builders<Character>.Filter.Eq(o => o.Name, n),
            (null, { } uid) => Builders<Character>.Filter.Eq(o => o.UserId, Character.ToDatabaseUserId(uid)),
            (null, null) => throw new NotSupportedException("Cannot filter all characters")
        };

    private async Task<IMongoCollection<Character>> GetCharactersCollectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_characters is not null) return _characters;

        await _indexCreationSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_characters is not null) return _characters;
            _characters = _database.GetCollection<Character>("characters");

            try
            {
                await _characters.Indexes.CreateOneAsync(
                    new CreateIndexModel<Character>(
                        Builders<Character>.IndexKeys.Ascending(o => o.UserId).Ascending(o => o.Name),
                        new CreateIndexOptions { Unique = true }),
                    cancellationToken: cancellationToken);
            }
            catch (MongoException ex)
            {
                ErrorCreatingIndex(_logger, "UserId, Name", ex.Message, ex);
            }

            try
            {
                await _characters.Indexes.CreateOneAsync(
                    new CreateIndexModel<Character>(
                        Builders<Character>.IndexKeys.Ascending(o => o.Name)),
                    cancellationToken: cancellationToken);
            }
            catch (MongoException ex)
            {
                ErrorCreatingIndex(_logger, "Name", ex.Message, ex);
            }

            return _characters;
        }
        finally
        {
            _indexCreationSemaphore.Release();
        }
    }

    private async Task<IMongoCollection<Guild>> GetGuildsCollectionAsync(
        CancellationToken cancellationToken = default)
    {
        await _indexCreationSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_guilds is not null) return _guilds;
            _guilds = _database.GetCollection<Guild>("guilds");

            try
            {
                await _guilds.Indexes.CreateOneAsync(
                    new CreateIndexModel<Guild>(
                        Builders<Guild>.IndexKeys.Ascending(o => o.GuildId),
                        new CreateIndexOptions { Unique = true }),
                    cancellationToken: cancellationToken);
            }
            catch (MongoException ex)
            {
                ErrorCreatingIndex(_logger, "GuildId", ex.Message, ex);
            }

            return _guilds;
        }
        finally
        {
            _indexCreationSemaphore.Release();
        }
    }
}
