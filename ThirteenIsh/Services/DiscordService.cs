using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Reflection;
using ThirteenIsh.Commands;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.Services;

internal sealed partial class DiscordService : IAsyncDisposable, IDisposable
{
    private static readonly TimeSpan SlashCommandTimeout = TimeSpan.FromMilliseconds(1500);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1, Message = "Error registering commands: {Details}")]
    private partial void RegisterCommandsErrorMessage(string details, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, EventId = 2, Message = "{Guild}: Error registering guild commands: {Details}")]
    private partial void RegisterGuildCommandsErrorMessage(string guild, string details, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, EventId = 3, Message =
        "{Guild}: Registered command {Name} ({Description}) with handler {HandlerType}")]
    private partial void RegisteredCommandMessage(string guild, string name, string description, Type handlerType);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 4, Message =
        "Slash command {Name} timed out after {Timeout}: {Details}")]
    private partial void SlashCommandTimeoutMessage(string name, TimeSpan timeout, string details, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, EventId = 5, Message = "[Discord]: {Message}")]
    private partial void DiscordErrorMessage(string message, Exception? exception);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 6, Message = "[Discord]: {Message}")]
    private partial void DiscordWarningMessage(string message, Exception? exception);

    [LoggerMessage(Level = LogLevel.Information, EventId = 7, Message = "[Discord]: {Message}")]
    private partial void DiscordInformationMessage(string message, Exception? exception);

    [LoggerMessage(Level = LogLevel.Debug, EventId = 8, Message = "[Discord]: {Message}")]
    private partial void DiscordDebugMessage(string message, Exception? exception);

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

    public async Task<IMessageChannel?> GetGuildMessageChannelAsync(ulong guildId, ulong channelId)
    {
        var guild = _client.GetGuild(guildId);
        var channel = await ((IGuild)guild).GetChannelAsync(channelId);
        return channel as IMessageChannel; // so you can send messages to it
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
        // TODO also need to be able to detect that the bot was previously kicked from a
        // guild, which of course de-registers its commands...
        Task.Run(() => RegisterGuildCommandsAsync(arg));
        return Task.CompletedTask;
    }

    private Task OnLogAsync(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                DiscordErrorMessage(message.Message, message.Exception);
                break;

            case LogSeverity.Warning:
                DiscordWarningMessage(message.Message, message.Exception);
                break;

            case LogSeverity.Info:
                DiscordInformationMessage(message.Message, message.Exception);
                break;

            default:
                DiscordDebugMessage(message.Message, message.Exception);
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
            if (!MessageBase.TryParseMessageId(arg.Data.CustomId, out var entityId, out var controlId)) return;

            var dataService = scope.ServiceProvider.GetRequiredService<SqlDataService>();
            var message = await dataService.GetMessageAsync(entityId, cancellationSource.Token);
            if (message is null || message.UserId != arg.User.Id) return;

            var handler = scope.ServiceProvider.ResolveMessageHandler(message);
            var completed = await handler.HandleAsync(arg, controlId, message, cancellationSource.Token);
            if (completed) await dataService.DeleteMessageAsync(entityId, cancellationSource.Token);
        }
        catch (OperationCanceledException ex)
        {
            SlashCommandTimeoutMessage(arg.Data.CustomId, SlashCommandTimeout, ex.Message, ex);
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
                SlashCommandTimeoutMessage(command.Data.Name, SlashCommandTimeout, ex.Message, ex);
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
            RegisterCommandsErrorMessage(ex.Message, ex);
        }
    }

    private async Task RegisterGuildCommandsAsync(SocketGuild guild)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        try
        {
            var dataService = scope.ServiceProvider.GetRequiredService<SqlDataService>();
            var guildEntity = await dataService.EnsureGuildAsync(guild.Id);
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
                RegisteredCommandMessage(guild.Name, build.Name.Value, build.Description.Value, command.GetType());
            }

            await dataService.UpdateGuildCommandVersionAsync(guildEntity, CommandBase.Version);
        }
        catch (Exception ex)
        {
            RegisterGuildCommandsErrorMessage(guild.Name, ex.Message, ex);
        }
    }
}
