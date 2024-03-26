using Discord.WebSocket;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Services;

/// <summary>
/// Helps handle pinned messages.
/// </summary>
internal sealed class PinnedMessageService
{
    private static readonly Action<ILogger, string, string, Exception> ErrorWritingMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1, nameof(PinnedMessageService)),
            "Pinned message {Action} : {Message}");

    private readonly DataService _dataService;
    private readonly ILogger<PinnedMessageService> _logger;

    public PinnedMessageService(
        DataService dataService,
        ILogger<PinnedMessageService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async Task SetEncounterMessageAsync(ISocketMessageChannel channel, string adventureName, ulong guildId, string text,
        CancellationToken cancellationToken = default)
    {
        await _dataService.EditGuildAsync(
            new SetEncounterMessageOperation(this, channel, adventureName, text),
            guildId,
            cancellationToken);
    }

    private static async Task<ulong> CreateAsync(ISocketMessageChannel channel, string text)
    {
        var message = await channel.SendMessageAsync(text);
        await message.PinAsync();
        return message.Id;
    }

    private async Task<bool> UpdateAsync(ISocketMessageChannel channel, ulong messageId, string text)
    {
        try
        {
            var result = await channel.ModifyMessageAsync(messageId, message =>
            {
                message.Content = text;
            });

            return result != null;
        }
        catch (Exception ex)
        {
            // I'm not quite sure what this would throw, so I'll just catch anything for now
            ErrorWritingMessage(_logger, nameof(UpdateAsync), ex.Message, ex);
            return false;
        }
    }

    private sealed class SetEncounterMessageOperation(
        PinnedMessageService pinnedMessageService,
        ISocketMessageChannel channel,
        string adventureName,
        string encounterMessage)
        : EditOperation<Guild, Guild, EditResult<Guild>>()
    {
        public override async Task<EditResult<Guild>> DoEditAsync(Guild guild, CancellationToken cancellationToken)
        {
            if (!guild.Encounters.TryGetValue(channel.Id, out var encounter) ||
                encounter.AdventureName != adventureName)
            {
                // The current encounter or adventure has changed and this message is no longer valid.
                return new EditResult<Guild>(null);
            }

            if (encounter.NativePinnedMessageId is { } pinnedMessageId &&
                await pinnedMessageService.UpdateAsync(channel, pinnedMessageId, encounterMessage))
            {
                // No edit to the guild is needed.
                return new EditResult<Guild>(null);
            }

            pinnedMessageId = await CreateAsync(channel, encounterMessage);
            encounter.PinnedMessageId = (long)pinnedMessageId;
            return new EditResult<Guild>(guild);
        }
    }
}

