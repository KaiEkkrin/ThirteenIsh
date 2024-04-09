﻿using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Results;

namespace ThirteenIsh.Services;

/// <summary>
/// Helps handle pinned messages.
/// </summary>
internal sealed class PinnedMessageService(
    SqlDataService dataService,
    ILogger<PinnedMessageService> logger)
{
    private static readonly Action<ILogger, string, string, Exception> ErrorWritingMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1, nameof(PinnedMessageService)),
            "Pinned message {Action} : {Message}");

    public async Task SetEncounterMessageAsync(ISocketMessageChannel channel, string adventureName, ulong guildId, string text,
        CancellationToken cancellationToken = default)
    {
        await dataService.EditEncounterAsync(
            guildId, channel.Id,
            new SetEncounterMessageOperation(this, channel, adventureName, text),
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
            ErrorWritingMessage(logger, nameof(UpdateAsync), ex.Message, ex);
            return false;
        }
    }

    private sealed class SetEncounterMessageOperation(
        PinnedMessageService pinnedMessageService,
        ISocketMessageChannel channel,
        string adventureName,
        string encounterMessage)
        : EditOperation<Encounter, EncounterResult, EditResult<Encounter>>()
    {
        public override async Task<EditResult<Encounter>> DoEditAsync(EncounterResult encounterResult,
            CancellationToken cancellationToken)
        {
            var (adventure, encounter) = encounterResult;
            if (adventure.Name != adventureName)
            {
                // The current encounter or adventure has changed and this message is no longer valid.
                return new EditResult<Encounter>(null);
            }

            if (encounter.PinnedMessageId is { } pinnedMessageId &&
                await pinnedMessageService.UpdateAsync(channel, pinnedMessageId, encounterMessage))
            {
                // No edit to the guild is needed.
                return new EditResult<Encounter>(null);
            }

            pinnedMessageId = await CreateAsync(channel, encounterMessage);
            encounter.PinnedMessageId = pinnedMessageId;
            return new EditResult<Encounter>(encounter);
        }
    }
}

