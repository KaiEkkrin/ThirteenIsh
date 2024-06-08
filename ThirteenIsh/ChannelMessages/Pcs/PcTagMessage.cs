namespace ThirteenIsh.ChannelMessages.Pcs;

internal sealed class PcTagMessage : GuildMessage
{
    public string? Name { get; init; }
    public required string TagValue { get; init; }
}
