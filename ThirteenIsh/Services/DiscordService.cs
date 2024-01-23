using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Reflection;

namespace ThirteenIsh.Services;

internal sealed class DiscordService : IAsyncDisposable, IDisposable
{
    private static readonly Action<ILogger, string, string, Exception> CreateCommandErrorMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(DiscordService)),
            "Error creating command {Name}: {Details}");

    private static readonly Action<ILogger, string, string, Type, Exception?> RegisteredCommandMessage =
        LoggerMessage.Define<string, string, Type>(
            LogLevel.Information,
            new EventId(2, nameof(DiscordService)),
            "Registered command {Name} ({Description}) with handler {HandlerType}");

    private readonly DiscordSocketClient _client = new();
    private readonly Dictionary<string, Type> _commandsMap = new();

    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private bool _isDisposed;

    public DiscordService(
        IConfiguration configuration,
        ILogger<DiscordService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;

        _client.Log += OnLogAsync;
        _client.Ready += OnReadyAsync;
        _client.SlashCommandExecuted += OnSlashCommandExecutedAsync;
        _serviceProvider = serviceProvider;
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

        // Set up commands
        _commandsMap.Clear();
        foreach (var (attribute, ty) in CommandRegistration.AllCommands)
        {
            SlashCommandBuilder commandBuilder = new();
            commandBuilder.WithName($"13-{attribute.Name}");
            commandBuilder.WithDescription(attribute.Description);
            try
            {
                await _client.CreateGlobalApplicationCommandAsync(commandBuilder.Build());
                _commandsMap[commandBuilder.Name] = ty;
                RegisteredCommandMessage(_logger, commandBuilder.Name, commandBuilder.Description, ty, null);
            }
            catch (HttpException ex)
            {
                CreateCommandErrorMessage(
                    _logger,
                    commandBuilder.Name,
                    JsonConvert.SerializeObject(ex.Errors, Formatting.Indented),
                    ex);
            }
        }
    }

    private async Task OnSlashCommandExecutedAsync(SocketSlashCommand command)
    {
        if (_isDisposed) return;
        if (_commandsMap.TryGetValue(command.Data.Name, out var commandType))
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var handler = (IThirteenIshCommand)scope.ServiceProvider.GetRequiredService(commandType);
            await handler.HandleAsync(command);
        }
        else
        {
            await command.RespondAsync($"Unrecognised command: {command.Data.Name}");
        }
    }
}
