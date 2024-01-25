using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Reflection;
using ThirteenIsh.Commands;

namespace ThirteenIsh.Services;

internal sealed class DiscordService : IAsyncDisposable, IDisposable
{
    private static readonly TimeSpan SlashCommandTimeout = TimeSpan.FromMilliseconds(2500);

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

    private static readonly Action<ILogger, string, TimeSpan, string, Exception> SlashCommandTimeoutMessage =
        LoggerMessage.Define<string, TimeSpan, string>(
            LogLevel.Warning,
            new EventId(3, nameof(DiscordService)),
            "Slash command {Name} timed out after {Timeout}: {Details}");

    private readonly DiscordSocketClient _client = new();
    private readonly ConcurrentDictionary<string, CommandBase> _commandsMap = new();

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
        foreach (var ty in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (!ty.IsClass || ty.IsAbstract || !ty.IsAssignableTo(typeof(CommandBase))) continue;

            var command = (CommandBase)_serviceProvider.GetRequiredService(ty);
            var builder = command.CreateBuilder();
            try
            {
                await _client.CreateGlobalApplicationCommandAsync(builder.Build());
                _commandsMap[builder.Name] = command;
                RegisteredCommandMessage(_logger, builder.Name, builder.Description, ty, null);
            }
            catch (HttpException ex)
            {
                CreateCommandErrorMessage(
                    _logger,
                    builder.Name,
                    JsonConvert.SerializeObject(ex.Errors, Formatting.Indented),
                    ex);
            }
        }
    }

    private async Task OnSlashCommandExecutedAsync(SocketSlashCommand command)
    {
        if (_isDisposed) return;
        if (_commandsMap.TryGetValue(command.Data.Name, out var commandHandler))
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            using CancellationTokenSource cancellationSource = new(SlashCommandTimeout);
            try
            {
                await commandHandler.HandleAsync(command, scope.ServiceProvider, cancellationSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                SlashCommandTimeoutMessage(_logger, command.Data.Name, SlashCommandTimeout, ex.Message, ex);
                await command.RespondAsync($"Command timed out after {SlashCommandTimeout}: {command.Data.Name}");
            }
        }
        else
        {
            await command.RespondAsync($"Unrecognised command: {command.Data.Name}");
        }
    }
}
