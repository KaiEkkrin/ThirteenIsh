using MongoDB.Bson;
using MongoDB.Driver;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System.Diagnostics;
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

    private static readonly Action<ILogger, long, TimeSpan, Exception?> DeletedOldMessages =
        LoggerMessage.Define<long, TimeSpan>(
            LogLevel.Information,
            new EventId(2, nameof(DataService)),
            "Deleted {Count} old messages in {Elapsed}");

    private static readonly Action<ILogger, string, Exception> ErrorDeletingOldMessages =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(3, nameof(DataService)),
            "Error deleting old messages: {Message}");

    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly ILogger<DataService> _logger;

    // These will be set after an index has been added
    private IMongoCollection<Character>? _characters;
    private IMongoCollection<Guild>? _guilds;
    private IMongoCollection<MessageBase>? _messages;

    private readonly SemaphoreSlim _indexCreationSemaphore = new(1, 1);

    // Retry policy
    private static readonly IEnumerable<TimeSpan> Delays = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(5), 6);
    private readonly AsyncRetryPolicy _retryPolicy;

    public DataService(
        IConfiguration configuration,
        ILogger<DataService> logger)
    {
        _client = new MongoClient(configuration[ConfigKeys.MongoConnectionString]);
        _database = _client.GetDatabase(DatabaseName);
        _logger = logger;

        _retryPolicy = Policy.Handle<WriteConflictException>().WaitAndRetryAsync(Delays);
    }

    public async Task AddMessageAsync(MessageBase message, CancellationToken cancellationToken = default)
    {
        message.Id = ObjectId.GenerateNewId();
        var collection = await GetMessagesCollectionAsync(cancellationToken);
        await collection.InsertOneAsync(message, cancellationToken: cancellationToken);
    }

    public async Task<Character?> CreateCharacterAsync(string name, string gameSystem, ulong userId,
        CancellationToken cancellationToken = default)
    {
        Character character = new()
        {
            Name = name,
            GameSystem = gameSystem,
            Sheet = new CharacterSheet(),
            UserId = (long)userId,
            Version = 1
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
        var filter = GetCharacterFilter(name, userId, null);
        var result = await collection.DeleteOneAsync(filter, cancellationToken);
        return result.DeletedCount > 0;
    }

    public async Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(messageId, out var id)) return;

        var collection = await GetMessagesCollectionAsync(cancellationToken);
        await collection.DeleteOneAsync(Builders<MessageBase>.Filter.Eq(o => o.Id, id), cancellationToken);
    }

    public async Task DeleteOldMessagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            var expiredTime = DateTimeOffset.UtcNow - MessageBase.Timeout;
            var collection = await GetMessagesCollectionAsync(cancellationToken);
            var result = await collection.DeleteManyAsync(
                Builders<MessageBase>.Filter.Lt(o => o.Timestamp, expiredTime),
                cancellationToken);

            stopwatch.Stop();
            DeletedOldMessages(_logger, result.DeletedCount, stopwatch.Elapsed, null);
        }
        catch (Exception ex)
        {
            ErrorDeletingOldMessages(_logger, ex.Message, ex);
        }
    }

    public async Task<T?> EditGuildAsync<T, TResult>(EditOperation<T, Guild, TResult> operation, ulong guildId,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        var collection = await GetGuildsCollectionAsync(cancellationToken);
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var guild = await EnsureGuildAsync(guildId, cancellationToken);
            var editResult = await operation.DoEditAsync(guild, cancellationToken);
            if (!editResult.Success) return editResult.Value;

            var beforeVersion = guild.Version++;

            // Only replace if the guild version hasn't changed -- otherwise re-read and try again
            var replaceResult = await collection.ReplaceOneAsync(
                GetGuildFilter(guildId, beforeVersion),
                guild,
                cancellationToken: cancellationToken);

            if (replaceResult.ModifiedCount < 1) throw new WriteConflictException(nameof(guild));
            return editResult.Value;
        });
    }

    public async Task<Guild> EnsureGuildAsync(ulong guildId, CancellationToken cancellationToken = default)
    {
        var collection = await GetGuildsCollectionAsync(cancellationToken);
        var guild = await collection.FindOneAndUpdateAsync(
            GetGuildFilter(guildId, null),
            Builders<Guild>.Update.SetOnInsert(nameof(Guild.GuildId), (long)guildId)
                .SetOnInsert(o => o.Version, 1L),
            new FindOneAndUpdateOptions<Guild> { IsUpsert = true, ReturnDocument = ReturnDocument.After },
            cancellationToken);

        return guild ?? throw new InvalidOperationException($"Failed to create guild record for {guildId}");
    }

    public async Task<Character?> GetCharacterAsync(string name, ulong? userId = null,
        CancellationToken cancellationToken = default)
    {
        return await ListCharactersAsync(name, userId, cancellationToken).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MessageBase?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(messageId, out var id)) return null;

        var collection = await GetMessagesCollectionAsync(cancellationToken);
        using var cursor = await collection.FindAsync(
            Builders<MessageBase>.Filter.Eq(o => o.Id, id),
            cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            if (cursor.Current.FirstOrDefault() is { } message) return message;
        }

        return null;
    }

    public async IAsyncEnumerable<Character> ListCharactersAsync(
        string? name = null,
        ulong? userId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var collection = await GetCharactersCollectionAsync(cancellationToken);
        var filter = GetCharacterFilter(name, userId, null);
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
        var collection = await GetCharactersCollectionAsync(cancellationToken);
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var character = await ListCharactersAsync(name, userId, cancellationToken).FirstOrDefaultAsync(cancellationToken);
            if (character is null) return null;

            updateSheet(character.Sheet);
            var beforeVersion = character.Version++;

            var filter = GetCharacterFilter(name, userId, beforeVersion);
            character = await collection.FindOneAndReplaceAsync(
                filter,
                character,
                new FindOneAndReplaceOptions<Character>
                {
                    ReturnDocument = ReturnDocument.After
                },
                cancellationToken);

            return character ?? throw new WriteConflictException(nameof(character));
        });
    }

    public async Task UpdateGuildCommandVersionAsync(ulong guildId, int commandVersion,
        CancellationToken cancellationToken = default)
    {
        var collection = await GetGuildsCollectionAsync(cancellationToken);
        await collection.UpdateOneAsync(
            Builders<Guild>.Filter.And(
                Builders<Guild>.Filter.Eq(o => o.GuildId, (long)guildId),
                Builders<Guild>.Filter.Lt(o => o.CommandVersion, commandVersion)),
            Builders<Guild>.Update.Set(o => o.CommandVersion, commandVersion)
                .Inc(o => o.Version, 1L),
            cancellationToken: cancellationToken);
    }

    public void Dispose() => _indexCreationSemaphore.Dispose();

    private static FilterDefinition<Character> GetCharacterFilter(string? name, ulong? userId, long? version)
    {
        List<FilterDefinition<Character>> conditions = [];
        if (!string.IsNullOrEmpty(name))
        {
            conditions.Add(Builders<Character>.Filter.Eq(o => o.Name, name));
        }

        if (userId.HasValue)
        {
            conditions.Add(Builders<Character>.Filter.Eq(nameof(UserEntityBase.UserId), userId.Value));
        }

        if (version.HasValue)
        {
            conditions.Add(Builders<Character>.Filter.Eq(o => o.Version, version.Value));
        }

        return conditions.Count switch
        {
            0 => throw new NotSupportedException("Cannot filter all characters"),
            1 => conditions[0],
            _ => Builders<Character>.Filter.And(conditions)
        };
    }

    private static FilterDefinition<Guild> GetGuildFilter(ulong? guildId, long? version)
    {
        List<FilterDefinition<Guild>> conditions = [];
        if (guildId.HasValue)
        {
            conditions.Add(Builders<Guild>.Filter.Eq(nameof(Guild.GuildId), (long)guildId.Value));
        }

        if (version.HasValue)
        {
            conditions.Add(Builders<Guild>.Filter.Eq(o => o.Version, version.Value));
        }

        return conditions.Count switch
        {
            0 => throw new NotSupportedException("Cannot filter all guilds"),
            1 => conditions[0],
            _ => Builders<Guild>.Filter.And(conditions)
        };
    }

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

    private async Task<IMongoCollection<MessageBase>> GetMessagesCollectionAsync(
        CancellationToken cancellationToken = default)
    {
        await _indexCreationSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_messages is not null) return _messages;
            _messages = _database.GetCollection<MessageBase>("messages");

            try
            {
                await _messages.Indexes.CreateOneAsync(
                    new CreateIndexModel<MessageBase>(
                        Builders<MessageBase>.IndexKeys.Ascending(o => o.Timestamp)),
                    cancellationToken: cancellationToken);
            }
            catch (MongoException ex)
            {
                ErrorCreatingIndex(_logger, "Timestamp", ex.Message, ex);
            }

            return _messages;
        }
        finally
        {
            _indexCreationSemaphore.Release();
        }
    }
}
