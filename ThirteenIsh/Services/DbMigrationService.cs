using Microsoft.EntityFrameworkCore;
using ThirteenIsh.Database;

namespace ThirteenIsh.Services;

/// <summary>
/// Ensures the SQL migration is applied on start.
/// </summary>
internal sealed class DbMigrationService(
    ILogger<DbMigrationService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private static readonly Action<ILogger, Exception?> MigratingDatabase = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1, nameof(DbMigrationService)),
        "Migrating database...");

    private static readonly Action<ILogger, Exception?> MigrateDatabaseSucceeded = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(2, nameof(DbMigrationService)),
        "Migrating database succeeded");

    private static readonly Action<ILogger, Exception?> MigrateDatabaseFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(3, nameof(DbMigrationService)),
        "Migrating database failed");

    // This semaphore starts held and stays like that until the migration is done
    private static readonly SemaphoreSlim _semaphore = new(0, 1);

    private static Exception? _exception;

    /// <summary>
    /// Call this to wait until migration is done
    /// </summary>
    public static async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Rethrow any exception that occurred on migration failure
            if (_exception != null) throw new AggregateException("Database migration failed", _exception);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        MigratingDatabase(logger, null);
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            await dataContext.Database.MigrateAsync(stoppingToken);
            MigrateDatabaseSucceeded(logger, null);
        }
        catch (Exception ex)
        {
            MigrateDatabaseFailed(logger, ex);
            _exception = ex;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
