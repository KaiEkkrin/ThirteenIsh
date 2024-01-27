using Microsoft.Extensions.Configuration.UserSecrets;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Services;

internal sealed class DataService
{
    private const string DatabaseName = "ThirteenIsh";

    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;

    public DataService(IConfiguration configuration)
    {
        _client = new MongoClient(configuration[ConfigKeys.MongoConnectionString]);
        _database = _client.GetDatabase(DatabaseName);
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
        var filter = (name, userId) switch
        {
            ({ } n, { } uid) => Builders<Character>.Filter.And(
                Builders<Character>.Filter.Eq(o => o.Name, n),
                Builders<Character>.Filter.Eq(o => o.UserId, Character.ToDatabaseUserId(uid))),
            ({ } n, null) => Builders<Character>.Filter.Eq(o => o.Name, n),
            (null, { } uid) => Builders<Character>.Filter.Eq(o => o.UserId, Character.ToDatabaseUserId(uid)),
            (null, null) => throw new NotSupportedException("Cannot list all characters")
        };

        using var cursor = await GetCharacters().FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var character in cursor.Current) yield return character;
        }
    }

    // TODO ensure indexes first, and all that
    private IMongoCollection<Character> GetCharacters() => _database.GetCollection<Character>("characters");
}
