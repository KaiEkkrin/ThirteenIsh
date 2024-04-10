using Microsoft.EntityFrameworkCore;
using ThirteenIsh.Database;
using ThirteenIsh.Services;

namespace ThirteenIsh;

internal sealed class Worker(
    DiscordService discordService,
    ILogger<Worker> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private static readonly TimeSpan TimerInterval = TimeSpan.FromMinutes(5);

    private static readonly Action<ILogger, DateTimeOffset, Exception?> WorkerRunningMessage =
        LoggerMessage.Define<DateTimeOffset>(
            LogLevel.Information,
            new EventId(1, nameof(Worker)),
            "Worker running at: {Time}");

    private static readonly Action<ILogger, string, Exception> ErrorRunningWorkerMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, nameof(Worker)),
            "Error running worker: {Message}");

    private static readonly Action<ILogger, string, Exception> ErrorStoppingWorkerMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(Worker)),
            "Error stopping worker: {Message}");

    private static readonly Action<ILogger, Exception?> StoppingWorkerMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(4, nameof(Worker)),
            "Stopping worker");

    private static readonly Action<ILogger, Exception?> MigratingDatabase = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(5, nameof(Worker)),
        "Migrating database...");

    private static readonly Action<ILogger, Exception?> MigrateDatabaseSucceeded = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(6, nameof(Worker)),
        "Migrating database succeeded");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await MigrateDatabaseAsync(stoppingToken);
            await discordService.StartAsync();
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wake up every now and again to run timer tasks
                WorkerRunningMessage(logger, DateTimeOffset.Now, null);
                await Task.Delay(TimerInterval, stoppingToken);

                await using var scope = serviceProvider.CreateAsyncScope();
                var dataService = scope.ServiceProvider.GetRequiredService<SqlDataService>();
                await dataService.DeleteOldMessagesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            StoppingWorkerMessage(logger, null);
        }
        catch (Exception ex)
        {
            ErrorRunningWorkerMessage(logger, ex.Message, ex);
        }
        finally
        {
            try
            {
                await discordService.StopAsync();
            }
            catch (Exception ex2)
            {
                ErrorStoppingWorkerMessage(logger, ex2.Message, ex2);
            }
        }
    }

    private async Task MigrateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        MigratingDatabase(logger, null);

        await using var scope = serviceProvider.CreateAsyncScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        await dataContext.Database.MigrateAsync(cancellationToken);
        MigrateDatabaseSucceeded(logger, null);
    }
}
