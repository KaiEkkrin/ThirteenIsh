using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities.Messages;

public class EndEncounterMessage : MessageBase
{
    /// <summary>
    /// The channel ID.
    /// </summary>
    public long ChannelId { get; set; }

    [BsonIgnore]
    public ulong NativeChannelId => (ulong)ChannelId;

    /// <summary>
    /// The guild ID.
    /// </summary>
    public long GuildId { get; set; }

    [BsonIgnore]
    public ulong NativeGuildId => (ulong)GuildId;

    public override async Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var (output, message) = await dataService.EditGuildAsync(
            new EditOperation(NativeChannelId), NativeGuildId, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await component.RespondAsync(message, ephemeral: true);
            return true;
        }

        if (output is null) throw new InvalidOperationException(nameof(output));
        if (output.Encounter.NativePinnedMessageId is { } pinnedMessageId)
            await component.Channel.DeleteMessageAsync(pinnedMessageId);

        await component.RespondAsync("Encounter has ended.");
        return true;
    }

    private sealed class EditOperation(ulong channelId)
        : SyncEditOperation<ResultOrMessage<EditOutput>, Guild, MessageEditResult<EditOutput>>
    {
        public override MessageEditResult<EditOutput> DoEdit(Guild guild)
        {
            if (!guild.Encounters.TryGetValue(channelId, out var encounter))
                return new MessageEditResult<EditOutput>(null, "There is no active encounter in this channel.");

            guild.Encounters.Remove(channelId);
            return new MessageEditResult<EditOutput>(new EditOutput(guild, encounter));
        }
    }

    private sealed record EditOutput(Guild Guild, Encounter Encounter);
}
