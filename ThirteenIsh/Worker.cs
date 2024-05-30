using Microsoft.EntityFrameworkCore;
using ThirteenIsh.Database;
using ThirteenIsh.Services;

namespace ThirteenIsh;

internal sealed partial class Worker(
    DiscordService discordService,
    ILogger<Worker> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private static readonly TimeSpan TimerInterval = TimeSpan.FromMinutes(5);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1, Message = "Worker running at: {Time}")]
    private partial void WorkerRunningMessage(DateTimeOffset time);

    [LoggerMessage(Level = LogLevel.Error, EventId = 2, Message = "Error running worker: {Message}")]
    private partial void ErrorRunningWorkerMessage(string message, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, EventId = 3, Message = "Error stopping worker: {Message}")]
    private partial void ErrorStoppingWorkerMessage(string message, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, EventId = 4, Message = "Stopping worker")]
    private partial void StoppingWorkerMessage();

    [LoggerMessage(Level = LogLevel.Information, EventId = 5, Message = "Migrating database...")]
    private partial void MigratingDatabaseMessage();

    [LoggerMessage(Level = LogLevel.Information, EventId = 6, Message = "Migrate database succeeded")]
    private partial void MigrateDatabaseSucceededMessage();

    private readonly ILogger<Worker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await MigrateDatabaseAsync(stoppingToken);
            await discordService.StartAsync();
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wake up every now and again to run timer tasks
                WorkerRunningMessage(DateTimeOffset.UtcNow);
                await Task.Delay(TimerInterval, stoppingToken);

                await using var scope = serviceProvider.CreateAsyncScope();
                var dataService = scope.ServiceProvider.GetRequiredService<SqlDataService>();
                await dataService.DeleteOldMessagesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            StoppingWorkerMessage();
        }
        catch (Exception ex)
        {
            ErrorRunningWorkerMessage(ex.Message, ex);
        }
        finally
        {
            try
            {
                await discordService.StopAsync();
            }
            catch (Exception ex2)
            {
                ErrorStoppingWorkerMessage(ex2.Message, ex2);
            }
        }
    }

    private async Task MigrateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        MigratingDatabaseMessage();

        await using var scope = serviceProvider.CreateAsyncScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        await dataContext.Database.MigrateAsync(cancellationToken);
        MigrateDatabaseSucceededMessage();
    }
}
