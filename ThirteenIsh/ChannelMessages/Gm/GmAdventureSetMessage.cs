namespace ThirteenIsh.ChannelMessages.Gm;

internal sealed class GmAdventureSetMessage : GuildMessage
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}
