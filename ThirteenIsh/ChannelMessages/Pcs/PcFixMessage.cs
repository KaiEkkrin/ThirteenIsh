namespace ThirteenIsh.ChannelMessages.Pcs;

internal class PcFixMessage : GuildMessage
{
    public required bool AsGm { get; init; }
    public string? Name { get; init; }
    public required string CounterNamePart { get; init; }
    public required int FixValue { get; init; }
}
