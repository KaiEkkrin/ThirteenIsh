using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace ThirteenIsh.Services;

internal sealed class DiscordService : IAsyncDisposable, IDisposable
{
    private static readonly Action<ILogger, string, string, Exception> CreateCommandErrorMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(DiscordService)),
            "Error creating command {Name}: {Details}");

    private readonly DiscordSocketClient _client = new();

    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordService> _logger;

    private bool _isDisposed;

    public DiscordService(
        IConfiguration configuration,
        ILogger<DiscordService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _client.Log += OnLogAsync;
        _client.Ready += OnReadyAsync;
        _client.SlashCommandExecuted += OnSlashCommandExecutedAsync;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _client.Dispose();
        _isDisposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        await _client.DisposeAsync();
        _isDisposed = true;
    }

    public async Task StartAsync()
    {
        if (_configuration[ConfigKeys.BotToken] is not { } botToken)
            throw new InvalidOperationException($"No {ConfigKeys.BotToken} setting found");

        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();
    }

    public Task StopAsync() => _client.StopAsync();

    private Task OnLogAsync(LogMessage message)
    {
        var logLevel = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            _ => LogLevel.Debug
        };

        _logger.Log(logLevel, message.Exception, "{Message}", message.Message);
        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        if (_isDisposed) return;

        // TODO set up commands here :)
        // I'm adding a test command for now to check I can make this stuff work.
        SlashCommandBuilder testCommand = new();
        testCommand.WithName("13-test");
        testCommand.WithDescription("A test command for ThirteenIsh");

        try
        {
            await _client.CreateGlobalApplicationCommandAsync(testCommand.Build());
        }
        catch (HttpException ex)
        {
            CreateCommandErrorMessage(
                _logger,
                testCommand.Name,
                JsonConvert.SerializeObject(ex.Errors, Formatting.Indented),
                ex);
        }
    }

    private async Task OnSlashCommandExecutedAsync(SocketSlashCommand command)
    {
        if (_isDisposed) return;
        await command.RespondAsync($"You executed {command.Data.Name}");
    }
}
