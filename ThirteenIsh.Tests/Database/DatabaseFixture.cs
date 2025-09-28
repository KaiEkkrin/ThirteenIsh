using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Respawn;
using ThirteenIsh.Database;

namespace ThirteenIsh.Tests.Database;

/// <summary>
/// XUnit fixture for managing database lifecycle during integration testing.
/// Creates a test database, applies migrations, and provides database cleanup capabilities.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "thirteenish-test";
    private readonly string _connectionString;
    private readonly string _testConnectionString;
    private Respawner? _respawner;

    public string TestConnectionString => _testConnectionString;

    public DatabaseFixture()
    {
        // Get connection string from environment variable (same as main app)
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        _connectionString = configuration[ConfigKeys.DbConnectionString]
            ?? throw new InvalidOperationException("DbConnectionString environment variable not found");

        // Create test database connection string
        var builder = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Database = TestDatabaseName
        };
        _testConnectionString = builder.ToString();
    }

    /// <summary>
    /// Gets a configured DataContext for testing
    /// </summary>
    public DataContext CreateDataContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseNpgsql(_testConnectionString)
            .Options;

        return new DataContext(options);
    }

    /// <summary>
    /// Resets the database to a clean state using Respawn
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (_respawner == null)
        {
            await using var connection = new NpgsqlConnection(_testConnectionString);
            await connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                SchemasToInclude = new[] { "public" },
                DbAdapter = DbAdapter.Postgres
            });
        }

        await using var resetConnection = new NpgsqlConnection(_testConnectionString);
        await resetConnection.OpenAsync();
        await _respawner.ResetAsync(resetConnection);
    }

    private async Task InitializeDatabaseAsync()
    {
        // Create test database if it doesn't exist
        await CreateTestDatabaseIfNotExistsAsync();

        // Apply migrations to test database
        await ApplyMigrationsAsync();
    }

    private async Task CreateTestDatabaseIfNotExistsAsync()
    {
        // Use the main connection string (without database) to connect to postgres
        var mainBuilder = new NpgsqlConnectionStringBuilder(_connectionString);
        var originalDatabase = mainBuilder.Database;
        mainBuilder.Database = "postgres"; // Connect to default database to create our test database

        await using var connection = new NpgsqlConnection(mainBuilder.ToString());
        await connection.OpenAsync();

        // Check if test database exists
        var checkDbCommand = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @dbName", connection);
        checkDbCommand.Parameters.AddWithValue("@dbName", TestDatabaseName);

        var exists = await checkDbCommand.ExecuteScalarAsync();
        if (exists != null)
        {
            // Drop existing test database to ensure clean state
            var terminateCommand = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @dbName AND pid <> pg_backend_pid()",
                connection);
            terminateCommand.Parameters.AddWithValue("@dbName", TestDatabaseName);
            await terminateCommand.ExecuteNonQueryAsync();

            var dropDbCommand = new NpgsqlCommand(
                $"DROP DATABASE \"{TestDatabaseName}\"", connection);
            await dropDbCommand.ExecuteNonQueryAsync();
        }

        // Create fresh test database
        var createDbCommand = new NpgsqlCommand(
            $"CREATE DATABASE \"{TestDatabaseName}\"", connection);
        await createDbCommand.ExecuteNonQueryAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        await using var context = CreateDataContext();
        await context.Database.MigrateAsync();
    }

    public async Task InitializeAsync()
    {
        // Initialize database asynchronously
        await InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        // Optionally drop the test database on disposal
        // For now, we'll leave it for faster subsequent test runs
        // Uncomment the following lines if you want to clean up completely:

        // await DropTestDatabaseAsync();

        await Task.CompletedTask;
    }

    private async Task DropTestDatabaseAsync()
    {
        var mainBuilder = new NpgsqlConnectionStringBuilder(_connectionString);
        mainBuilder.Database = "postgres";

        await using var connection = new NpgsqlConnection(mainBuilder.ToString());
        await connection.OpenAsync();

        // Terminate connections to the test database
        var terminateCommand = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @dbName AND pid <> pg_backend_pid()",
            connection);
        terminateCommand.Parameters.AddWithValue("@dbName", TestDatabaseName);
        await terminateCommand.ExecuteNonQueryAsync();

        // Drop the test database
        var dropDbCommand = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{TestDatabaseName}\"", connection);
        await dropDbCommand.ExecuteNonQueryAsync();
    }
}