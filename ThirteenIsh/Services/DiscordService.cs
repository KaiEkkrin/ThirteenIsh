using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Reflection;
using ThirteenIsh.Commands;
using ThirteenIsh.Messages;

namespace ThirteenIsh.Services;

internal sealed class DiscordService : IAsyncDisposable, IDisposable
{
    private static readonly TimeSpan SlashCommandTimeout = TimeSpan.FromMilliseconds(1500);

    private static readonly Action<ILogger, string, Exception> RegisterCommandsErrorMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(DiscordService)),
            "Error registering commands: {Details}");

    private static readonly Action<ILogger, string, string, Exception> RegisterGuildCommandsErrorMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(DiscordService)),
            "{Guild}: Error registering guild commands: {Details}");

    private static readonly Action<ILogger, string, string, string, Type, Exception?> RegisteredCommandMessage =
        LoggerMessage.Define<string, string, string, Type>(
            LogLevel.Information,
            new EventId(2, nameof(DiscordService)),
            "{Guild}: Registered command {Name} ({Description}) with handler {HandlerType}");

    private static readonly Action<ILogger, string, TimeSpan, string, Exception> SlashCommandTimeoutMessage =
        LoggerMessage.Define<string, TimeSpan, string>(
            LogLevel.Warning,
            new EventId(3, nameof(DiscordService)),
            "Slash command {Name} timed out after {Timeout}: {Details}");

    private readonly DiscordSocketClient _client = new();
    private readonly ConcurrentDictionary<string, CommandBase> _commandsMap = new();
    private readonly ConcurrentDictionary<string, MessageBase> _messagesMap = new();

    private readonly IConfiguration _configuration;
    private readonly DataService _dataService;
    private readonly ILogger<DiscordService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private bool _isDisposed;

    public DiscordService(
        IConfiguration configuration,
        DataService dataService,
        ILogger<DiscordService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _dataService = dataService;
        _logger = logger;
        _serviceProvider = serviceProvider;

        _client.ButtonExecuted += OnButtonExecuted;
        _client.JoinedGuild += OnJoinedGuildAsync;
        _client.Log += OnLogAsync;
        _client.Ready += OnReadyAsync;
        _client.SlashCommandExecuted += OnSlashCommandExecutedAsync;
        _serviceProvider = serviceProvider;

        // Set up command types (these must be ready right away.)
        BuildCommandsMap();
    }

    public void AddMessageInteraction(MessageBase message)
    {
        _messagesMap.AddOrUpdate(
            message.MessageId,
            message,
            (_, _) => throw new InvalidOperationException($"A message with ID {message.MessageId} is already being tracked."));
    }

    public void DeleteExpiredMessages()
    {
        List<string> expiredIds = [];
        foreach (var (id, message) in _messagesMap)
        {
            if (message.IsExpired) expiredIds.Add(id);
        }

        foreach (var id in expiredIds)
        {
            _messagesMap.Remove(id, out _);
        }
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

    private async Task OnButtonExecuted(SocketMessageComponent arg)
    {
        if (!_messagesMap.TryRemove(arg.Data.CustomId, out var message)) return;

        await using var scope = _serviceProvider.CreateAsyncScope();
        using CancellationTokenSource cancellationSource = new(SlashCommandTimeout);
        try
        {
            await message.HandleAsync(arg, scope.ServiceProvider, cancellationSource.Token);
        }
        catch (OperationCanceledException ex)
        {
            SlashCommandTimeoutMessage(_logger, arg.Data.CustomId, SlashCommandTimeout, ex.Message, ex);
            await arg.RespondAsync($"Message timed out after {SlashCommandTimeout}: {arg.Data.CustomId}");
        }
    }

    private Task OnJoinedGuildAsync(SocketGuild arg)
    {
        if (_isDisposed) return Task.CompletedTask;

        // This is slow, and should be done asynchronously.
        Task.Run(() => RegisterGuildCommandsAsync(arg));
        return Task.CompletedTask;
    }

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

    private Task OnReadyAsync()
    {
        if (_isDisposed) return Task.CompletedTask;

        // Global commands would be more convenient but according to the Discord.net documentation they
        // "can take up to an hour to register", so for now I'm going to use guild commands for every
        // guild we're registered in, instead.
        // This is slow, and should be done asynchronously.
        var _ = Task.Run(RegisterCommandsAsync);
        return Task.CompletedTask;
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

    private void BuildCommandsMap()
    {
        _commandsMap.Clear();
        foreach (var ty in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (!ty.IsClass || ty.IsAbstract || !ty.IsAssignableTo(typeof(CommandBase))) continue;

            var command = (CommandBase)_serviceProvider.GetRequiredService(ty);
            _commandsMap[command.Name] = command;
        }
    }

    private async Task RegisterCommandsAsync()
    {
        try
        {
            // Register with all our guilds
            foreach (var guild in _client.Guilds)
            {
                await RegisterGuildCommandsAsync(guild);
            }
        }
        catch (Exception ex)
        {
            RegisterCommandsErrorMessage(_logger, ex.Message, ex);
        }
    }

    private async Task RegisterGuildCommandsAsync(SocketGuild guild)
    {
        try
        {
            var guildEntity = await _dataService.EnsureGuildAsync(guild.Id);
            if (guildEntity.CommandVersion >= CommandBase.Version)
            {
                // Up-to-date commands are already registered
                return;
            }

            // It might be expensive, but the easiest way to ensure all our commands are
            // up-to-date is to delete them all and re-create them
            await guild.DeleteApplicationCommandsAsync();
            foreach (var (name, command) in _commandsMap)
            {
                var build = command.CreateBuilder().Build();
                await guild.CreateApplicationCommandAsync(build);
                RegisteredCommandMessage(_logger, guild.Name, build.Name.Value, build.Description.Value, command.GetType(), null);
            }

            await _dataService.UpdateGuildCommandVersionAsync(guild.Id, CommandBase.Version);
        }
        catch (Exception ex)
        {
            RegisterGuildCommandsErrorMessage(_logger, guild.Name, ex.Message, ex);
        }
    }
}
