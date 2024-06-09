using Discord;
using ThirteenIsh.Commands;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.Services;

/// <summary>
/// I did a lot of fancy stuff here, but at the scale that I'm running at, I suspect that
/// simply using Task.Run to send off message handlers into their own independent task
/// is entirely good enough, and avoids issues with commands waiting for each others'
/// retries when they could be doing something.
/// </summary>
internal sealed partial class ChannelMessageService(
    ILogger<ChannelMessageService> logger,
    IServiceProvider serviceProvider)
{
    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(45);

    [LoggerMessage(Level = LogLevel.Error, EventId = 3, Message =
        "ChannelMessageService : Error processing message of type {Type} : {Message}")]
    private partial void ErrorProcessingMessage(Type type, string message, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 4, Message =
        "Message handler {Type} timed out after {Timeout} : {Details}")]
    private partial void MessageTimeoutMessage(Type type, TimeSpan timeout, string details, Exception exception);

    private readonly ILogger<ChannelMessageService> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary>
    /// Adds a message for processing. Takes care of deferring the interaction, and of
    /// finishing it and complaining if we're too busy.
    /// After calling this, you can return from the command handler without doing anything else.
    /// TODO supply cancellation token here, do WriteAsync with that brief cancellation rather than TryWrite?
    /// </summary>
    public async Task AddMessageAsync(IDiscordInteraction interaction, MessageBase message, bool ephemeral = false)
    {
        await interaction.DeferAsync(ephemeral);
        var _ = Task.Run(() => ProcessMessageAsync(interaction, message));
    }

    private async Task ProcessMessageAsync(IDiscordInteraction interaction, MessageBase message)
    {
        try
        {
            using CancellationTokenSource cancellationSource = new(MessageTimeout);
            await using var scope = _serviceProvider.CreateAsyncScope();

            var handler = MessageHandlerRegistration.ResolveMessageHandler(scope.ServiceProvider, message);
            while (!await handler.HandleAsync(interaction, string.Empty, message, cancellationSource.Token))
            {
                // I doubt there's going to be a use case for this, but including the code anyway just in case
                await Task.Yield();
            }
        }
        catch (OperationCanceledException ex)
        {
            MessageTimeoutMessage(message.GetType(), MessageTimeout, ex.Message, ex);
            await CommandUtil.RespondWithTimeoutMessageAsync(interaction, MessageTimeout, message.GetType().Name);
        }
        catch (Exception ex)
        {
            ErrorProcessingMessage(message.GetType(), ex.Message, ex);
            await CommandUtil.RespondWithInternalErrorMessageAsync(interaction, message.GetType().Name);
        }
    }
}
