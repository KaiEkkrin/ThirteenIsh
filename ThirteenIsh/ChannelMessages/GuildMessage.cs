using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.ChannelMessages;

internal class GuildMessage : MessageBase
{
    public required ulong GuildId { get; init; }
}
