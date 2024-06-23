namespace ThirteenIsh.ChannelMessages.Pcs;

internal class PcFixMessage : GuildMessage
{
    public string? Name { get; init; }
    public required string CounterNamePart { get; init; }
    public required int FixValue { get; init; }
}
