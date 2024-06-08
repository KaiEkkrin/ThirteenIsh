namespace ThirteenIsh.ChannelMessages.Pcs;

internal sealed class PcUntagMessage : GuildMessage
{
    public string? Name { get; init; }
    public required string TagValue { get; init; }
}
