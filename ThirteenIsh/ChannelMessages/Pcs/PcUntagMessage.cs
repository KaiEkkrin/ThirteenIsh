namespace ThirteenIsh.ChannelMessages.Pcs;

internal sealed class PcUntagMessage : GuildMessage
{
    public required bool AsGm { get; init; }
    public string? Name { get; init; }
    public required string TagValue { get; init; }
}
