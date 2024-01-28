using MongoDB.Driver;
using System.Runtime.CompilerServices;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Services;

/// <summary>
/// Database adapter -- a singleton
/// </summary>
internal sealed class DataService : IDisposable
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
        await collection.InsertOneAsync(character, new InsertOneOptions { }, cancellationToken);
        return character;
    }

    public async Task<Character?> GetCharacterAsync(string name, ulong? userId = null,
        CancellationToken cancellationToken = default)
    {
        return await ListCharactersAsync(name, userId, cancellationToken).FirstOrDefaultAsync(cancellationToken);
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
        var collection = await GetCharactersCollectionAsync(cancellationToken);
        var character = await ListCharactersAsync(name, userId, cancellationToken).FirstOrDefaultAsync(cancellationToken);
        if (character is null) return null;

        updateSheet(character.Sheet);

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
}
