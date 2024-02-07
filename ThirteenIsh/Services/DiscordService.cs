using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Reflection;
using ThirteenIsh.Commands;

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

    private static readonly Action<ILogger, string, Exception?> DiscordErrorMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(4, nameof(DiscordService)),
            "[Discord]: {Message}");

    private static readonly Action<ILogger, string, Exception?> DiscordWarningMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(5, nameof(DiscordService)),
            "[Discord]: {Message}");

    private static readonly Action<ILogger, string, Exception?> DiscordInformationMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(6, nameof(DiscordService)),
            "[Discord]: {Message}");

    private static readonly Action<ILogger, string, Exception?> DiscordDebugMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(7, nameof(DiscordService)),
            "[Discord]: {Message}");

    private readonly DiscordSocketClient _client = new();
    private readonly ConcurrentDictionary<string, CommandBase> _commandsMap = new();

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

        _client.ButtonExecuted += OnMessageAsync;
        _client.JoinedGuild += OnJoinedGuildAsync;
        _client.Log += OnLogAsync;
        _client.Ready += OnReadyAsync;
        _client.SelectMenuExecuted += OnMessageAsync;
        _client.SlashCommandExecuted += OnSlashCommandExecutedAsync;
        _serviceProvider = serviceProvider;

        // Set up command types (these must be ready right away.)
        BuildCommandsMap();
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

    public Task<IGuildUser> GetGuildUserAsync(ulong guildId, ulong userId)
    {
        var guild = _client.GetGuild(guildId);
        return ((IGuild)guild).GetUserAsync(userId);
    }

    public async Task StartAsync()
    {
        if (_configuration[ConfigKeys.BotToken] is not { } botToken)
            throw new InvalidOperationException($"No {ConfigKeys.BotToken} setting found");

        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();
    }

    public Task StopAsync() => _client.StopAsync();

    private Task OnJoinedGuildAsync(SocketGuild arg)
    {
        if (_isDisposed) return Task.CompletedTask;

        // This is slow, and should be done asynchronously.
        Task.Run(() => RegisterGuildCommandsAsync(arg));
        return Task.CompletedTask;
    }

    private Task OnLogAsync(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                DiscordErrorMessage(_logger, message.Message, message.Exception);
                break;

            case LogSeverity.Warning:
                DiscordWarningMessage(_logger, message.Message, message.Exception);
                break;

            case LogSeverity.Info:
                DiscordInformationMessage(_logger, message.Message, message.Exception);
                break;

            default:
                DiscordDebugMessage(_logger, message.Message, message.Exception);
                break;
        }

        return Task.CompletedTask;
    }

    private async Task OnMessageAsync(SocketMessageComponent arg)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        using CancellationTokenSource cancellationSource = new(SlashCommandTimeout);

        try
        {
            var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
            var message = await dataService.GetMessageAsync(arg.Data.CustomId, cancellationSource.Token);
            if (message is null || message.NativeUserId != arg.User.Id) return;

            await message.HandleAsync(arg, scope.ServiceProvider, cancellationSource.Token);
            await dataService.DeleteMessageAsync(arg.Data.CustomId, cancellationSource.Token);
        }
        catch (OperationCanceledException ex)
        {
            SlashCommandTimeoutMessage(_logger, arg.Data.CustomId, SlashCommandTimeout, ex.Message, ex);
            await arg.RespondAsync($"Message timed out after {SlashCommandTimeout}: {arg.Data.CustomId}");
        }
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
