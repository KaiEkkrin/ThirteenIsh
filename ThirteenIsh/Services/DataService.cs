using MongoDB.Driver;
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

    public IMongoCollection<Character> GetCharacters() => _database.GetCollection<Character>("characters");
}
