using Discord;
using System.Threading.Channels;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.Services;

/// <summary>
/// Use to process channel messages (do database transactions in response to input; not passed
/// through a Discord component or stored in the database; lost if the bot host goes down.)
/// These messages let me defer a response to a command for a longer time while attempting the
/// transaction.
/// The service lets me throttle the number of concurrent transaction attempts that go on.
/// </summary>
internal sealed partial class ChannelMessageService : IAsyncDisposable
{
    private const int BufferSize = 400;
    private const int Concurrency = 8;

    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(45);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1, Message =
        "ChannelMessageService started with {WorkerCount} workers")]
    private partial void StartedMessage(int workerCount);

    [LoggerMessage(Level = LogLevel.Information, EventId = 2, Message =
        "ChannelMessageService stopped. Processed {MessageCount} messages")]
    private partial void StoppedMessage(long messageCount);

    [LoggerMessage(Level = LogLevel.Error, EventId = 3, Message =
        "ChannelMessageService : Error processing message of type {Type} : {Message}")]
    private partial void ErrorProcessingMessage(Type type, string message, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 4, Message =
        "ChannelMessageService : Error stopping worker")]
    private partial void ErrorStoppingWorkerMessage(Exception exception);

    private readonly CancellationTokenSource _cancellationSource = new();

    private readonly Channel<ChannelMessage> _channel =
        Channel.CreateBounded<ChannelMessage>(BufferSize);

    private readonly ILogger<ChannelMessageService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly List<Task> _tasks = [];
    private readonly List<TaskCompletionSource<long>> _taskCompletionSources = [];

    private bool _isDisposed;

    public ChannelMessageService(
        ILogger<ChannelMessageService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        for (var i = 0; i < Concurrency; ++i)
        {
            _taskCompletionSources.Add(new TaskCompletionSource<long>());
            _tasks.Add(ProcessAsync(i));
        }
    }

    /// <summary>
    /// Adds a message for processing. Takes care of deferring the interaction, and of
    /// finishing it and complaining if we're too busy.
    /// After calling this, you can return from the command handler without doing anything else.
    /// </summary>
    public async Task AddMessageAsync(IDiscordInteraction interaction, MessageBase message, bool ephemeral = false)
    {
        await interaction.DeferAsync(ephemeral);
        if (_channel.Writer.TryWrite(new ChannelMessage(interaction, message))) return;

        // If we got here, the channel is full and the user should try again later.
        await interaction.ModifyOriginalResponseAsync(
            properties => properties.Content = "The bot is too busy. Please try again later.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _channel.Writer.TryComplete();
        _cancellationSource.Cancel();
        await Task.WhenAll(_taskCompletionSources.Select(x => x.Task));
        _isDisposed = true;
    }

    private async Task ProcessAsync(int index)
    {
        var messageCount = 0L;
        try
        {
            while (await _channel.Reader.WaitToReadAsync(_cancellationSource.Token))
            {
                while (_channel.Reader.TryRead(out var message))
                {
                    await ProcessMessageAsync(message);
                    ++messageCount;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorStoppingWorkerMessage(ex);
        }
        finally
        {
            _taskCompletionSources[index].TrySetResult(messageCount);
        }
    }

    private async Task ProcessMessageAsync(ChannelMessage channelMessage)
    {
        var (interaction, message) = channelMessage;

        // I want to cancel each message processing either after the message timeout,
        // or when the host is stopped, whichever comes first
        using CancellationTokenSource messageTimeoutSource = new(MessageTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            _cancellationSource.Token, messageTimeoutSource.Token);

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            var handler = scope.ServiceProvider.ResolveMessageHandler(message);
            var completed = await handler.HandleAsync(interaction, string.Empty, message, linkedSource.Token);
            if (!completed)
            {
                // Push that message back onto the channel
                // (I don't think I'm going to use this, but still...)
                await _channel.Writer.WriteAsync(channelMessage, linkedSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            var isMessageTimeout = messageTimeoutSource.IsCancellationRequested;
            var errorMessage = isMessageTimeout
                ? "Operation timed out."
                : "The bot is shutting down.";

            await interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage);
            if (!isMessageTimeout) throw;
        }
        catch (Exception exception)
        {
            // Report and swallow this exception, don't let it break the worker
            ErrorProcessingMessage(channelMessage.Message.GetType(), exception.Message, exception);
        }
    }

    private record ChannelMessage(IDiscordInteraction Interaction, MessageBase Message);
}
