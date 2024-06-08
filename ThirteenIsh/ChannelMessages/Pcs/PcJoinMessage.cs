namespace ThirteenIsh.ChannelMessages.Pcs;

internal sealed class PcJoinMessage : GuildMessage
{
    public required string CharacterName { get; init; }
}
